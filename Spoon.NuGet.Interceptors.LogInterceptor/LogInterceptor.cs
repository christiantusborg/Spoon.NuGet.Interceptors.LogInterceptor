namespace Spoon.NuGet.Interceptors.LogInterceptor
{
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using Castle.Core.Internal;
    using Castle.DynamicProxy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Spoon.NuGet.EitherCore;
    using Spoon.NuGet.EitherCore.Contracts;
    using Spoon.NuGet.EitherCore.Enums;
    using Spoon.NuGet.EitherCore.Exceptions;
    using Spoon.NuGet.EitherCore.Extensions;

    /// <summary>
    ///     Class LogInterceptor.
    ///     Implements the <see cref="IInterceptor" />.
    /// </summary>
    /// <seealso cref="IInterceptor" />
    public class LogInterceptorDefault  : IInterceptor
    {
        /// <summary>
        ///     The logger.
        /// </summary>
        private readonly ILogger<LogInterceptorDefault> _logger;

        /// <summary>
        ///     The context.
        /// </summary>
        private readonly IHttpContextAccessor _context;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LogInterceptor" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="context">The context.</param>
        public LogInterceptorDefault(ILogger<LogInterceptorDefault> logger, IHttpContextAccessor context)
        {
            this._logger = logger;
            this._context = context;
        }

        /// <summary>
        ///     Intercepts the specified invocation.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Should never happen.</exception>
        public void Intercept(IInvocation invocation)
        {
            var sw = Stopwatch.StartNew();

            var currentLogLevel = LogLevel.Information;

            var hasLogInterceptorDefaultLogLevel = invocation.Method.DeclaringType.GetAttribute<LogInterceptorDefaultLogLevelAttribute>();

            if (hasLogInterceptorDefaultLogLevel is not null)
            {
                currentLogLevel = hasLogInterceptorDefaultLogLevel.LogLevel;
            }

            var arguments = new object[invocation.Arguments.Length + 4];
            var parameters = invocation.Method.GetParameters();
            var traceIdentifier = this._context.HttpContext?.TraceIdentifier;
            arguments[0] = traceIdentifier!;
            arguments[1] = invocation.InvocationTarget + "." + invocation.Method.Name;
            var sb = new StringBuilder();

            var baseString = $"TraceIdentifier: {{{0}}} -  Method: {{{1}}} - Executed in {{{2}}}ms - Arguments: ";

            if (invocation.Arguments.Length == 0)
            {
                baseString += "None ";
            }

            sb.Append(baseString);

            var argumentsCount = 3;
            foreach (var parameterInfo in parameters)
            {
                var argumentValue = invocation.Arguments[argumentsCount - 3];

                var isLogInterceptorExcludeAttribute = parameterInfo.GetCustomAttributes(typeof(LogInterceptorExcludeAttribute));

                if(argumentValue is null)
                    continue;
                
                if (isLogInterceptorExcludeAttribute?.Count() > 0)
                {
                    argumentValue = "Excluded";
                }

                if (this.IsSimpleType(argumentValue.GetType()))
                {
                    arguments[argumentsCount] = argumentValue;
                }
                else
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(argumentValue, new JsonSerializerSettings
                        {
                            ContractResolver = new IgnorePropertiesResolver(new[]
                            {
                                "LogInterceptorExcludeAttribute",
                            }),
                        });
                        arguments[argumentsCount] = new
                        {
                            ArgumentType = argumentValue.GetType().FullName,
                            Value = json,
                        };

                    }
                    catch (Exception)
                    {
                        arguments[argumentsCount] = arguments.GetType().Name + "NotSerialized";
                    }
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
                this._logger.LogCritical($"LogInterceptor failed invocation.Proceed()  Method: {0}, Exception {1} ", invocation.Method, ex);
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
                        this._logger.LogTrace(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.Debug:
                        this._logger.LogDebug(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.Information:
                        this._logger.LogInformation(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.Warning:
                        this._logger.LogWarning(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.Error:
                        this._logger.LogError(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.Critical:
                        this._logger.LogCritical(sb.ToString().Trim(), arguments);
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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
                return JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    ContractResolver = new IgnorePropertiesResolver(new[]
                    {
                        "LogInterceptorExcludeAttribute",
                    }),
                });
            }

            if (responseResultType.GetGenericTypeDefinition() != typeof(Either<>))
            {
                return JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    ContractResolver = new IgnorePropertiesResolver(new[]
                    {
                        "LogInterceptorExcludeAttribute",
                    }),
                });
            }

            var eitherEnumField = responseResultType.GetField("EitherEnum", BindingFlags.NonPublic | BindingFlags.Instance);

            var eitherEnum = (EitherEnum)eitherEnumField!.GetValue(response) !;

            if (eitherEnum == EitherEnum.Success)
            {
                var eitherSuccessField =
                    responseResultType.GetField("Success", BindingFlags.NonPublic | BindingFlags.Instance);
                var eitherSuccess = eitherSuccessField!.GetValue(response) !;
                var result = JsonConvert.SerializeObject(eitherSuccess, new JsonSerializerSettings
                {
                    ContractResolver = new IgnorePropertiesResolver(new[]
                    {
                        "LogInterceptorExcludeAttribute",
                    }),
                });
                return result;
            }

            if (eitherEnum == EitherEnum.IsFaulted)
            {
                var eitherErrorField =
                    responseResultType.GetField("faulted", BindingFlags.NonPublic | BindingFlags.Instance);

                var eitherException = (EitherException)eitherErrorField!.GetValue(response) !;


                var eitherExceptionCollection = eitherException.ToICollection();

                var eitherExceptionResult = JsonConvert.SerializeObject(eitherExceptionCollection, new JsonSerializerSettings
                {
                    ContractResolver = new IgnorePropertiesResolver(new[]
                    {
                        "LogInterceptorExcludeAttribute",
                    }),
                });
                return eitherExceptionResult;
            }

            var eitherErrorField2 =
                responseResultType.GetField("EitherError", BindingFlags.NonPublic | BindingFlags.Instance);

            var eitherError = (EitherErrorMessage)eitherErrorField2!.GetValue(response) !;

            return eitherError;
        }

        /// <summary>
        ///     Determines whether [is simple type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [is simple type] [the specified type]; otherwise, <c>false</c>.</returns>
        private bool IsSimpleType(Type type)
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
        ///     The ignore props.
        /// </summary>
        private readonly HashSet<string> ignoreProps;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IgnorePropertiesResolver" /> class.
        /// </summary>
        /// <param name="propNamesToIgnore">The property names to ignore.</param>
        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            this.ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        /// <summary>
        ///     Creates the property.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="memberSerialization">The member serialization.</param>
        /// <returns>JsonProperty.</returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var customInterceptorExcludeAttribute = member.GetCustomAttributes(typeof(LogInterceptorExcludeAttribute));
            var property = base.CreateProperty(member, memberSerialization);
            if (customInterceptorExcludeAttribute.Count() >= 1)
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
}