using Azure.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json.Serialization;
using System.Web;

namespace SecureMultiTenantApp.SecurityNL
{
    public class TokenBridge
    {
        private readonly ILogger<TokenBridge> _logger;
        private readonly TokenCredential _tokenCredential;

        public TokenBridge(ILogger<TokenBridge> logger, TokenCredential tokenCredential)
        {
            _logger = logger;
            _tokenCredential = tokenCredential;
        }

        [Function("TokenBridge")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "securitynl/msi")] HttpRequestData req,
            CancellationToken cancellationToken
            )
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var values) ? values.FirstOrDefault() : null;
            var body = await req.ReadAsStringAsync();
            _logger.LogInformation("Received request {contentType}: {body}", contentType, body);

            if (string.IsNullOrEmpty(body))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Parse url encoded form data
            var formData = HttpUtility.ParseQueryString(body);
            var resource = formData["resource"]?.Trim(' ', '\r', '\n');

            if (string.IsNullOrEmpty(resource))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("Requesting token for resource {resource}", resource);

            var tokenResult = await _tokenCredential.GetTokenAsync(new TokenRequestContext([resource]), cancellationToken);
            
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new TokenResult
            {
                AccessToken = tokenResult.Token,
                ExpiresIn = (int)tokenResult.ExpiresOn.Subtract(DateTimeOffset.UtcNow).TotalSeconds,
                ExpiresOn = (int)tokenResult.ExpiresOn.ToUnixTimeSeconds(),
                NotBefore = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 30,
                Resource = resource
            }, cancellationToken);

            return response;
        }
    }

    public class TokenResult
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("expires_on")]
        public int ExpiresOn { get; set; }

        [JsonPropertyName("not_before")]
        public int NotBefore { get; set; }

        [JsonPropertyName("resource")]
        public string? Resource { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

    }
}
