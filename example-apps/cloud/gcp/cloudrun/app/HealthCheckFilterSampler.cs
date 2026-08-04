using OpenTelemetry.Trace;

/// <summary>
/// If you wish to prevent traces on AspNetCore instrumentation for healthchecks
/// </summary>
public class HealthCheckFilteringSampler : Sampler
{
    private static readonly string[] ExcludedPatterns = { "/healthcheck", "/health", "/metrics" };
    private readonly Sampler _rootSampler;

    public HealthCheckFilteringSampler(Sampler rootSampler)
    {
        _rootSampler = rootSampler;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        foreach (var tag in samplingParameters.Tags ?? Enumerable.Empty<KeyValuePair<string, object?>>())
        {
            if ((tag.Key == "url.full" || tag.Key == "http.route" || tag.Key == "http.url")
                && tag.Value is string value
                && ExcludedPatterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                return new SamplingResult(SamplingDecision.Drop);
            }
        }

        return _rootSampler.ShouldSample(in samplingParameters);
    }
}
