# Secure multi-tenant application with Entra ID

This is a sample application that demonstrates how to secure a multi-tenant application with Entra ID, more details in [this blog post](https://svrooij.io/2022/01/20/secure-multi-tenant-app/).

Sample application is not production ready and should be used only for educational purposes.

by Stephan van Rooij

- [Blog](https://svrooij.io/)
- [Twitter](https://twitter.com/svrooij/)
- [LinkedIn](https://www.linkedin.com/in/stephanvanrooij/)
- [GitHub](https://github.com/svrooij/)

## Requirements to run yourself

- Visual Studio 2022 or Visual Studio Code
- .NET 8.0 SDK
- Azure Functions Tools
- Azure Key Vault
- App registration in Entra ID

## Deploy to Azure

1. Create Key Vault in Azure
2. Generate a new certificate in the Key Vault (and mark the key as NOT exportable)
3. Download the certificate as `.cer` file
4. Create new App Registration
5. Upload the certificate under Certificates and Secrets
6. Deploy Function app from visual studio to Azure
7. Modify settings

## Settings

This app needs several settings or it will refuse to start (because of this line in the Program.cs `services.AddOptionsWithValidateOnStart<StrongConfiguration>();`).

User Secrets:

```json
{
  "MySettings:ClientID": "xxx",
  "MySettings:ClientCertificateName": "test-client-credentials",
  "MySettings:ClientCertificateNameNoKey": "test-client-credentials-no-export",
  "MySettings:TenantID": "yyy",
  "MySettings:KeyVaultUri": "https://zzz.vault.azure.net/"
}
```

Or set the configuration values in the Azure Portal

- `MySettings:ClientID` - The client ID of the app registration
- `MySettings:ClientCertificateName` - The name of the certificate in the Key Vault
- `MySettings:ClientCertificateNameNoKey` - The name of the certificate in the Key Vault marked as not exportable
- `MySettings:TenantID` - The tenant ID of the app registration
- `MySettings:KeyVaultUri` - The URI of the Key Vault
