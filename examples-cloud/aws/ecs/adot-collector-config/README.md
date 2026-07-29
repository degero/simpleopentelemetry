# AWS ECS ADOT collector sidecar config examples

These configs only support gRPC OTLP receivers as to work with manual opentelemetry app instrumentation and not AWS SDK telemetry like the xray library. They are designed for PRODUCTION use with trace filters, memory limit, collector telemetry (core signals for alerting) and tail sampling to reduce load/cost of telemetry.

ECS configs in (Github aws-observability/aws-otel-collector)[https://github.com/aws-observability/aws-otel-collector/tree/main/config/ecs] and OpenTelemetry Exporters to Cloudwatch OTLP endpoints (Collector config examples) [https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPSimplesetup.html] were used as guides for these configs.

## Env vars

These configs require the following ENV vars:

- AWS_REGION
- LOG_GROUP_NAME
- LOG_GROUP_STREAM
- COLLECTOR_MEM_LIMIT_MB
- COLLECTOR_SPIKE_LIMIT_MB
- GOMEMLIMIT

Additionally adotcollector-ecs-legacyexport.yml requires:

- LOG_RETENTION_DAYS
- METRIC_RETENTION_DAYS


## Important configurable sections

### Deployment resources

You can see AWS sample sidecar deployment templates [here](https://github.com/aws-observability/aws-otel-collector/tree/main/deployment-template) for guidance on mem / cpu capacity.


### Memory limit

Set COLLECTOR_MEM_LIMIT / COLLECTOR_SPIKE_LIMIT as needed for your app. For PRODUCTION envs, adjust for best practices and your telemetry volume / hosting footprint allows. Note if using Tail sampling over Xray Remote sampling it has a high impact on memory usage. This 'memory_limiter' processor's best practice is to also have an env var 'GOMEMLIMIT' as 80% of COLLECTOR_MEM_LIMIT 


### Sampling 

By default this config is setup for using AWS XRay remote Sampling. Included is a commonly used 'tail_sampling' setup, use this or some other sampling configuration if you don't have sampling setup on the application side OpenTelemetry setup (eg XRay Remote Sampling, ratio based etc). If you are not using XRay sampling remove the 'awsproxy' extension. If using XRay Remote Sampling, ensure you have [setup your sampling rules](https://docs.aws.amazon.com/xray/latest/devguide/xray-console-sampling.html#xray-console-custom). 

Further information on the sampler config in adot collector had be found in [Configuring the OpenTelemetry Collector for X-Ray remote Sampling](https://aws-otel.github.io/docs/getting-started/remote-sampling)


## Trace filtering

These are set under 'filter/traces:' to filter out http client traces on xray sampler and healthchecks. Add any other http requests you don't want telemetry for if you have http client trace instrumentation enabled on your app
