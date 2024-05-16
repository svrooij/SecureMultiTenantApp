using Azure.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SecureMultiTenantApp.Extensions;
using Smartersoft.Identity.Client.Assertion;
using System.Net.Http.Headers;

namespace SecureMultiTenantApp.Demo2
{
    public class AuthenticateToEntra
    {
        private readonly ILogger<AuthenticateToEntra> _logger;
        private readonly StrongConfiguration _strongConfiguration;
        private readonly TokenCredential _tokenCredential;
        private readonly HttpClient _httpClient;

        public AuthenticateToEntra(ILogger<AuthenticateToEntra> logger, IOptions<StrongConfiguration> options, TokenCredential tokenCredential, HttpClient httpClient)
        {
            _logger = logger;
            // Load the configuration, registered in the Program.cs file
            _strongConfiguration = options.Value;
            _tokenCredential = tokenCredential;
            _httpClient = httpClient;
        }

        [Function(nameof(AuthenticateToEntra))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "demo2/call-graph")] HttpRequestData req,
            string? tenantId, // This will be loaded from the url if passed
            FunctionContext executionContext
            )
        {
            // Use MSAL.NET and Smartersoft.Identity.Client.Assertion to retrieve the token
            // You have a token to call the Microsoft Graph API
            var token = await GetTokenUsingMsalAsync(
                _tokenCredential,
                _strongConfiguration.KeyVaultUri!,
                tenantId ?? _strongConfiguration.TenantId!,
                _strongConfiguration.ClientId!,
                _strongConfiguration.ClientCertificateNameNoKey ?? _strongConfiguration.ClientCertificateName!,
                [_strongConfiguration.GraphScope!],
                executionContext.CancellationToken);

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
        /// Get a token using Microsoft.Identity.Client and Smartersoft.Identity.Client.Assertion
        /// </summary>
        /// <param name="tokenCredential">Token credential to connect to the Key Vault</param>
        /// <param name="keyVaultUri">Uri of the Key Vault to use</param>
        /// <param name="tenantId">Tenant ID to request token for</param>
        /// <param name="clientId">Client ID to request token for</param>
        /// <param name="certificateName">Name of the certificate in Azure</param>
        /// <param name="scopes">Application scopes</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Access token</returns>
        private async Task<string> GetTokenUsingMsalAsync(TokenCredential tokenCredential, Uri keyVaultUri, string tenantId, string clientId, string certificateName, string[] scopes, CancellationToken cancellationToken)
        {
            // The code for the `WithKeyVaultCertificate` extension can be found here https://github.com/Smartersoft/identity-client-assertion/blob/b12318fe7b0d054a65334d61686dc96a9a29024d/src/Smartersoft.Identity.Client.Assertion/ConfidentialClientApplicationBuilderExtensions.cs#L60-L74
            // which eventually calls this code: https://github.com/Smartersoft/identity-client-assertion/blob/b12318fe7b0d054a65334d61686dc96a9a29024d/src/Smartersoft.Identity.Client.Assertion/ClientAssertionGenerator.cs#L146-L166
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                //.WithCertificate(certificateName) // this is replaced with the following line
                .WithKeyVaultCertificate(keyVaultUri, certificateName, tokenCredential) // this is the new line
                .WithTenantId(tenantId)
                .Build();
            var result = await app.AcquireTokenForClient(scopes)
                .ExecuteAsync(cancellationToken);

            return result.AccessToken;
        }
    }
}
