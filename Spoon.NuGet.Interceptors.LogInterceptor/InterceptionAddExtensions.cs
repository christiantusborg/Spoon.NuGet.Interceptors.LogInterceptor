namespace Spoon.NuGet.Interceptors.LogInterceptor;

using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Class InterceptionExtensions.
/// </summary>
public static partial class InterceptionExtensions
{
    /// <summary>
    /// Adds the intercepted singleton.
    /// </summary>
    /// <typeparam name="TInterface">The type of the t interface.</typeparam>
    /// <typeparam name="TImplementation">The type of the t implementation.</typeparam>
    /// <typeparam name="TInterceptor">The type of the t interceptor.</typeparam>
    /// <param name="services">The services.</param>
    public static void AddInterceptedSingleton<TInterface, TImplementation, TInterceptor>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TInterceptor : class, IInterceptor
    {
        services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddSingleton<TImplementation>();
        services.TryAddTransient<TInterceptor>();
        services.AddSingleton(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var impl = provider.GetRequiredService<TImplementation>();
            var interceptor = provider.GetRequiredService<TInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(impl, interceptor);
        });
    }
    
    /// <summary>
    /// Adds the intercepted singleton.
    /// </summary>
    /// <typeparam name="TInterface">The type of the t interface.</typeparam>
    /// <typeparam name="TImplementation">The type of the t implementation.</typeparam>
    /// <typeparam name="TInterceptor">The type of the t interceptor.</typeparam>
    /// <param name="services">The services.</param>
    public static void AddInterceptedTransient<TInterface, TImplementation, TInterceptor>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TInterceptor : class, IInterceptor
    {
        services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddTransient<TImplementation>();
        services.TryAddTransient<TInterceptor>();
        services.AddTransient(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var impl = provider.GetRequiredService<TImplementation>();
            var interceptor = provider.GetRequiredService<TInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(impl, interceptor);
        });
    }    
}