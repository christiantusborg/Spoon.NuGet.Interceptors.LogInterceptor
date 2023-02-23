namespace Spoon.NuGet.Interceptors.LogInterceptor;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Class InterceptionExtensions.
/// </summary>
public static partial class InterceptionExtensions
{
    /// <summary>
    /// Attaches the intercepted singleton.
    /// </summary>
    /// <param name="services">The services.</param>
    public static void AttachInterceptedSingleton(this IServiceCollection services)
    {
        var logInterceptorAttachType = typeof(IInterceptorAttach);
        var providers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => logInterceptorAttachType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)

            .Select(x =>
            {
                return Activator.CreateInstance(x);
            })
            .Cast<IInterceptorAttach>().ToList();

        foreach (var logInterceptorAttach in providers)
        {
            services = logInterceptorAttach.Attach(services);
        }
    }
}