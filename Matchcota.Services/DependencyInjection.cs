using Matchcota.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Matchcota.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
