namespace Spoon.NuGet.Interceptors.LogInterceptor;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

/// <summary>
/// Class LogInterceptorDefaultLogLevelAttribute.
/// Implements the <see cref="System.Attribute" />.
/// </summary>
/// <seealso cref="System.Attribute" />
public class LogInterceptorDefaultLogLevelAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogInterceptorDefaultLogLevelAttribute"/> class.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    public LogInterceptorDefaultLogLevelAttribute([Required] LogLevel logLevel)
    {
        this.LogLevel = logLevel;
    }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    /// <value>The log level.</value>
    public LogLevel LogLevel { get; }
}
