namespace Spoon.NuGet.Interceptors.LogInterceptor;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface IInterceptorAttach.
/// </summary>
public interface IInterceptorAttach
{
    /// <summary>
    /// Attaches the specified services.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>IServiceCollection.</returns>
    IServiceCollection Attach(IServiceCollection services);
}