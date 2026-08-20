# Google OpenTelemetry Distro collector configurations

These configs are catered for OpenTelemetry sent from Dotnet apps and to be used with the `google-cloud-opentelemetry-collector` docker image `us-docker.pkg.dev/cloud-ops-agents-artifacts/google-cloud-opentelemetry-collector/otelcol-google` (currently 0.151.0) [GitHub google-built-opentelemetry-collector](https://github.com/GoogleCloudPlatform/opentelemetry-operations-collector/tree/master/google-built-opentelemetry-collector)

_IMPORTANT_: The collector and its resource detector `gcp` does not set a core resource attribute `service.instance.id` but `faas.instance` as an instanceid. for normalised standard portable telemetry the resource/instance id processor has been added to these files.

## Config files

_NOTE_: At the time of writing (July 2026) google's documented samples use the legacy exporters but is moving towards OTLP. Google recommends to use the OTLP exporter for metrics as stated here [Get started with the OpenTelemetry Collector](https://docs.cloud.google.com/stackdriver/docs/managed-prometheus/setup-otel). For tracing most examples export with OTLP. Only OTLP logs remain pre-GA. It is recommended to use the legacy logs export for PROD until logs OTLP goes GA. Check the guide here to see if it is now GA [OTLP log ingestion overview](https://docs.cloud.google.com/stackdriver/docs/otlp-logs/overview).

- otelcollector-cloudrun-legacyexport.yaml - this uses google's exporters for logs and metrics, has a healthcheck, tail sampling example (for using as a central collector) and sends collector metrics (3 core signals).

- otelcollector-cloudrun-otlpexport.yaml - this uses OTLP http/protobuf for all signals to google's newer OTLP endpoints https://telemetry.googleapis.com, has a healthcheck, tail sampling example (for using as a central collector) and sends collector metrics (3 core signals). You can adopt the `googlecloud` log exporter from the above legacy file if not.

- otelcollector-local.yaml - for local docker use, tail sampling omitted. It defaults to OTLP export to google but commented sections to use legacy

### Environment variables

These configs use the following env vars:

- GOOGLE_CLOUD_PROJECT (the short name of your google project)
- GOOGLE_CLOUD_LOG_NAME (log name to go under the structure `projects/{GOOGLE_CLOUD_PROJECT}/logs/`)

For `otelcollector-cloudrun-otlpexport.yaml`, if you would like to use the default 'otlp' log name google sets for the OTLP export, remove `GOOGLE_CLOUD_LOG_NAME` use and the `transform/log_name`. `otelcollector-cloudrun-legacyexport.yaml` requires a `GOOGLE_CLOUD_LOG_NAME`

## Google platform features, differences and constraints

- .Net ILogger.LogInformation() sets a severity_text as "Information" which GCP will drop, included is `transform/fix_severity` to set the google expected severity_text (INFO) as well as the normal expected UPPERCASE for all others

- GCP does not have a severity_text of "TRACE" so any .Net ILogger.LogTrace appears as "DEBUG"

- You can optionally make use of Google's [Structured logging](https://cloud.google.com/logging/docs/structured-logging) for quering logs by log parameters. Enabling with either `transform/conditional_logs_structured` or `transform/conditional_logs_structured_fromformatted`, the latter applies when you have `IncludeFormattedMessage` set as 'true' on the app otel SDK use.

- Metrics via OTLP require a start_time or they will be dropped

- Metrics are transformed so you cannot resource query attributes as you would a prometheus store. Resource attributes go into `target_info` see [metric mapping](https://docs.cloud.google.com/stackdriver/docs/reference/telemetry/v1.metrics#metric-mapping)

- Cloud run has its own request trace it inserts as a parent to your app and sampling rules which can conflict with any sampling settings you set on the app ([cloud run tracing docs](https://cloud.google.com/run/docs/trace)). There is no option to disable (see issue link at the bottom). The .NET OTel SDK's trace instrumentation correctly extracts this header and parents the service's root span to it, but Cloud Run's root span is never exposed as an exportable OTLP span, it may appear as a "Missing Span" ([trace context docs](https://docs.cloud.google.com/trace/docs/trace-context)).

## Local use

You can run the otelcollector-cloudrun-\*.yaml files locally to test. Just add `endpoint: 0.0.0.0:4317` under `grpc:` and add the `resource/required_labels` definition from otelcollector-local.yaml. For otelcollector-cloudrun-legacyexport.yaml a `location` attribute of your project location needs to be added to metrics.

## Production tips

- Be sure to adjust memory limiter and GOMEMLIMIT env var to your needs/hosting constraints. See [memory_limiter best practices](https://github.com/open-telemetry/opentelemetry-collector/blob/main/processor/memorylimiterprocessor/README.md#best-practices))

- Adjust collector self-metrics or remove if you do not need for alerting / dashboards. For sample alerting see [Github monitoringartist/opentelemetry-collector-monitoring](https://github.com/monitoringartist/opentelemetry-collector-monitoring)

- If looking to scale, consider an alternate delivery structure for telemetry to a 'central' collector hosted on a separate cloudrun deployment

- Check your trace volume and decide which head sampling rule to use and if you want to use an additional separate 'centralized' cloud run collector with tail sampling included in the configs. You may wish to use an alternate head sampling env var setting (eg alwayson) to avoid app traces dropped by Cloud runs parent request trace and sampling rules.

- OpenTelemetry is constantly changing as is Google's use of it. Be sure to verify your telemetry is coming through correctly in lower environments (eg. with correct attributes)

- Cloud Run automatically generates traces and has sampling enabled by default which may affect app traces when using parent based sampling, for details see [Using distributed tracing](https://docs.cloud.google.com/run/docs/trace).

- These configs are specifiably setup for use as a sidecar in Google CloudRun. If you are sending Telemetry to google from an external hosted platforms Google documentation recommends their [BindPlane](https://docs.cloud.google.com/logging/docs/bindplane/on-premise-hybrid-logging)

- Cloudrun defaults to capture in separate logs: stdout, stderr and requests. You may want to adjust if these are sampled or captured at all.

## Further information on Cloud Run distributed tracing

https://issuetracker.google.com/issues/363032992

https://discuss.google.dev/t/google-cloud-trace-is-missing-all-spans-from-cloud-load-balancer/147087

https://medium.com/@vladislavmarkevich/distributed-tracing-cloudrun-6a3bac9d165a
