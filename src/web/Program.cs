using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.Extensions.Configuration.AzureAppConfiguration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // Necessary for actually starting to route some API endpoints

var azureCredential = new DefaultAzureCredential();

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
                keyFilter: "my-pizza-app:static:*",
                labelFilter: "nonprod"
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
                keyFilter: "my-pizza-app:sentinelless-pull:*",
                labelFilter: "nonprod"
            )
        ;
        options.ConfigureKeyVault(options => {
            options.SetCredential(azureCredential);
            options.SetSecretRefreshInterval(refreshInterval: TimeSpan.FromSeconds(15));
        });
    }
);

builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigName = "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE";
        var appConfigUrl = $"https://{appConfigName}.azconfig.io";
        options.Connect(new Uri(appConfigUrl), azureCredential)
            .Select(
                keyFilter: "my-pizza-app:pull-update:*",
                labelFilter: "nonprod"
            )
            .ConfigureRefresh(refreshOptions =>
                refreshOptions.Register(
                    key: "my-pizza-app:pull-update:sentinel-for-pull-update",
                    label: "nonprod",
                    refreshAll: true
                )
                .SetCacheExpiration(TimeSpan.FromSeconds(15))
            )
        ;
        options.ConfigureKeyVault(options => { options.SetCredential(azureCredential); });
    }
);

IConfigurationRefresher pushUpdateConfigurationRefresher = null;
builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigName = "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE";
        var appConfigUrl = $"https://{appConfigName}.azconfig.io";
        options.Connect(new Uri(appConfigUrl), azureCredential)
            .Select(
                keyFilter: "my-pizza-app:push-update:*",
                labelFilter: "nonprod"
            )
            .ConfigureRefresh(refreshOptions =>
                // We still want to poll for updates to the sentinel key once a day,
                // just in case a "push" update gets missed for any reason.
                refreshOptions.Register(
                    key: "my-pizza-app:push-update:sentinel-for-push-update",
                    label: "nonprod",
                    refreshAll: true
                )
                .SetCacheExpiration(TimeSpan.FromDays(1))
            )
        ;
        options.ConfigureKeyVault(options => { options.SetCredential(azureCredential); });
        pushUpdateConfigurationRefresher = options.GetRefresher();
    }
);

builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigName = "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE";
        var appConfigUrl = $"https://{appConfigName}.azconfig.io";
        options.Connect(new Uri(appConfigUrl), azureCredential)
            .UseFeatureFlags(opt =>
            {
                // Each HTTP request will result in an auto-poll for fresh enabled/disabled/filter values, as long as it's
                // been 30 seconds or more since the last check.
                // This polling comes built in to .UserFeatureFlags().
                opt.Select(
                    featureFlagFilter: "my-pizza-app--percentage-enabled--*",
                    labelFilter: "nonprod"
                );
            })
        ;
    }
);

builder.Services.AddAzureAppConfiguration();

builder.Services.AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    ;

var app = builder.Build();

var servBusName = "my-appconfig-servicebus-namespace";
var servBusFullyQualifiedNamespace = $"{servBusName}.servicebus.windows.net";
var servBusClient = new ServiceBusClient(fullyQualifiedNamespace: servBusFullyQualifiedNamespace, credential: azureCredential);
ServiceBusProcessor processor = servBusClient.CreateProcessor(
    topicName: "my-appconfig-servicebus-topic",
    subscriptionName: "my-appconfig-servicebus-topic-subscription"
);

processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

async Task MessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    // KeyPerFileConfigurationBuilderExtensionConsole.WriteLine(body);
    await Task.Delay(TimeSpan.FromSeconds(1));
    var eventGridEvent = EventGridEvent.Parse(BinaryData.FromBytes(args.Message.Body));
    eventGridEvent.TryCreatePushNotification(out PushNotification pushNotification);
    // NOTE:  I can't tell if ProcessPushNotification() keeps TryRefreshAsync() no-oppey (and therefore cheap) when processing a push that isn't the sentinel.
    // "The ProcessPushNotification method resets the cache expiration to a short random delay. This causes future calls to RefreshAsync or TryRefreshAsync to re-validate the cached values against App Configuration and update them as necessary. In this example you register to monitor changes to the key: TestApp:Settings:Message with a cache expiration of one day. This means no request to App Configuration will be made before a day has passed since the last check. By calling ProcessPushNotification your application will send requests to App Configuration in the next few seconds. Your application will load the new configuration values shortly after changes occur in the App Configuration store without the need to constantly poll for updates. In case your application misses the change notification for any reason, it will still check for configuration changes once a day."
    // "The short random delay for cache expiration is helpful if you have many instances of your application or microservices connecting to the same App Configuration store with the push model. Without this delay, all instances of your application could send requests to your App Configuration store simultaneously as soon as they receive a change notification. This can cause the App Configuration Service to throttle your store. Cache expiration delay is set to a random number between 0 and a maximum of 30 seconds by default, but you can change the maximum value through the optional parameter maxDelay to the ProcessPushNotification method."
    // "The ProcessPushNotification method takes in a PushNotification object containing information about which change in App Configuration triggered the push notfication. This helps ensure all configuration changes up to the triggering event are loaded in the following configuration refresh. The SetDirty method does not gurarantee the change that triggers the push notification to be loaded in an immediate configuration refresh. If you are using the SetDirty method for the push model, we recommend using the ProcessPushNotification method instead."
    // NOTE:  I think so (cheap), because what I saw AFTER "hopefully updated config" but BEFORE "True" & "flavor 6: fet..." in the sysout, the time I changed the push sentinel value, read:
    // info: Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh[0]
    //   Setting updated. Key:'my-pizza-app:push-update:sentinel-for-push-update'
    //   Configuration reloaded.
    pushUpdateConfigurationRefresher.ProcessPushNotification(pushNotification: pushNotification, maxDelay: TimeSpan.FromSeconds(1));
    await Task.Delay(TimeSpan.FromSeconds(1));
    // Console.WriteLine("hopefully updated config");
    // NOTE:  TryRefreshAsync() is a no-op if the cache expiration time window isn't reached.
    var result = await pushUpdateConfigurationRefresher.TryRefreshAsync();
    // Console.WriteLine(result);
    // Console.WriteLine($"Flavor 6:  {app.Configuration["my-pizza-app:push-update:topping-non-secret-push-update"] ?? "(oh no -- no value for \"my-pizza-app:push-update:topping-non-secret-push-update\")!"}.  ");
    await Task.Delay(TimeSpan.FromSeconds(1));
    // we can evaluate application logic and use that to determine how to settle the message.
    await args.CompleteMessageAsync(args.Message);
    return;
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    // the error source tells me at what point in the processing an error occurred
    Console.WriteLine(args.ErrorSource);
    // the fully qualified namespace is available
    Console.WriteLine(args.FullyQualifiedNamespace);
    // as well as the entity path
    Console.WriteLine(args.EntityPath);
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

await processor.StartProcessingAsync();

app.UseAzureAppConfiguration();

app.MapControllers(); // Necessary for actually starting to route some API endpoints

app.Run();