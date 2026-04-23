using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FirstBrick.Shared.ErrorHandling;

public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Registers the shared <see cref="GlobalExceptionHandler"/> plus
    /// <c>AddProblemDetails()</c> so framework-generated 4xx responses
    /// (401, 404, 415, …) also serialize as <c>application/problem+json</c>.
    /// </summary>
    public static IServiceCollection AddFirstBrickErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// Plugs the exception handler middleware into the pipeline.
    /// Call this before any other middleware so every downstream exception is caught.
    /// </summary>
    public static IApplicationBuilder UseFirstBrickErrorHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
