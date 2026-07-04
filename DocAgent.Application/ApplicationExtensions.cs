using Microsoft.Extensions.DependencyInjection;
using DocAgent.Core.Interfaces;
using DocAgent.Application.Services;

namespace DocAgent.Application;

/// <summary>
/// Extension methods for application dependency injection
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Register application services
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }
}
