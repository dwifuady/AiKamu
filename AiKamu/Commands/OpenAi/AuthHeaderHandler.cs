using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AiKamu.Commands.OpenAi;

public class AuthHeaderHandler(IOptions<OpenAiConfig> options) : DelegatingHandler
{
    private readonly OpenAiConfig _config = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}