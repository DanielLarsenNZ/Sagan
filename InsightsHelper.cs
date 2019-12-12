using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sagan
{
    public static class InsightsHelper
    {
        public static TelemetryClient InitializeTelemetryClient(string iKey)
        {
            // Create the DI container.
            IServiceCollection services = new ServiceCollection();
            services.AddApplicationInsightsTelemetryWorkerService(iKey);

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<TelemetryClient>();
        }
    }
}
