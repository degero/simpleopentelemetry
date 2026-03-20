using Azure.Identity;

public class AzureMonitorHttpClientFactory
{
    DefaultAzureCredential _credential;
    public AzureMonitorHttpClientFactory()
    {
        // Create credential — uses az login locally, managed identity on Azure
        _credential = new DefaultAzureCredential();
    }

    public HttpClient HttpClientFactory() => new HttpClient(new AzureMonitorAuthHandler(_credential));
}