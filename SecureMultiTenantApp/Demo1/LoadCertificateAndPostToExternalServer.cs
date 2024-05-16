using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace SecureMultiTenantApp.Demo1
{
    public class LoadCertificateAndPostToExternalServer
    {
        private readonly ILogger<LoadCertificateAndPostToExternalServer> _logger;
        private readonly StrongConfiguration _strongConfiguration;
        private readonly TokenCredential _tokenCredential;
        private readonly HttpClient _httpClient;

        public LoadCertificateAndPostToExternalServer(ILogger<LoadCertificateAndPostToExternalServer> logger, IOptions<StrongConfiguration> options, TokenCredential tokenCredential, HttpClient httpClient)
        {
            _logger = logger;
            // Load the configuration, registered in the Program.cs file
            _strongConfiguration = options.Value;
            _tokenCredential = tokenCredential;
            _httpClient = httpClient;
        }

        [Function(nameof(LoadCertificateAndPostToExternalServer))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "demo1/steal-certificate")] HttpRequestData req,
            FunctionContext executionContext
            )
        {
            // Create a certificate client, passing in the key vault uri (from configuration) and the token credential
            var certificateClient = new CertificateClient(_strongConfiguration.KeyVaultUri, _tokenCredential);
            // Download the certificate, passing in the name of the certificate
            var certificate = await certificateClient.DownloadCertificateAsync(new DownloadCertificateOptions(_strongConfiguration.ClientCertificateName), cancellationToken: executionContext.CancellationToken);

            // At this point, the certificate is loaded ... and we can do whatever we want with it

            _logger.LogWarning("We are stealing the certificate by posting it to an external server");
            // By calling ExportCertificatePem and ExportRSAPrivateKeyPem and puting it underneath each other the certContent will look like:
            // -----BEGIN CERTIFICATE-----
            // MIIDBzCCAe+gAwIBAgIQJQ6+J5Zz1z5zq2Z
            // -----END CERTIFICATE-----
            // -----BEGIN RSA PRIVATE KEY-----
            // MIIEpAIBAAKCAQEAz0Zz1z5zq2Z
            // -----END RSA PRIVATE
            var certContent = certificate.Value.ExportCertificatePem() + "\r\n" + certificate.Value.GetRSAPrivateKey()!.ExportRSAPrivateKeyPem();

            // An external server might not be reachable, but it just a string at this point.
            // You could also write it to the blog storage (every Azure Function has a storage account)
            // or send it to a queue, or maybe even just return it in the response.

            var result = req.CreateResponse(System.Net.HttpStatusCode.OK);
            // I don't want to show the full (working) certificate, so I'm cutting it off but you get the idea
            await result.WriteStringAsync(certContent.Substring(0, certContent.Length - 400) + "...", executionContext.CancellationToken);
            return result;

        }

    }
}
