# A tiny ASP.NET website that displays values from Azure App Configuration

## NEXT STEPS

1. Play with Key Vault.
2. Play with Key Vault and sentinel values.  (Do they help update Key Vault?)
3. And what's this 3rd thing "periodically reload..."?  How does/doesn't it interact w/ sentinel usage?
      * https://learn.microsoft.com/en-us/azure/azure-app-configuration/reload-key-vault-secrets-dotnet
4. And how do you avoid overprogramming or toe-stepping with "SetSecretRefreshInterval" vs. "ConfigureRefresh"?

---

## Prerequisites

1. To play with this on your local computer, you must install the .NET runtime _(preferably version 7, as that's what I coded this using)_ onto your local machine in a way that lets you run commands beginning with "`dotnet`" from your computer's command prompt _(the "dotnet" executable must be in your "PATH.")_.
    * Don't have Windows admin rights?  Check out David Kou's [Install the .NET runtime onto Windows without admin rights](https://dev.to/davidkou/install-anything-without-admin-rights-4p0j#install-dotnet-sdk-or-runtime-without-admin).
1. You must download a copy of this codebase onto your local computer.
1. Assign the "App Configuration Data Reader" role to whatever Azure AD identity will be logging into Azure App Configuration for this runtime.  Plain old "Owner" or "Contributor" isn't adequate.  Wait up to 15 minutes before you stop getting 403 errors.
1. It seems actually having a key-value pair or something else in the App Configuration resource also helps avoid 403 errors.
1. Assign the "Key Vault Secrets User" role to whatever Azure AD identity will be logging into Azure App Configuration for this runtime.  Plain old "Owner" or "Contributor" isn't adequate.
1. You personally may also need "Key Vault Administrator" to go into the Azure portal and set up secrets in Key Vault.
1. Create 2 secrets in your Key Vault resource:
      * `directlyAccessedSecretFlavorPullUpdate` with a value of "`margherita`".
      * `indirectlyAccessedSecretFlavorPullUpdate` with a value of "`sausage`".
      * `indirectlyAccessedSecretFlavorStatic` with a value of "`anchovy`".
      * `indirectlyAccessedSecretFlavorSentinellessPull` with a value of "`pepperoni`".
1. Create the following key-value pairs in your Azure App Configuration resource:
      * `sentinel-for-pull-update` with a label of "`pull-update`" and a value of "`attempt 1`".
            ```sh
            az appconfig kv set --key "sentinel-for-pull-update" --label "pull-update" --value "attempt 1" --name "INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE"
            ```
      * `pizza-flavor-non-secret-pull-update` with a label of "`pull-update`" and a value of "`green pepper`".
      * `pizza-flavor-non-secret-static` with a label of "`static`" and a value of "`pineapple`".
1. Create the following Key Vault references in your Azure App Configuration resource:
      * `pizza-flavor-indirect-secret-pull-update` with a label of "`pull-update`" and a secret reference pointing to "`indirectlyAccessedSecretFlavorPullUpdate`".
      * `pizza-flavor-indirect-secret-static` with a label of "`static`" and a secret reference pointing to "`indirectlyAccessedSecretFlavorStatic`".
      * `pizza-flavor-indirect-secret-sentinelless-pull` with a label of "`sentinelless-pull`" and a secret reference pointing to "`indirectlyAccessedSecretFlavorSentinellessPull`".
1. Create the following feature flag in your Azure App Configuration resource:
      * `release-red-sauce` with "Enable feature flag" checked, a label of "`percentage-enabled`," "Use feature filter" checked, and a custom filter added with a name of "`Microsoft.Percentage`," a parameter name of "`Value`," and a Value of "`33`."
1. Log your Azure CLI into Azure.
      * _(For good measure, do `azure logout` and then `azure login` if it's been a while since you played with this codebase.)_
1. Find and replace all occurrences of "`INSERT-YOUR-APP-CONFIG-RESOURCE-NAME-HERE`" under the `src/web` folder of this codebase with the actual name of your Azure App Configuration resource.

**Note:**  I'm almost certainly using labels wrong.  I believe they're really meant more for multiple value variations per key name, as you might want with environments, than for "thematically grouping" keys where there's no overlap in name anyway.  I suspect I should've been doing what I'm doing with naming conventions -- but I wanted to play with labels.  Maybe I'll clean this up later.

---

## Building source code into a runtime

Open up a command line interface.

Ensure that its prompt indicates that your commands will be running within the context of the folder into which you downloaded a copy of this codebase.

Run the following command:

```sh
(cd ./src/web && dotnet publish --configuration Release --output ../../my_output && cd ../..)
```

The above command should execute within a second or two.

* It will add a new subfolder called `/bin/` as well as one called `/obj/` into `/src/web/` within the folder on your computer containing a copy of this codebase, but you can ignore those.
* More importantly, it will add a new subfolder called `/my_output/` into the top level of the folder on your computer containing a copy of this codebase.

Congratulations -- you have now built an executable "runtime" for ASP.NET that, when executed, will behave as a web server.

The entirety of your "runtime" that you just built lives in the `/my_output/` folder.  It should contain about 6 files, including one called `Handwritten.exe`.

---

## Running your web server

Open up a command line interface.

Ensure that its prompt indicates that your commands will be running within the context of the folder into which you downloaded a copy of this codebase.

Run the following command:

```sh
./my_output/Handwritten.exe
```

* **WARNING**:  Do _not_ close the command line just yet or it will be difficult to stop your web server later in this exercise.

The output you will see will say something like:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\example
```

---

## Visiting your website

Open a web browser and navigate to [http://localhost:5000/api/sayhello](http://localhost:5000/api/sayhello) _(being sure to use an alternative port number to `5000` if indicated "`now listening on`" message you saw when you executed your runtime)_.

Take a look in the upper-left corner of the webpage you just visited:  it should say "**Hello World**".

---

## Trying key-value updates

1. Reload the page a dozen or so times.  Note that because we've set the "red sauce" feature to only be enabled 33% of the time, you should see it mention white sauce about 2/3 of the time and red sauce about 1/3 of the time.
1. Change the value of "`pizza-flavor-non-secret-pull-update`" to "`green pepper 2`".  Wait 15 seconds, reload the webpage twice, and validate that it still says "`green pepper`".
1. Change the value of "`pizza-flavor-non-secret-static`" to "`pineapple 2`".  Wait 15 seconds, reload the webpage twice, and validate that it still says "`pineapple`".
1. Create a new version of "`indirectlyAccessedSecretFlavorPullUpdate`" with a value of "`sausage 2`".  Wait 15 seconds, reload the webpage twice, and validate that it still says "`sausage`".
1. Create a new version of "`indirectlyAccessedSecretFlavorStatic`" with a value of "`anchovy 2`".  Wait 15 seconds, reload the webpage twice, and validate that it still says "`anchovy`".
1. Create a new version of "`indirectlyAccessedSecretFlavorSentinellessPull`" with a value of "`pepperoni 2`".  Wait 15 seconds, reload the webpage twice, and validate that it now says "`pepperoni 2`".
      * You should also see server logs after the first reload of this step, and before the second, reading something like this:
            ```
            info: Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh[0]
                  Setting updated from Key Vault. Key:'pizza-flavor-indirect-secret-sentinelless-pull'
            ```
1. Change the value of "`sentinel-for-pull-update`" to "`attempt 2`".  Wait 15 seconds, reload the webpage twice, and validate that it now says "`green pepper 2`" and "`sausage 2`" but still just "`pineapple`" and "`anchovy`".
      * You should also see server logs after the first post-sentinel-edit reload, and before the second, reading something like this:
            ```
            info: Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh[0]
                  Setting updated. Key:'sentinel-for-pull-update'
                  Configuration reloaded.
            ```
      * You might also see something like:
            ```
            info: Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh[0]
                  Setting updated from Key Vault. Key:'pizza-flavor-indirect-secret-pull-update'
            ```

---

## Stopping your web server

Go back to the command line interface from which you ran `./my_output/Handwritten.exe`.

Hold <kbd>Ctrl</kbd> and hit <kbd>c</kbd>, then release them both.

---

## Notes

* Even if you set your sentinel-polling lifecycle to 15 seconds and let 15 seconds elapse, .NET won't actually bother to poll Azure App Config and check whether "sentinel" has been updated until some sort of URL _(not necessarily a valid one, just one)_ gets actually visited.
      * Therefore, don't be surprised if you have to reload your web page twice to see your changes.  _(Or visit some other page and then reload the page on which you want to see your changes.)_