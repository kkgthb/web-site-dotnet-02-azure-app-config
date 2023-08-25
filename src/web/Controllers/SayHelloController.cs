using Microsoft.AspNetCore.Mvc;

namespace Handwritten.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SayHelloController : ControllerBase
{
    private IConfiguration configuration;
    public SayHelloController(IConfiguration configuration) { this.configuration = configuration; }
    // You can get this response by visiting "/api/sayhello" (any capitalization of "sayhello" is fine)
    [HttpGet(Name = "GetSayHello")]
    public String Get()
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
        return pizzaGreeting;
    }
}