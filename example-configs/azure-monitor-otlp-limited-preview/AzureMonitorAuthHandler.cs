using System.Net.Http.Headers;
using Azure.Core;

/// <summary>
/// Injects a Microsoft Entra ID Bearer token into every outgoing HTTP request.
/// This is the .NET equivalent of the OTel Collector's azureauthextension.
/// </summary>
internal sealed class AzureMonitorAuthHandler : DelegatingHandler
{
    private readonly TokenCredential credential;
    private readonly string[] scopes = ["https://monitor.azure.com/.default"];

    public AzureMonitorAuthHandler(TokenCredential credential)
    {
        this.credential = credential;
        InnerHandler = new HttpClientHandler();
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResult = credential.GetToken(
            new TokenRequestContext(scopes), cancellationToken);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        
        HttpResponseMessage? message = null;
        try
        {
            message = base.Send(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Write("asdfa");
        }

        return message;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResult = await credential.GetTokenAsync(
            new TokenRequestContext(scopes), cancellationToken)
            .ConfigureAwait(false);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResult.Token);

        HttpResponseMessage? message = null;
        try
        {
            message = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Write("asdfa");
        }

        return message;
    }
}