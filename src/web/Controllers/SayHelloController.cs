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
        pizzaGreeting += $"Flavor 1, which should auto-update via sentinel-value polling:  {configuration["pizza-flavor-non-secret-pull-update"] ?? "(oh no -- no value for \"pizza-flavor-non-secret-pull-update\")!"}.  ";
        pizzaGreeting += $"Flavor 2, which updates at startup only:  {configuration["pizza-flavor-non-secret-static"] ?? "(oh no -- no value for \"pizza-flavor-non-secret-static\")!"}.  ";
        pizzaGreeting += $"Flavor 3, which should auto-update via sentinel-value polling:  {configuration["pizza-flavor-indirect-secret-pull-update"] ?? "(oh no -- no value for \"pizza-flavor-indirect-secret-pull-update\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"Flavor 4, which updates at startup only:  {configuration["pizza-flavor-indirect-secret-static"] ?? "(oh no -- no value for \"pizza-flavor-indirect-secret-static\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"Flavor 5, which should auto-update sentinel-less-ly:  {configuration["pizza-flavor-indirect-secret-sentinelless-pull"] ?? "(oh no -- no value for \"pizza-flavor-indirect-secret-sentinelless-pull\")!"}.  Don't tell -- it's a secret!  ";
        // pizzaGreeting += $"Flavor 6, which should auto-update via sentinel-value polling:  {configuration["pizzaFlavorDirectSecretAutoUpdateSentinelPoll"] ?? "(oh no -- no value for \"pizzaFlavorDirectSecretAutoUpdateSentinelPoll\")!"}.  Don't tell -- it's a secret!  ";
        // pizzaGreeting += $"Flavor 7, which updates at startup only:  {configuration["pizzaFlavorDirectSecretStartupUpdateOnly"] ?? "(oh no -- no value for \"pizzaFlavorDirectSecretStartupUpdateOnly\")!"}.  Don't tell -- it's a secret!  ";
        pizzaGreeting += $"And the sentinel is {configuration["sentinel-for-pull-update"] ?? "(oh no -- no value for \"sentinel-for-pull-update\")"}.  ";
        var redSauceIsEnabled = await featuremanager.IsEnabledAsync(feature: "release-red-sauce");
        var saucePhrase = (redSauceIsEnabled ? "New and improved pizzas using red sauce.  " : "The pizza is made with white sauce.  ");
        pizzaGreeting += saucePhrase;
        return Ok(pizzaGreeting);
    }
}