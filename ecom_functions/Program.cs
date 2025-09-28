using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            // This line is now corrected
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEY_VAULT_URI"));
            config.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton((s) => {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("The 'CosmosDbConnectionString' is missing.");
            }
            return new CosmosClient(connectionString);
        });
    })
    .Build();

host.Run();