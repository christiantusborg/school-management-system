# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend
```bash
dotnet run --project SharedLibrary.Basics.Opaque.Api              # runs on http://localhost:5103
dotnet build                                                        # build all projects
dotnet run --project SharedLibrary.Basics.Opaque.Api --no-launch-profile -- --urls "http://localhost:5103"  # no browser launch
```

### Frontend (Odin.Web)
```bash
cd Odin.Web && npm run dev    # runs on http://localhost:5173
```
Vite proxies `/v1/*` → `http://localhost:5103`.

### EF Core Migrations
```bash
dotnet ef migrations add {Name} --project Odin.Api.Base
dotnet ef database update --project Odin.Api.Base
```

## Architecture

### Backend — Modular feature slices
Each feature lives in its own project (e.g. `SharedLibrary.Basics.Opaque.LoginApi`). The host project `SharedLibrary.Basics.Opaque.Api` wires everything together via assembly markers.

- **`Odin.Api.Base`** — EF Core `OdinDbContext`, Identity, entity models, migrations, field-level encryption
- **`SharedLibrary.Basics.Opaque.Domains`** — domain entities and repository interfaces
- **`SharedLibrary.Basics.Opaque.Api`** — ASP.NET Core host, DI registration, middleware, `Program.cs`
- **Feature API projects** — each is self-contained: LoginApi, RegisterApi, ChangePasswordApi, ProfileApi, MfaEmailApi, MfaToTpApi, MfaFido2Api, AccountApi, AdminApi, etc.
- **`SharedLibrary.Basics.OpaqueService`** — server-side OPAQUE crypto (Ristretto255 OPRF, Ed25519 signature verification)
- **`SharedLibrary.Basics.TransientStateCache`** — in-memory TTL cache for multi-step flows (login state, MFA state, verify state)

### CQRS Pattern
Every endpoint follows the same structure under `{Feature}/V1/{Operation}/`:

```
Command/
  *Command.cs            — implements IHandleableCommand<Command, Validator, Handler, Result>
  *CommandHandler.cs     — implements ICommandHandler<Command, Result, TEntity, TRepository>
  *CommandResult.cs      — result DTO
  *CommandValidator.cs   — FluentValidation rules
Endpoint/
  *Endpoint.cs           — IEndpointMarker, [Route("/v1/...")], [EndpointTag("...")]
  *EndpointRequest.cs    — request DTO (if needed)
  *EndpointResponse.cs   — response DTO (extends HateoasBaseResponse)
  Mappers/
    *RequestToCommandMapper.cs
    *CommandResultToResponseMapper.cs
*MappersServicesExtension.cs  — service registration hook
```

Handlers return `SuccessOrFailure<T>`. Endpoints call `.ToCreatedResult()` or `.ToOkResult()` to convert to `IResult`.

The dispatcher auto-discovers handlers and mappers by scanning assemblies registered via marker types in `Program.cs`.

### Pipeline Behaviors
`TransactionPipelineBehaviour` wraps every command in an EF transaction and calls `SaveChangesAsync` on success. This means handlers should NOT call `SaveChangesAsync` themselves.

### OPAQUE Authentication
Zero-knowledge password authentication using OPRF (Ristretto255) + Ed25519:
1. **Init:** Client blinds password, sends blinded element → server evaluates OPRF → client unblinds to derive Ed25519 key pair
2. **Finish:** Client signs a server-issued challenge with derived private key → server verifies against stored public key

Frontend crypto in `Odin.Web/src/crypto/opaque.ts` uses `@noble/curves` and `@noble/hashes`.
OPRF seeds stored encrypted at rest (AES-256 via `Encryption:FieldKey` config).

### Adding a new endpoint — checklist
1. Create the folder structure above in the relevant feature project
2. Add a `*MappersServicesExtension.cs` (implements `IMapperMarker`)
3. Register the assembly marker type in `Program.cs` → `markerTypes` HashSet
4. If adding `IsVerified` or similar fields to entities, add an EF migration

### Database
SQLite (`odin.db` in the Api project directory). All schema changes via EF Core migrations in `Odin.Api.Base/Data/Migrations/`.

### Key Config (appsettings.json)
- `ConnectionStrings:DefaultConnection` — SQLite path
- `Encryption:FieldKey` — AES-256 key for at-rest field encryption (OprfSeed, TotpSecret, etc.)
- `Brevo:*` — SMTP settings for email sending (real emails via smtp-relay.brevo.com)
- `App:Domain` — used in FIDO2 and invite link generation
- CORS allows origins on ports 5173–5177
