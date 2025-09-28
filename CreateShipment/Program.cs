using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        // This block connects to Key Vault for local development to avoid storing secrets in files
        if (context.HostingEnvironment.IsDevelopment())
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEY_VAULT_URI"));
            config.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // This registers the CosmosClient as a singleton to be reused by all functions
        services.AddSingleton((s) => {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("The 'CosmosDbConnectionString' is missing from the configuration.");
            }
            return new CosmosClient(connectionString);
        });
    })
    .Build();

host.Run();