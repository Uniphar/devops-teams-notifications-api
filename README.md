# DevOps Teams Notifications API

This is the first iteration of the Notifications API. In this iteration, the API will handle a single API call to:

1. Create an app.
2. Place it in the appropriate Teams channel.
3. Set up a card in the Teams channel with two buttons:

   - One for reprocessing (dummy functionality).
   - One to view a file.

4. Retrieve the file from a blob storage location.
5. Display the file in Teams when the button is clicked.

## local setup

Local setup will use the debug bot to communicate making it possible to locally debug.
We run the rest of the application in Azure AKS with WorkLoad identities, these are created with `azwi` and their clientId's are stored in a Service account, we use the service acount in the pod to authenticate, more information can be found here: `https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview`

Since this uses federation we cannot use it locally, for this we will use a bot service setup with a client secret, we also have to have an api endpoint, to make it work locally:

1. Create a dev tunnel:

   ```bash
   devtunnel host -p 3978 --allow-anonymous

   ```

2. On the Azure Bot (for local/debug: devops-debug-bot), select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages` eg: `https://kw238403-3978.eun1.devtunnels.ms/api/messages`
3. Change the secret of the appsettings.local, you can find this in the keyvault under `devops-debug-bot-secret`, the client-id and tenant is already setup
4. Run the application
5. Add the bot to teams, select **Settings**, then **Channels**, and click on the link **Open in Teams**
6. Select a channel, you might need to send a message in the channel to be able to initiate the bot for the first time, this is since we do not get the notification that it has been installed if the bot was already installed on that team.
