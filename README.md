# School Management System (IBSS)

End-to-end school management platform: students, partners, programmes, majors, subjects, enrolments, document handling, and pathway-based admission rules. Includes an admin portal and a partner portal.

## Stack

| Layer        | Tech                                                      |
|--------------|-----------------------------------------------------------|
| Backend      | ASP.NET Core (.NET 10), modular feature-slice projects     |
| Persistence  | EF Core 10 + SQLite (field-level AES-256 at-rest encryption) |
| Auth         | OPAQUE (Ristretto255 OPRF + Ed25519) — zero-knowledge      |
| Frontend     | Vue 3 + Vite, axios                                        |
| Crypto (FE)  | `@noble/curves`, `@noble/hashes`                           |

## Repository layout

```
backend/
  Odin.Api.Base/                          EF Core DbContext, entities, migrations
  SharedLibrary.Basics.Opaque.Domains/    Domain entities + repository interfaces
  SharedLibrary.Basics.Opaque.Api/        ASP.NET Core host (Program.cs, DI, middleware)
  SharedLibrary.Basics.Opaque.*Api/       OPAQUE feature slices (Login, Mfa, Profile, …)
  School/                                 School-domain feature slices
    School.ProgrammeApi/                  Core programme CRUD + approval workflow
    School.PartnerAdminApi/               Partner self-service (custom programmes etc.)
    School.MajorApi/                      Majors
    School.SubjectApi/                    Subjects
    SystemConfigurations/                 Pathways, DocumentTypes, ModesOfStudy, …
frontend/
  app/
    src/views/                            Page-level Vue components
    src/components/                       Shared widgets (CrudManager, PathwayManager, …)
    src/api/client.js                     Axios instance with bearer-token interceptor
    vite.config.js                        Proxies /v1/* → http://localhost:5103
```

## Architecture

### Feature-slice + CQRS

Every endpoint lives in its own folder under `{Feature}/V1/{Operation}/`:

```
Command/    *Command.cs, *CommandHandler.cs, *CommandResult.cs, *CommandValidator.cs
Endpoint/   *Endpoint.cs (IEndpointMarker), *EndpointRequest/Response.cs
            Mappers/ *RequestToCommandMapper.cs, *CommandResultToResponseMapper.cs
*MappersServicesExtension.cs   service registration hook (IMapperMarker)
```

The dispatcher auto-discovers handlers and mappers by scanning marker assemblies registered in `Program.cs`. `TransactionPipelineBehaviour` wraps every command in an EF transaction and calls `SaveChangesAsync` on success — handlers do **not** call save themselves.

### Authorization

| Policy        | Used by                                          |
|---------------|--------------------------------------------------|
| `AdminOnly`   | `/v1/school/*`, `/v1/admin/*`, system config     |
| `PartnerOnly` | `/v1/partner/*` (self-service)                   |

PartnerId is **not** in the JWT claims — it lives on the `ApplicationUser` record and is loaded per request.

### Domain model highlights

- **Programme** — `PartnerId == null` for IBSS core programmes; non-null for partner-owned (cloned or from-scratch). Approval workflow `Draft → Pending → Approved/Rejected`. Admin override via `IsDisabledByAdmin`.
- **Pathway** — admission route (e.g. *Pathway 2: Bachelor's Degree + 5 Years Work Exp*). Pathways carry **required document types** via `PathwayDocumentRequirement`.
- **ProgrammePathway** — many-to-many between Programme and Pathway; defines which pathways a programme accepts.
- **ProgrammeDocumentRequirement** — programme- or major-level required documents.
- **DocumentType / Pathway / ModeOfStudy / TuitionFeeStatus / EnrollmentStatus / FinalProjectStatus** — system-config reference data with admin CRUD.

## Running locally

### Prerequisites
- .NET 10 SDK
- Node.js 20+

### Backend

```bash
cd backend
dotnet build
dotnet run --project SharedLibrary.Basics.Opaque.Api    # http://localhost:5103
```

EF migrations:
```bash
dotnet ef migrations add {Name}         --project Odin.Api.Base
dotnet ef database update               --project Odin.Api.Base
```

### Frontend

```bash
cd frontend/app
npm install
npm run dev                                              # http://localhost:5173
```

Vite proxies `/v1/*` → `http://localhost:5103`.

## Configuration

`backend/SharedLibrary.Basics.Opaque.Api/appsettings.json`:

| Key                          | Purpose                                                    |
|------------------------------|------------------------------------------------------------|
| `ConnectionStrings:Default…` | SQLite path (default `odin.db` in the API project dir)     |
| `Encryption:FieldKey`        | AES-256 key for field-level encryption (rotate per env)    |
| `Brevo:*`                    | SMTP relay credentials for transactional email             |
| `App:Domain`                 | Used in FIDO2 challenges and invite-link generation        |
| `Cors:AllowedOrigins`        | Origins allowed to call `/v1/*` (5173–5177 by default)     |

> **Security:** `Encryption:FieldKey` and `Brevo:SmtpPassword` must be rotated per environment. Do not reuse the values shipped in this repo.

## Adding a new endpoint

1. Create the CQRS folder structure above inside the relevant feature project.
2. Add a `*MappersServicesExtension.cs` that implements `IMapperMarker`.
3. Register the assembly marker type in `SharedLibrary.Basics.Opaque.Api/Program.cs` → `markerTypes` HashSet.
4. If you added or changed entity fields, add an EF migration (`dotnet ef migrations add …`).

## License

Proprietary — all rights reserved.
