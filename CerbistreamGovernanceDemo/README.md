# CerbiStream: Developer-Friendly, Governance-Enforced Logging for .NET

CerbiStream is a **structured logging and governance layer** for .NET.  
It gives you:

- Consistent, structured logs
- Safe delivery to queues, files, and telemetry backends
- **Runtime governance enforcement** for PII-safe logging and policy compliance

You keep your existing logging stack (MEL, Serilog adapters, OTEL, etc.) and let CerbiStream handle **routing, encryption, fallback, and governance**.

---

## CerbiSuite Components

CerbiStream is part of the broader **CerbiSuite**:

- **CerbiStream** – Structured logging for .NET with governance, queues, and cloud targets
- **Cerbi.Governance.Runtime** – Runtime enforcement of governance rules
- **CerbiStream.GovernanceAnalyzer** – Compile-time governance analyzer (Roslyn)
- **CerbiShield** (coming soon) – Governance dashboard and deployment portal
- **CerbIQ** (coming soon) – Metadata aggregation and routing pipeline
- **CerbiSense** (coming soon) – Governance scoring and ML analysis engine

---

## Getting Started

### Installation

```bash
dotnet add package CerbiStream
````

### Minimal Setup (Dev)

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal();
});
```

This config:

* Uses console output
* Skips governance checks
* Minimizes extra metadata

Perfect for local development and quick experiments.

---

## Configuration Options (Fluent API Overview)

CerbiStream exposes a fluent configuration API designed to be **discoverable in IntelliSense** and **safe to turn on/off in production**.

| Method                                          | Description                                                | Example                                                                   | When to Use                                    |
| ----------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------- | ---------------------------------------------- |
| `WithQueue(type, host, name)`                   | Configure which message queue to send logs to.             | `.WithQueue("RabbitMQ", "localhost", "logs-queue")`                       | Structured delivery to centralized systems.    |
| `WithDisableQueue(true)`                        | Disable queue delivery.                                    | `.WithDisableQueue(true)`                                                 | File-only / HTTP-only / debugging scenarios.   |
| `WithTelemetryProvider(provider)`               | Send logs to a telemetry backend.                          | `.WithTelemetryProvider(CreateTelemetryProvider("appinsights"))`          | Centralized metrics & tracing.                 |
| `WithQueueRetries(enable, count, delayMs)`      | Add retry logic for queue delivery (Polly under the hood). | `.WithQueueRetries(true, 3, 200)`                                         | Handle transient queue failures.               |
| `WithFileFallback(path)`                        | Log to JSON file if queue delivery fails.                  | `.WithFileFallback("logs/fallback.json")`                                 | Guarantee logs aren’t lost during outages.     |
| `WithEncryptedFallback(fallback, primary, ...)` | Encrypted file fallback + rotation.                        | `.WithEncryptedFallback("fallback.json", "primary.json", key, iv)`        | Protect logs at rest in fallback mode.         |
| `WithEncryptionMode(mode)`                      | Encrypt payload before transport.                          | `.WithEncryptionMode(EncryptionType.AES)`                                 | PII / PHI / sensitive data paths.              |
| `WithEncryptionKey(key, iv)`                    | Provide AES key and IV.                                    | `.WithEncryptionKey(keyBytes, ivBytes)`                                   | Required for AES mode.                         |
| `WithGovernanceChecks(true)`                    | Enable runtime governance validation.                      | `.WithGovernanceChecks(true)`                                             | Core Cerbi governance enforcement.             |
| `WithGovernanceValidator(delegate)`             | Custom validation delegate (advanced override).            | `.WithGovernanceValidator((profile, meta) => meta.ContainsKey("userId"))` | Very specific rule overrides or experiments.   |
| `WithMetadataInjection(true)`                   | Inject timestamp, level, etc.                              | `.WithMetadataInjection(true)`                                            | Strong default for all environments.           |
| `WithAdvancedMetadata(true)`                    | Add cloud info, region, app ID, etc.                       | `.WithAdvancedMetadata(true)`                                             | Full observability and cross-app correlation.  |
| `WithSecurityMetadata(true)`                    | Add security/redaction metadata.                           | `.WithSecurityMetadata(true)`                                             | Compliance/security review workflows.          |
| `WithTracingEnrichment(true)`                   | Add `TraceId`, `SpanId`, `ParentSpanId`.                   | `.WithTracingEnrichment(true)`                                            | Lightweight distributed tracing.               |
| `WithApplicationIdentity(appType, serviceType)` | Attach logical app/service identifiers.                    | `.WithApplicationIdentity("WebApp", "PaymentService")`                    | Business-context classification.               |
| `WithTargetSystem(appType, serviceType)`        | Identify dependencies / downstream targets.                | `.WithTargetSystem("Worker", "OrderQueue")`                               | Cross-service dependency mapping.              |
| `WithTelemetryLogging(true)`                    | Also send logs to telemetry provider.                      | `.WithTelemetryLogging(true)`                                             | Dual-destination delivery (queue + telemetry). |
| `WithConsoleOutput(true)`                       | Mirror logs to console.                                    | `.WithConsoleOutput(true)`                                                | Dev and diagnostic scenarios.                  |

---

## Core Configuration Patterns

### Queue Configuration

```csharp
options.WithQueue("AzureServiceBus", "Endpoint=sb://...", "log-queue");
```

Supports patterns like:

* Azure Service Bus
* RabbitMQ
* Kafka
* AWS SQS
* Google Pub/Sub

(Transport specifics are handled via your own queue senders / adapters behind the scenes.)

---

### Telemetry Integration

```csharp
options.WithTelemetryProvider(
    TelemetryProviderFactory.CreateTelemetryProvider("appinsights")
);
```

Typical backends:

* Azure Application Insights
* OpenTelemetry exporters
* Datadog
* AWS CloudWatch
* GCP Operations (Stackdriver)
* Other MEL-compatible telemetry providers

---

### Encryption

```csharp
options
    .WithEncryptionMode(EncryptionType.AES)
    .WithEncryptionKey(key, iv);
```

Modes:

* `None`
* `Base64`
* `AES` (recommended for log-at-rest protection)

---

### File Fallback

```csharp
options.WithFileFallback("logs/fallback.json");
```

If queue/telemetry delivery fails, logs are written locally, optionally encrypted:

```json
[ENCRYPTED]<base64-or-AES-payload>[/ENCRYPTED]
```

---

### Governance Enforcement

```csharp
options.WithGovernanceChecks(true);
```

This wires CerbiStream into **Cerbi.Governance.Runtime** to:

* Validate logs against governance profiles
* Redact forbidden/disallowed fields
* Tag events with governance metadata (violations, mode, version, relaxed flag)

---

### Metadata Injection

```csharp
options
    .WithMetadataInjection(true)
    .WithAdvancedMetadata(true)
    .WithSecurityMetadata(true);
```

Typical injected fields include:

* `TimestampUtc`
* `LogLevel`
* `ApplicationId`
* `InstanceId`
* `CloudProvider`
* `Region`
* Security/governance tags

---

## Governance and Compliance

CerbiStream is built for **compliance-oriented logging**:

* **Runtime Enforcement**
  Uses `Cerbi.Governance.Runtime` to validate each log against a profile (e.g., required fields, forbidden/PII fields, disallowed keys).

* **Relaxed Logging**
  You can explicitly bypass enforcement for targeted diagnostics:

  ```csharp
  logger.Relax().LogInformation("This log bypasses governance checks.");
  ```

  The event is tagged as relaxed (for audit and scoring) but not blocked/redacted.

* **Topic Tagging**
  Assign logical topics via attribute:

  ```csharp
  [CerbiTopic("Orders")]
  public class OrderProcessor { }
  ```

  Profiles can be topic-aware (`RequireTopic`, `AllowedTopics`, etc.).

---

## Usage Examples

### Relaxed Logging

```csharp
logger.Relax().LogInformation("This log bypasses governance checks.");
```

Result (illustrative):

```json
{
  "Message": "This log bypasses governance checks.",
  "GovernanceRelaxed": true,
  "TimestampUtc": "2025-05-19T03:21:15Z",
  "LogLevel": "Information",
  "CloudProvider": "Azure",
  "Region": "eastus",
  "InstanceId": "machine-xyz",
  "ApplicationVersion": "1.2.3"
}
```

---

### Topic Assignment

```csharp
[CerbiTopic("Payments")]
public class PaymentProcessor
{
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(ILogger<PaymentProcessor> logger)
    {
        _logger = logger;
    }
}
```

Logs from this class will be evaluated under the `Payments` profile when configured.

---

### Valid Structured Log

```csharp
logger.LogInformation("New Order", new Dictionary<string, object>
{
    ["userId"] = "u123",
    ["email"] = "user@example.com"
});
```

Possible output:

```json
{
  "Message": "New Order",
  "userId": "u123",
  "email": "user@example.com",
  "GovernanceProfileUsed": "Orders",
  "TimestampUtc": "2025-05-19T03:22:45Z",
  "LogLevel": "Information"
}
```

---

### Violation Example (Missing Required Fields)

```csharp
logger.LogWarning("Login Failed", new Dictionary<string, object>
{
    ["note"] = "userId missing"
});
```

Illustrative governed output:

```json
{
  "Message": "Login Failed",
  "note": "userId missing",
  "GovernanceViolations": [ "Missing: userId" ],
  "GovernanceMode": "Strict",
  "TimestampUtc": "2025-05-19T03:23:11Z",
  "LogLevel": "Warning"
}
```

---

## 🚀 Preset Modes

To avoid “option soup,” CerbiStream ships with preset modes:

```csharp
options.EnableDevModeMinimal();                // Console only, minimal metadata, governance off
options.EnableDeveloperModeWithoutTelemetry(); // Metadata on, telemetry off, governance usually off
options.EnableDeveloperModeWithTelemetry();    // Metadata + telemetry, governance optional
options.EnableProductionMode();                // Full governance, encryption, telemetry
options.EnableBenchmarkMode();                 // No external outputs, tuned for perf testing
```

Use these as safe starting points, then override as needed.

---

## Benchmarking

CerbiStream includes a dedicated benchmark suite.

* **Performance**: Competitive with established loggers in baseline scenarios.
* **Governance overhead**: Explicit and measurable, especially for redaction-heavy rules.
* **Memory usage**: Optimized via pooling and streaming parsers.

See:

* [CerbiStream Benchmark Tests](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)

Run locally:

```bash
dotnet run -c Release --project CerbiStream.BenchmarkTests/CerbiStream.BenchmarkTests.csproj
```

(Adjust path to match your repo layout.)

---

## Contributing

Contributions are welcome.

* File issues for bugs, feature requests, or integration examples.
* See `CONTRIBUTING.md` for guidelines.

---

## License

MIT License — see the `LICENSE` file.

---

For more information, visit the main repo:

👉 **[https://github.com/Zeroshi/Cerbi-CerbiStream](https://github.com/Zeroshi/Cerbi-CerbiStream)**

