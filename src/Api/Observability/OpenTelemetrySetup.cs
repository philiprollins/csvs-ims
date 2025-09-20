namespace Api.Observability;

public static class OpenTelemetrySetup
{
    public static IServiceCollection AddMinimalOpenTelemetry(this IServiceCollection services, string serviceName)
    {
        // Keep minimal and optional; can be expanded later.
        // Example wiring (commented to avoid forcing package dependencies):
        // services.AddOpenTelemetry()
        //     .WithMetrics(b => b.AddAspNetCoreInstrumentation())
        //     .WithTracing(b => b.AddAspNetCoreInstrumentation());
        return services;
    }
}
