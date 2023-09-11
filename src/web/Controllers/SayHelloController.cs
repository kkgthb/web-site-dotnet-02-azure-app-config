using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Handwritten.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SayHelloController : ControllerBase
{
    private IConfiguration configuration;
    private readonly IFeatureManager featuremanager;
    public SayHelloController(IConfiguration configuration, IFeatureManager featuremanager) { 
        this.configuration = configuration;
        this.featuremanager = featuremanager;
    }
    // You can get this response by visiting "/api/sayhello" (any capitalization of "sayhello" is fine)
    [HttpGet(Name = "GetSayHello")]
    public async Task<IActionResult> Get()
    {
        String pizzaGreeting = "Hello, world.  Here are some pizza flavors I like:  ";
        pizzaGreeting += $"Flavor 1, which should auto-update via sentinel-value polling:  {configuration["my-pizza-app:pull-update:topping-non-secret-pull-update"] ?? "(oh no -- no value for \"my-pizza-app:pull-update:topping-non-secret-pull-update\")!"}.  ";
        pizzaGreeting += $"Flavor 2, which updates at startup only:  {configuration["my-pizza-app:static:topping-non-secret-static"] ?? "(oh no -- no value for \"my-pizza-app:static:topping-non-secret-static\")!"}.  ";
        pizzaGreeting += $"Flavor 3, which should auto-update via sentinel-value polling:  {configuration["my-pizza-app:pull-update:pizza-flavor-indirect-secret-pull-update"] ?? "(oh no -- no value for \"my-pizza-app:pull-update:pizza-flavor-indirect-secret-pull-update\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"Flavor 4, which updates at startup only:  {configuration["my-pizza-app:static:topping-indirect-secret-static"] ?? "(oh no -- no value for \"my-pizza-app:static:topping-indirect-secret-static\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"Flavor 5, which should auto-update sentinel-less-ly:  {configuration["my-pizza-app:sentinelless-pull:topping-indirect-secret-sentinelless-pull"] ?? "(oh no -- no value for \"my-pizza-app:sentinelless-pull:topping-indirect-secret-sentinelless-pull\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"Flavor 6, which should auto-update via event push:  {configuration["my-pizza-app:push-update:topping-non-secret-push-update"] ?? "(oh no -- no value for \"my-pizza-app:push-update:topping-non-secret-push-update\")!"}.  ";
        pizzaGreeting += $"Flavor 7, which should auto-update via event push:  {configuration["my-pizza-app:push-update:topping-indirect-secret-push-update"] ?? "(oh no -- no value for \"my-pizza-app:push-update:topping-indirect-secret-push-update\")!"}.  Don't tell -- it's a secret!  ";
        // pizzaGreeting += $"Flavor 8, which updates at startup only:  {configuration["pizzaFlavorDirectSecretStartupUpdateOnly"] ?? "(oh no -- no value for \"pizzaFlavorDirectSecretStartupUpdateOnly\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"And the pull-sentinel is {configuration["my-pizza-app:pull-update:sentinel-for-pull-update"] ?? "(oh no -- no value for \"my-pizza-app:pull-update:sentinel-for-pull-update\")"}.  ";
        pizzaGreeting += $"And the push-sentinel is {configuration["my-pizza-app:push-update:sentinel-for-push-update"] ?? "(oh no -- no value for \"my-pizza-app:push-update:sentinel-for-push-update\")"}.  ";
        var redSauceIsEnabled = await featuremanager.IsEnabledAsync(feature: "my-pizza-app--percentage-enabled--release-red-sauce");
        Console.WriteLine(configuration.GetChildren());
        var saucePhrase = (redSauceIsEnabled ? "New and improved pizzas using red sauce.  " : "The pizza is made with white sauce.  ");
        pizzaGreeting += saucePhrase;
        return Ok(pizzaGreeting);
    }
}