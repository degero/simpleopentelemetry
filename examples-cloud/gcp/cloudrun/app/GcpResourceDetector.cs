using OpenTelemetry.Resources;

public static class GcpResourceDetector
{
    public static string? DetectInstanceId()
    {
        return FetchCloudRunInstanceIdAsync().GetAwaiter().GetResult();
    }

    private static readonly HttpClient MetadataClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    private static async Task<string?> FetchCloudRunInstanceIdAsync()
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://metadata.google.internal/computeMetadata/v1/instance/id");
            request.Headers.Add("Metadata-Flavor", "Google");

            using var response = await MetadataClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }
}