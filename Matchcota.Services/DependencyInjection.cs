using Matchcota.Services.Auth;
using Matchcota.Services.Chat;
using Matchcota.Services.Discovery;
using Matchcota.Services.Dogs;
using Matchcota.Services.Safety;
using Microsoft.Extensions.DependencyInjection;

namespace Matchcota.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<ISwipeService, SwipeService>();
        services.AddScoped<IDogsService, DogsService>();
        services.AddScoped<ISafetyService, SafetyService>();
        return services;
    }
}
