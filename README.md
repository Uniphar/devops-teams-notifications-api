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

1. create a dev tunnel:

   ```bash
   devtunnel host -p 3978 --allow-anonymous

   ```

2. On the Azure Bot (for the moment: devops-bot-demo), select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages` eg: `https://kw238403-3978.eun1.devtunnels.ms/api/messages`
3. Change the appsettings.json : "{{ClientId}}" "{{TenantId}}" and "{{ClientSecret}}" to the creds of the bot
4. Run the application
5. Add the bot to teams, select **Settings**, then **Channels**, and click on the link **Open in Teams**
6. Select a channel, you might need to send a message in the channel to be able to initiate the bot for the first time, this is since we do not get the notification that it has been installed if the bot was already installed on that team.
