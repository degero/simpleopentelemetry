# Azure monitor OTLP endpoints (March 2026 in preview)

## Infra

Follow steps to create an Appinsights / azure monitor workspace with OTLP endpoints
https://learn.microsoft.com/en-us/azure/azure-monitor/containers/opentelemetry-protocol-ingestion


## Setup (local dev use)

- dotnet add package Azure.Identity
- Get endpoints from Appinsights Overview / Essentials pane 'OTLP connection info' link add these to cofig (see sample otlp expoerter config.json as guidance)
- In same side blade with endpoints, click the 'Data collection rule' link. Add RBAC role assignemnt 'Monitorig Metrics Publisher' to your local logged in Azure cli / Visual studio azure account

## View metrics / logs / traces

### Metrics

Appinsights -> Monitoring -> Metrics

### Traces

Appinsights -> Investigate -> Transactions search

### Logs

Appinsights -> Monitoring -> Logs

KQL
```
requests
| where timestamp > ago(1h)
| order by timestamp desc

traces
| where timestamp > ago(1h)
| order by timestamp desc

customMetrics
| where timestamp > ago(1h)
| order by timestamp desc
```

## Credit

https://medium.com/@rajkumar.rangaraj/send-opentelemetry-data-directly-to-application-insights-using-the-otlp-exporter-no-collector-96ddfe1c3c78

