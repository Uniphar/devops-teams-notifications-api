using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Me.SendMail;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Teams.Notifications.Api.Dialogs;

public class SimpleGraphClient
{
    private readonly GraphServiceClient _graphClient;


    public SimpleGraphClient(string token)
    {
        var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(token));
        _graphClient = new GraphServiceClient(authenticationProvider);
    }

    // Sends an email on the users behalf using the Microsoft Graph API
    public async Task SendMailAsync(string toAddress, string subject, string content)
    {
        if (string.IsNullOrWhiteSpace(toAddress)) throw new ArgumentNullException(nameof(toAddress));

        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentNullException(nameof(subject));

        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentNullException(nameof(content));


        var recipients = new List<Recipient>
        {
            new()
            {
                EmailAddress = new EmailAddress
                {
                    Address = toAddress
                }
            }
        };

        // Create the message.
        var email = new SendMailPostRequestBody
        {
            Message = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Text
                },
                Subject = subject,
                ToRecipients = recipients
            }
        };

        // Send the message.

        await _graphClient.Me.SendMail.PostAsync(email);
    }

    // Gets mail for the user using the Microsoft Graph API
    public async Task<Message[]> GetRecentMailAsync()
    {
        var messages = await _graphClient.Me.Messages.GetAsync();
        return messages.Value.Take(5).ToArray();
    }

    // Get information about the user.
    public async Task<User> GetMeAsync()
    {
        var me = await _graphClient.Me.GetAsync();
        return me;
    }

    // Gets the user's photo
    public async Task<string> GetPhotoAsync()
    {
        var photo = await _graphClient.Me.Photo.Content.GetAsync();
        if (photo != null)
        {
            var ms = new MemoryStream();
            photo.CopyTo(ms);
            var buffers = ms.ToArray();
            var imgDataURL = string.Format("data:image/png;base64,{0}", Convert.ToBase64String(buffers));
            return imgDataURL;
        }

        return "";
    }
}