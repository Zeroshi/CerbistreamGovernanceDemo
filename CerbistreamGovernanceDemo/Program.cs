// CerbiStream Multi-Scenario Demo
// Requires NuGet packages: CerbiStream, Cerbi.Governance.Runtime

using Cerbi.Governance;
using CerbiStream;
using CerbiStream.Configuration;
using CerbiStream.Extensions;
using CerbiStream.Classes.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[CerbiTopic("Orders")]
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddCerbiStream(options =>
            {
                // Option 1: Azure Service Bus with telemetry and retries
                options.WithQueue("AzureServiceBus", "Endpoint=sb://yourbus.servicebus.windows.net/", "orders-queue")
                       .WithTelemetryProvider(TelemetryProviderFactory.CreateTelemetryProvider("appinsights"))
                       .WithQueueRetries(true, 3, 200)
                       .WithEncryptionMode(CerbiStream.Interfaces.IEncryptionTypeProvider.EncryptionType.Base64)
                       .WithFileFallback("logs/fallback.json")
                       .WithGovernanceChecks(true)
                       .EnableDeveloperModeWithTelemetry()
                       .WithAdvancedMetadata(true)
                       .WithSecurityMetadata(true);

                // Option 2 (Uncomment to switch): Kafka with AES encryption and fallback
                // options.WithQueue("Kafka", "localhost:9092", "logs-topic")
                //        .WithEncryptionMode(EncryptionType.AES)
                //        .WithEncryptionKey(Convert.FromBase64String("<base64key>"), Convert.FromBase64String("<base64iv>"))
                //        .WithFileFallback("logs/fallback_kafka.json")
                //        .WithGovernanceChecks(true)
                //        .EnableProductionMode();

                // Option 3 (Uncomment to simulate HTTP endpoint logging with no telemetry)
                // options.WithDisableQueue(true)
                //        .WithGovernanceChecks(true)
                //        .WithEncryptionMode(EncryptionType.None)
                //        .EnableDeveloperModeWithoutTelemetry();
            });
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<Program>>();

        // ✅ Normal structured log (passes governance)
        logger.LogInformation("New Order Placed", new Dictionary<string, object>
        {
            ["userId"] = "u123",
            ["email"] = "user@example.com"
        });
        // Output includes default metadata:
        // {
        //   "userId": "u123",
        //   "email": "user@example.com",
        //   "GovernanceProfileUsed": "Orders",
        //   "TimestampUtc": "<utc time>",
        //   "LogLevel": "Information",
        //   "CloudProvider": "Azure",
        //   "Region": "eastus",
        //   "InstanceId": "<machine>",
        //   "ApplicationId": "MyApp",
        //   "ApplicationVersion": "1.2.3",
        //   "ServiceName": "<optional>",
        //   "OriginApp": "<optional>",
        //   "TraceId": "<if available>",
        //   "SpanId": "<if available>",
        //   "ParentSpanId": "<if available>"
        // }

        // ❌ Log with violation (missing required fields)
        logger.LogWarning("Order Attempted", new Dictionary<string, object>
        {
            ["note"] = "missing userId"
        });
        // Output includes GovernanceViolations and all standard metadata

        // ✅ Relaxed log (bypasses governance but is tagged)
        logger.Relax().LogInformation("Emergency log without metadata");
        // Output includes:
        // "GovernanceRelaxed": true

        // ✅ Log with manual profile injection
        logger.LogInformation("Manual profile injection", new Dictionary<string, object>
        {
            ["GovernanceProfileUsed"] = "Orders",
            ["userId"] = "u456",
            ["email"] = "backup@example.com"
        });

        // ✅ Dynamic dictionary payload
        var dynamic = new Dictionary<string, object>
        {
            ["userId"] = "u999",
            ["email"] = "dynamic@demo.com",
            ["requestId"] = Guid.NewGuid().ToString()
        };
        logger.LogInformation("Dynamic user event", dynamic);

        // ✅ Encrypted fallback log (simulating failure)
        logger.LogError("Order failed", new Dictionary<string, object>
        {
            ["userId"] = "u111",
            ["email"] = "fail@demo.com",
            ["error"] = "Timeout"
        });
        // Output: AES encrypted JSON written to fallback.json with enriched metadata

        // ✅ Simulated failover by disabling queue
        logger.LogCritical("Failover simulation", new Dictionary<string, object>
        {
            ["userId"] = "u777",
            ["email"] = "failover@example.com"
        });
        // Output: Log written to fallback, includes cloud + tracing + governance metadata

        await Task.Delay(1000); // flush
    }
}
