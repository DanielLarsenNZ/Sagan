using Microsoft.ApplicationInsights;
//using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using System;
//using Microsoft.ApplicationInsights.TraceListener;
//using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using System.Diagnostics;

namespace Sagan
{
    public static class InsightsHelper
    {
        //private readonly static TraceSource traceSource = new TraceSource("ConsoleApp5", SourceLevels.All);

        public static TelemetryClient InitializeTelemetryClient(string iKey)
        {
            // Create the DI container.
            IServiceCollection services = new ServiceCollection();

            // Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
            // Hence instrumentation key must be specified here.
            services.AddApplicationInsightsTelemetryWorkerService(iKey);

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Obtain logger instance from DI.
            //ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Obtain TelemetryClient instance from DI, for additional manual tracking or to flush.
            return serviceProvider.GetRequiredService<TelemetryClient>();








            /*

            //traceSource.Listeners.Add(new ApplicationInsightsTraceListener());

            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.InstrumentationKey = iKey;

            var insights = new TelemetryClient(telemetryConfig);

            //insights.TrackTrace("Examples.Pipeline.MessageGenerator.Main");
            //var module = new DependencyTrackingTelemetryModule();
            //module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            //module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            //module.Initialize(telemetryConfig);
            //telemetryConfig.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            //QuickPulseTelemetryProcessor processor = null;
            //telemetryConfig.TelemetryProcessorChainBuilder
            //    .Use((next) =>
            //    {
            //        processor = new QuickPulseTelemetryProcessor(next);
            //        return processor;
            //    })
            //    .Build();

            //var QuickPulse = new QuickPulseTelemetryModule();
            //QuickPulse.Initialize(telemetryConfig);
            //QuickPulse.RegisterTelemetryProcessor(processor);

            //insights.Context.Cloud.RoleName = cloudRoleName;
            //insights.Context.Cloud.RoleInstance = cloudRoleInstance;

            return insights;

            */
        }
    }
}
