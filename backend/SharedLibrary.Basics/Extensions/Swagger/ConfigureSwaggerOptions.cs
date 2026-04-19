using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuVian.SharedLibrary.Basics.Extensions.Swagger;

/// <summary>
/// Configures Swagger options including security definitions and application metadata.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
    /// </summary>
    /// <param name="environment">The current hosting environment.</param>
    public ConfigureSwaggerOptions(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Applies the Swagger configuration.
    /// </summary>
    /// <param name="options">The SwaggerGenOptions instance to configure.</param>
    public void Configure(SwaggerGenOptions options)
    {
        // Adds JWT Bearer token authentication scheme
        options.AddSecurityDefinition("Bearer",
            new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please provide a valid JWT token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

        // Sets the requirement for the Bearer token in all requests
        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    }
}