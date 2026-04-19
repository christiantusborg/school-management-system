using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuVian.SharedLibrary.Basics.MockProviders;
using Odin.Api.Base;
using Odin.Api.Base.Authentication;
using Odin.Api.Base.Data;
using Odin.Api.Base.Data.Repositories;
using Odin.Api.Base.Middleware;
using SharedLibrary.Basics.Opaque.Api.Infrastructure;
using SharedLibrary.Basics.TransientStateCache;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.Opaque.Domains;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using QuVian.SharedLibrary.Basics.Dispatchers;
using QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.TransactionPipelineBehaviours;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials;
using QuVian.SharedLibrary.Basics.Mappers;
using QuVian.SharedLibrary.Validations;
using SharedLibrary.Basics.Opaque.LoginApi;
using SharedLibrary.Basics.Opaque.LogoutApi;
using SharedLibrary.Basics.Opaque.RegisterApi;
using SharedLibrary.Basics.Opaque.RecoveryLoginApi;
using SharedLibrary.Basics.Opaque.PasswordResetApi;
using SharedLibrary.Basics.Opaque.MeApi;
using SharedLibrary.Basics.Opaque.AccountApi;
using SharedLibrary.Basics.Opaque.KemKeyPairApi;
using SharedLibrary.Basics.Opaque.MfaStatusApi;
using SharedLibrary.Basics.Opaque.MfaToTpApi;
using SharedLibrary.Basics.Opaque.MfaEmailApi;
using SharedLibrary.Basics.Opaque.MfaSmsApi;
using SharedLibrary.Basics.Opaque.MfaFido2Api;
using SharedLibrary.Basics.Opaque.ProfileApi;
using SharedLibrary.Basics.Opaque.AdminApi;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi;
using SharedLibrary.Basics.Opaque.ChangePasswordApi;
using School.DocumentTypeApi;
using School.EnrollmentStatusApi;
using School.FinalProjectStatusApi;
using School.PathwayApi;
using School.TuitionFeeStatusApi;
using School.ModeOfStudyApi;
using School.ProgrammeApi;
using School.MajorApi;
using School.SubjectApi;
using School.PartnerAdminApi;

using Swashbuckle.AspNetCore.SwaggerUI;
using QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials;
using QuVian.SharedLibrary.Basics.MockProviders;
using QuVian.SharedLibrary.Basics.Mappers;
using QuVian.SharedLibrary.Validations;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/odin-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var markerTypes = new HashSet<Type>
{
    typeof(AccountApiAssemblyMarker),
    typeof(AdminApiAssemblyMarker),
    typeof(ChangePasswordApiAssemblyMarker),
    typeof(KemKeyPairApiAssemblyMarker),
    typeof(LoginApiAssemblyMarker),
    typeof(LogoutApiAssemblyMarker),
    typeof(MeApiAssemblyMarker),
    
    typeof(MfaEmailApiAssemblyMarker),
    typeof(MfaFido2ApiAssemblyMarker),
    typeof(MfaSmsApiAssemblyMarker),
    typeof(MfaStatusApiAssemblyMarker),
    typeof(MfaTotpApiAssemblyMarker),
    typeof(PasswordResetApiAssemblyMarker),
    typeof(ProfileApiAssemblyMarker),
    typeof(RecoveryCodesApiAssemblyMarker),
    typeof(RecoveryLoginApiAssemblyMarker),
    typeof(RegisterApiAssemblyMarker),

    typeof(DocumentTypeApiAssemblyMarker),
    typeof(EnrollmentStatusApiAssemblyMarker),
    typeof(FinalProjectStatusApiAssemblyMarker),
    typeof(PathwayApiAssemblyMarker),
    typeof(TuitionFeeStatusApiAssemblyMarker),
    typeof(ModeOfStudyApiAssemblyMarker),

    typeof(ProgrammeApiAssemblyMarker),
    typeof(MajorApiAssemblyMarker),
    typeof(SubjectApiAssemblyMarker),
    typeof(PartnerAdminApiAssemblyMarker),
};
var configuration = builder.Configuration;
builder.Services.AddEndpointV1Adjunct(configuration);
builder.Services.AddEndpointV1Deprecations(configuration);
var assemblies = AppDomain.CurrentDomain.GetAssemblies();

// Field-level encryption for sensitive DB columns (TotpSecret, OprfSeed).
// Must be configured before any DbContext is created.
Odin.Api.Base.Crypto.FieldEncryption.Configure(
    builder.Configuration["Encryption:FieldKey"]
    ?? throw new InvalidOperationException("Encryption:FieldKey is required. Generate a 32-byte hex key and add it to appsettings.json or environment variables."));

// Database
builder.Services.AddDbContext<OdinDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// Register base DbContext so TransactionPipelineBehaviour can resolve it
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<OdinDbContext>());

// Identity (password options are intentionally minimal — OPAQUE handles password security client-side)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<OdinDbContext>()
    .AddDefaultTokenProviders();

// Providers
builder.Services.AddSingleton<IGuidProvider, SystemGuidProvider>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<IDispatcher, Dispatcher>();
// Authentication & Crypto
builder.Services.AddScoped<SessionTokenService>();
builder.Services.AddSingleton<Odin.Api.Base.Crypto.OpaqueServer>();
builder.Services.AddSingleton<SharedLibrary.Basics.OpaqueService.OpaqueServer>();

// Senders (stub implementations for local development)
builder.Services.AddScoped<ISmsSender, NullSmsSender>();
builder.Services.AddScoped<IEmailSender, BrevoEmailSender>();

// Repositories

builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IOpaqueCredentialRepository, OpaqueCredentialRepository>();
builder.Services.AddScoped<IOpaqueRecoveryCodeRepository, OpaqueRecoveryCodeRepository>();
builder.Services.AddScoped<IKemKeyPairRepository, KemKeyPairRepository>();
builder.Services.AddScoped<ISessionTokenRepository, SessionTokenRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IInviteCodeRepository, InviteCodeRepository>();
builder.Services.AddScoped<IUserTwoFactorMethodRepository, UserTwoFactorMethodRepository>();
builder.Services.AddScoped<IFido2CredentialRepository, Fido2CredentialRepository>();
builder.Services.AddScoped<IUserPhoneRepository, UserPhoneRepository>();
builder.Services.AddScoped<IUserContactEmailRepository, UserContactEmailRepository>();
builder.Services.AddScoped<IUserAddressRepository, UserAddressRepository>();

builder.Services.AddScoped<Odin.Api.Base.Services.OpaqueUserCreationService>();

// Student domain
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IProgrammeRepository, ProgrammeRepository>();
builder.Services.AddScoped<IMajorRepository, MajorRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
builder.Services.AddScoped<IStudentDocumentRepository, StudentDocumentRepository>();
builder.Services.AddScoped<IProgrammeDocumentRequirementRepository, ProgrammeDocumentRequirementRepository>();
builder.Services.AddScoped<IEnrollmentDocumentRepository, EnrollmentDocumentRepository>();
builder.Services.AddScoped<IEnrollmentStatusRepository, EnrollmentStatusRepository>();
builder.Services.AddScoped<ITuitionFeeStatusRepository, TuitionFeeStatusRepository>();
builder.Services.AddScoped<IModeOfStudyRepository, ModeOfStudyRepository>();
builder.Services.AddScoped<IPathwayRepository, PathwayRepository>();
builder.Services.AddScoped<IPathwayDocumentRequirementRepository, PathwayDocumentRequirementRepository>();
builder.Services.AddScoped<IProgrammePathwayRepository, ProgrammePathwayRepository>();
builder.Services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
builder.Services.AddScoped<IFinalProjectStatusRepository, FinalProjectStatusRepository>();
builder.Services.AddScoped<IFinalProjectRepository, FinalProjectRepository>();
builder.Services.AddScoped<ISubjectGradeRepository, SubjectGradeRepository>();
builder.Services.AddScoped<IStudentNoteRepository, StudentNoteRepository>();
builder.Services.AddAuthentication("SessionToken")
    .AddScheme<SessionTokenOptions, SessionTokenAuthenticationHandler>("SessionToken", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PartnerOnly", policy => policy.RequireRole("Partner"));
});

// Cache
builder.Services.AddTransientStateCache();

// Rate limiting — applied to all auth-sensitive endpoints (login, register, recovery)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("mfa", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(15);
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// FIDO2
var fido2Origins = builder.Configuration.GetSection("App:Origins").Get<string[]>()
    ?? [builder.Configuration["App:Origin"]!];
builder.Services.AddSingleton<IFido2>(new Fido2(new Fido2Configuration
{
    ServerDomain = builder.Configuration["App:Domain"]!,
    ServerName = "Odin",
    Origins = new HashSet<string>(fido2Origins)
}));

// MediatR (legacy handlers still in the monolith)
//builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// QuVian dispatcher + mappers (new API library handlers)
builder.Services.AddDispatcher(assemblies);
builder.Services.AddMappers(markerTypes.ToArray());

// Pipeline behavior: wraps every command in a transaction, auto-saves, and rolls back on error
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehaviour<,>));
//builder.Services.AddValidatorsFromAssemblies(assemblies);

builder.Services.AddValidations();
builder.Services.AddScoped(typeof(ValidationRuleFactory<>));

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// HttpContextAccessor (needed by mappers)
builder.Services.AddHttpContextAccessor();

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize QuVian SuccessOrFailure result extensions (must use app.Services, not BuildServiceProvider)
// Do NOT dispose this scope early — EndpointV1Adjunct stores the provider and uses it during endpoint mapping below
var initScope = app.Services.CreateScope();
SuccessOrFailureResultExtensions.Initialize(initScope.ServiceProvider);
EndpointV1Adjunct.Initialize(initScope.ServiceProvider);

// Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();



// Route group mapping
var routeGroupBuilder = app.MapGroup("/")
    .AllowAnonymous();
//    .RequireAuthorization();

routeGroupBuilder.MapEndpointsExcluding(
    markerTypes,
    []
);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed
await DatabaseSeeder.SeedAsync(app.Services);

app.Run();

