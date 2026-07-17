using Matchcota.Services.Auth;
using Matchcota.Services.Discovery;
using Matchcota.Services.Dogs;
using Microsoft.Extensions.DependencyInjection;

namespace Matchcota.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<ISwipeService, SwipeService>();
        services.AddScoped<IDogsService, DogsService>();
        return services;
    }
}
