using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SecureMultiTenantApp.Extensions;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace SecureMultiTenantApp.Demo1
{
    public class LoadCertificateAndAuthenticateToEntra
    {
        private readonly ILogger<LoadCertificateAndAuthenticateToEntra> _logger;
        private readonly StrongConfiguration _strongConfiguration;
        private readonly TokenCredential _tokenCredential;
        private readonly HttpClient _httpClient;

        public LoadCertificateAndAuthenticateToEntra(ILogger<LoadCertificateAndAuthenticateToEntra> logger, IOptions<StrongConfiguration> options, TokenCredential tokenCredential, HttpClient httpClient)
        {
            _logger = logger;
            // Load the configuration, registered in the Program.cs file
            _strongConfiguration = options.Value;
            _tokenCredential = tokenCredential;
            _httpClient = httpClient;
        }

        [Function(nameof(LoadCertificateAndAuthenticateToEntra))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "demo1/call-graph")] HttpRequestData req,
            string? tenantId, // This will be loaded from the url if passed
            FunctionContext executionContext
            )
        {
            // Create a certificate client, passing in the key vault uri (from configuration) and the token credential
            var certificateClient = new CertificateClient(_strongConfiguration.KeyVaultUri, _tokenCredential);
            // Download the certificate, passing in the name of the certificate
            var certificate = await certificateClient.DownloadCertificateAsync(new DownloadCertificateOptions(_strongConfiguration.ClientCertificateName), cancellationToken: executionContext.CancellationToken);

            // At this point, the certificate is loaded and can be used to authenticate to Entra

            // Option 1: Use Azure.Identity to authenticate to Entra
            var token = await GetTokenUsingClientCertificateCredentialAsync(tenantId ?? _strongConfiguration.TenantId!, _strongConfiguration.ClientId!, certificate.Value, [_strongConfiguration.GraphScope!], executionContext.CancellationToken);

            // Option 2: Use MSAL.NET to retrieve the token
            // You can choose to use MSAL.NET to retrieve the token
            // The outcome is the same, you have a token to call the Microsoft Graph API
            //var token = await GetTokenUsingMsalAsync(tenantId ?? _strongConfiguration.TenantId!, _strongConfiguration.ClientId!, certificate.Value, [_strongConfiguration.GraphScope!], executionContext.CancellationToken);

            _logger.LogInformation("Getting users from Graph API with token {token}", TokenModifier.RemoveSignature(token));

            // Use the token to call the Microsoft Graph API
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$select=id,displayName&$top=3");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(httpRequest, executionContext.CancellationToken);
            var responseData = await response.Content.ReadAsByteArrayAsync(executionContext.CancellationToken);

            // Return the graph response
            var result = req.CreateResponse(response.StatusCode);
            await result.WriteBytesAsync(responseData, executionContext.CancellationToken);
            return result;
        }

        /// <summary>
        /// Get a token using Azure.Identity
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId"></param>
        /// <param name="certificate"></param>
        /// <param name="scopes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Access token</returns>
        private async Task<string> GetTokenUsingClientCertificateCredentialAsync(string tenantId, string clientId, X509Certificate2 certificate, string[] scopes, CancellationToken cancellationToken)
        {
            var clientCredential = new ClientCertificateCredential(tenantId, clientId, certificate);
            var result = await clientCredential.GetTokenAsync(new TokenRequestContext(scopes), cancellationToken);

            return result.Token;
        }

        /// <summary>
        /// Get a token using Microsoft.Identity.Client
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId"></param>
        /// <param name="certificate"></param>
        /// <param name="scopes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Access token</returns>
        private async Task<string> GetTokenUsingMsalAsync(string tenantId, string clientId, X509Certificate2 certificate, string[] scopes, CancellationToken cancellationToken)
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithCertificate(certificate)
                .WithTenantId(tenantId)
                .Build();
            var result = await app.AcquireTokenForClient(scopes)
                .ExecuteAsync(cancellationToken);

            return result.AccessToken;
        }
    }
}
