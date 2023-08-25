using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // Necessary for actually starting to route some API endpoints

var azureCredential = new AzureCliCredential();

builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigName = "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE";
        var appConfigUrl = $"https://{appConfigName}.azconfig.io";
        options.Connect(new Uri(appConfigUrl), azureCredential)
            // Fascinating -- options.Connect().ConfigureRefresh().Register()
            // (https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.azureappconfiguration.azureappconfigurationrefreshoptions.register)
            // completely ignores which .Select() of the .Connect() it's nested inside of.  
            // It applies any "refreshAll: true" to the entire AddAzureAppConfiguration() context -- including 
            // adding auto-refreshing to other options.Connect().Select() statements within the configuration.  
            // If you want to include something in your config, but 
            // exclude it from being auto-refreshed after the sentinel gets refreshed, 
            // you have to put it in a totally separate AddAzureAppConfiguration() call.
            // As seen here, that is.
            .Select(
                keyFilter: KeyFilter.Any,
                labelFilter: "static"
            )
        ;
        options.ConfigureKeyVault(options => { options.SetCredential(azureCredential); });
    }
);

builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigName = "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE";
        var appConfigUrl = $"https://{appConfigName}.azconfig.io";
        options.Connect(new Uri(appConfigUrl), azureCredential)
            .Select(
                keyFilter: KeyFilter.Any,
                labelFilter: "pull-update"
            )
            .ConfigureRefresh(refreshOptions =>
                refreshOptions.Register(
                    key: "sentinel-for-pull-update",
                    label: "pull-update",
                    refreshAll: true
                )
                .SetCacheExpiration(TimeSpan.FromSeconds(15))
            )
        ;
        options.ConfigureKeyVault(options => { options.SetCredential(azureCredential); });
    }
);

builder.Services.AddAzureAppConfiguration();

var app = builder.Build();

app.UseAzureAppConfiguration();

app.MapControllers(); // Necessary for actually starting to route some API endpoints

app.Run();