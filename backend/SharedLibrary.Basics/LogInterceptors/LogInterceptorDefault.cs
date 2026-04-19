using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;

namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
///     Class LogInterceptor.
///     Implements the <see cref="IInterceptor" />.
/// </summary>
/// <seealso cref="IInterceptor" />
[SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
// ReSharper disable once ClassNeverInstantiated.Global
public class LogInterceptorDefault : IInterceptor
{
    private static readonly FieldInfo? IsSuccessField = typeof(SuccessOrFailure<>).GetField("IsSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? SuccessField = typeof(SuccessOrFailure<>).GetField("success", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? FaultedField = typeof(SuccessOrFailure<>).GetField("faulted", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Represents the configuration object that provides access to application settings.
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    ///     The context.
    /// </summary>
    private readonly IHttpContextAccessor _context;

    /// <summary>
    ///     The logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LogInterceptor" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="context">The context.</param>
    /// ///
    /// <param name="configuration">The configuration.</param>
    public LogInterceptorDefault(ILogger<LogInterceptorDefault> logger, IHttpContextAccessor context, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    ///     Intercepts the specified invocation.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Should never happen.</exception>
    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    [SuppressMessage("Usage", "CA2253:Named placeholders should not be numeric values")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public void Intercept(IInvocation invocation)
    {
        Debug.Assert(invocation != null, nameof(invocation) + " != null");
        var sw = Stopwatch.StartNew();

        var currentLogLevel = Enum.TryParse(_configuration["LogInterceptorDefaultLogLevel"], true, out LogLevel logLevel) ? logLevel : LogLevel.Information;

        var hasLogInterceptorDefaultLogLevelOnInterface =
            invocation.Method.DeclaringType.GetAttribute<LogInterceptorDefaultLogLevelAttribute>();

        if (hasLogInterceptorDefaultLogLevelOnInterface != null)
        {
            currentLogLevel = hasLogInterceptorDefaultLogLevelOnInterface.LogLevel;
        }

        // Do we have a LogInterceptorDefaultLogLevelAttribute on the implementation?
        var target = invocation.InvocationTarget;
        var targetType = target.GetType();

        var hasLogInterceptorDefaultLogLevelOnImplementation =
            targetType.GetAttribute<LogInterceptorDefaultLogLevelAttribute>();

        if (hasLogInterceptorDefaultLogLevelOnImplementation != null)
        {
            currentLogLevel = hasLogInterceptorDefaultLogLevelOnImplementation.LogLevel;
        }

        // The +4 is for TraceIdentifier, Method, ExecutionTime and ReturnValue
        var arguments = new object[invocation.Arguments.Length + 4];

        var parameters = invocation.Method.GetParameters();
        var traceIdentifier = _context.HttpContext?.TraceIdentifier;
        arguments[0] = traceIdentifier!;
        arguments[1] = invocation.InvocationTarget + "." + invocation.Method.Name;
        var sb = new StringBuilder();

        var baseString = "TraceIdentifier: {{{0}}} -  Method: {{{1}}} - Executed in {{{2}}}ms - Arguments: ";

        if (invocation.Arguments.Length == 0)
        {
            baseString += "None ";
        }

        sb.Append(baseString);

        var argumentsCount = 3;
        foreach (var parameterInfo in parameters)
        {
            var argumentValue = invocation.Arguments[argumentsCount - 3];

            var isLogInterceptorExcludeAttribute =
                parameterInfo.GetCustomAttributes(typeof(LogInterceptorExcludeAttribute));

            if (argumentValue is null)
            {
                continue;
            }

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (isLogInterceptorExcludeAttribute?.Count() > 0)
            {
                argumentValue = "Excluded";
            }

            if (IsSimpleType(argumentValue.GetType()))
            {
                arguments[argumentsCount] = argumentValue;
            } else
            {
                var json = SerializeObject(argumentValue);
                arguments[argumentsCount] = new
                {
                    ArgumentType = argumentValue.GetType().FullName, Value = json
                };

                arguments[argumentsCount] = arguments.GetType().Name + (json == null ? "NotSerialized" : "");
            }

            sb.Append($"{parameterInfo.Name}:{{{argumentsCount}}} ");
            argumentsCount++;
        }

        sb.Append($"- ReturnValue (): {{{argumentsCount}}}");

        try
        {
            invocation.Proceed();
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"{nameof(LogInterceptorDefault)} failed invocation.Proceed()  Method: {{0}}, Exception {{1}} ",
                invocation.Method, ex);
        }
        finally
        {
            arguments[2] = sw.ElapsedMilliseconds;

            var tmp = invocation.ReturnValue;
            var returnValue = GetEitherResult(tmp);
            arguments[argumentsCount] = returnValue!;
            sw.Stop();

            switch (currentLogLevel)
            {
                case LogLevel.Trace:
                    _logger.LogTrace(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.Information:
                    _logger.LogInformation(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.Error:
                    _logger.LogError(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical(sb.ToString().Trim(), arguments);
                    break;
                case LogLevel.None:
                    break;
            }
        }
    }

    /// <summary>
    ///     Gets the either result.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
    private static object? GetEitherResult(object? response)
    {
        if (response is null)
        {
            return null;
        }

        var responseResultType = response.GetType();

        if (!responseResultType.IsGenericType)
        {
            return SerializeObject(new
            {
                response
            });
        }

        if (responseResultType.GetGenericTypeDefinition() != typeof(SuccessOrFailure<>))
        {
            return SerializeObject(response);
        }


        var isSuccess = (bool)IsSuccessField.GetValue(response)!;

        if (isSuccess)
        {
            var eitherSuccess = SuccessField.GetValue(response);
            var result = SerializeObject(eitherSuccess!);
            return result;
        }

        var eitherException = (SuccessOrFailureException)FaultedField.GetValue(response)!;
        var eitherExceptionResult = SerializeObject(eitherException.ToResult());
        return eitherExceptionResult;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private static object? SerializeObject(object response)
    {
        try
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new IgnorePropertiesResolver(typeof(LogInterceptorExcludeAttribute))
            };

            return JsonConvert.SerializeObject(response, jsonSerializerSettings);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    /// <summary>
    ///     Determines whether [is simple type] [the specified type].
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsSimpleType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        if (type.IsEnum)
        {
            return true;
        }

        if (type == typeof(Guid))
        {
            return true;
        }

        var tc = Type.GetTypeCode(type);
        switch (tc)
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.Char:
            case TypeCode.String:
            case TypeCode.Boolean:
            case TypeCode.DateTime:
                return true;
            case TypeCode.Object:
                return typeof(TimeSpan) == type || typeof(DateTimeOffset) == type;
            case TypeCode.Empty:
            case TypeCode.DBNull:
            default:
                return false;
        }
    }
}

/// <summary>
///     Class IgnorePropertiesResolver.
///     Implements the <see cref="Newtonsoft.Json.Serialization.DefaultContractResolver" />.
/// </summary>
/// <seealso cref="Newtonsoft.Json.Serialization.DefaultContractResolver" />
public class IgnorePropertiesResolver : DefaultContractResolver
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="IgnorePropertiesResolver" /> class.
    /// </summary>
    /// <param name="attributeType"></param>
    public IgnorePropertiesResolver(Type attributeType)
    {
        AttributeType = attributeType;
    }

    private Type AttributeType { get; }

    /// <summary>
    ///     Creates the property.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="memberSerialization">The member serialization.</param>
    /// <returns>JsonProperty.</returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var customInterceptorExcludeAttribute = member.GetCustomAttributes(AttributeType);
        var property = base.CreateProperty(member, memberSerialization);
        if (customInterceptorExcludeAttribute.Any())
        {
            property.ShouldSerialize = _ => false;
        }

        return property;
    }
}

