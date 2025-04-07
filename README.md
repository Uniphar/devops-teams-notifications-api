# DevOps Teams Notifications API

This is the first iteration of the Notifications API. In this iteration, the API will handle a single API call to:

1. Create an app.
2. Place it in the appropriate Teams channel.
3. Set up a card in the Teams channel with two buttons:

   - One for reprocessing (dummy functionality).
   - One to view a file.

4. Retrieve the file from a blob storage location.
5. Display the file in Teams when the button is clicked.
