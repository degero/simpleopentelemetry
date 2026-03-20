# SimpleOpenTelemetry

A lightweight, .Net Generic Host / Web Core Host (I) .NET library for simplified OpenTelemetry integration. Abstracts the complexity of OpenTelemetry configuration through supporting multiple exporters, easy metrics/tracing instrumentation for many platforms and logging settings. This is not autoinstrumentation, but a low-code alternative to OpenTelemetryBuilder with some added configuration features.

## Overview

SimpleOpenTelemetry provides a straightforward way to add distributed tracing to .NET applications with minimal setup. It handles the boilerplate configuration of OpenTelemetry and includes built-in support for popular exporters and instrumentation types.

**Supported Frameworks:** .NET 8.0, .NET 10.0
**License:** MIT

---

## Features

- Sets OTEL_RESOURCE_ATTRIBUTES 'service.version' from app assembly version if one is not provided in env var / config

---

## Monitoring your apps

### Distributed tracing

For an example of all the dotnet tracing features see (MSLearn - Adding distributed tracing instrumentation)[https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs]

---

## License

MIT License - see LICENSE file for details

---

## Support

For issues, feature requests, or contributions, visit:
https://github.com/degero/SimpleOpenTelemetry
