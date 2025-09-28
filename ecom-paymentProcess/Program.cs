using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

// This is the entry point for your .NET Isolated Function App.
var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        // This block runs only when you are debugging locally (IsDevelopment).
        // It connects to Key Vault to securely load your connection strings.
        if (context.HostingEnvironment.IsDevelopment())
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEY_VAULT_URI"));
            config.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // This registers the CosmosClient for dependency injection.
        // A single instance will be created and reused across all your functions, which is a best practice.
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