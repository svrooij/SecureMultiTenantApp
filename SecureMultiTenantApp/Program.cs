using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureMultiTenantApp;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddHttpClient();

        // Let's add the StrongConfiguration to the services
        // this will require `MySettings__ClientId` and `MySettings__TenantId` to be set
        services.Configure<StrongConfiguration>(ctx.Configuration.GetSection("MySettings"));
        // This will validate the configuration on startup, based on the DataAnnotations
        services.AddOptionsWithValidateOnStart<StrongConfiguration>();

        // Register token credentials based on the environment
        if (ctx.HostingEnvironment.IsDevelopment())
        {
            // DefaultAzureCredential will try several authentication methods until it finds one that works
            services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        } else
        {
            // ManagedIdentityCredential will use the managed identity of the app service
            services.AddSingleton<TokenCredential, ManagedIdentityCredential>();
        }
    })
    .Build();

host.Run();
