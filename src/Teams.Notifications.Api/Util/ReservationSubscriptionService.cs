using ContosoScuba.Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Text;
using Newtonsoft.Json;

namespace ContosoScuba.Bot.Services
{
    //This static class is a mockup message routing service when ms teams members can subscribe to 
    //be notified when a scuba booking reservation is made by a customer.  Subscribers receive a message
    //that contains a button enabling them to chat with the customer from the bot's webchat.  From that point
    //messages are proxied back and forth between the subscriber and the customer.
    public static class ReservationSubscriptionService
    {
        //all who have subscribed to receive notifications when a customer reserves a scuba getaway(key: subscriber userId, conversationRefernce: subscriber)
        private static ConcurrentDictionary<string, ConversationReference> _reservationSubscribers = new ConcurrentDictionary<string, ConversationReference>();

        //all who have made a scuba reservation (key: customer userId, conversationRefernce: customer webchat
        private static ConcurrentDictionary<string, Tuple<ConversationReference,UserScubaData>> _recentReservations = new ConcurrentDictionary<string, Tuple<ConversationReference, UserScubaData>>();
        //subscribers who have begun chatting with a user via proxy of webchat messages (key: customer userId, conversationRefernce: subscriber webchat)
        private static ConcurrentDictionary<string, ConversationReference> _subscriberToUser = new ConcurrentDictionary<string, ConversationReference>();
       
   

        public static void AddOrUpdateSubscriber(string userId, ConversationReference conversationReference)
        {
            _reservationSubscribers.AddOrUpdate(userId, conversationReference, (key, oldValue) => conversationReference);
        }

        public static void RemoveSubscriber(string userId)
        {
            ConversationReference reference = null;
            _reservationSubscribers.TryRemove(userId, out reference);
        }
        
        public static async Task SendActionableMessage(UserScubaData userScubaData)
        {
            var client = new HttpClient();
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(userScubaData)));

            // TODO: Send actionable message and push notification
            await client.PostAsync("https://adaptivetestfunctions.azurewebsites.net/api/SendScubaEmail?code=4tRDT5xalBkFidaesDGNSg1xVRcU2HPh7Ar7Zsc8vpAXE8DdG9mzHg==", content);


        }
        public static IEnumerable<string> GetSubscribers()
        {
            foreach (var subscriber in _reservationSubscribers.Values)
            {
                yield return subscriber.User.Name;
            }
        }

        public static async Task NotifySubscribers(UserScubaData userScubaData, BotAdapter adapter, ConversationReference reserverReference = null)
        {
            if (reserverReference != null)
            {
                var scubaReservation = new Tuple<ConversationReference, UserScubaData>(reserverReference, userScubaData);
                _recentReservations.AddOrUpdate(reserverReference.User.Id, scubaReservation, (key, oldValue) => scubaReservation);
                //todo: this should not be a hard coded url
                userScubaData.ChatWithUserUrl = "https://contososcubademo.azurewebsites.net?chatWithId=" + reserverReference.User.Id;
                //chatWithUserIdUrl = "Use this URL to chat with them: http://localhost:3979?chatWithId=" + reserverReference.User.Id;
            }
            string message = $"New scuba booking for {userScubaData.PersonalInfo.Name}";

            var replaceInfo = new Dictionary<string, string>();
            replaceInfo.Add("{{destination}}", userScubaData.Destination);
            replaceInfo.Add("{{school}}", userScubaData.School);
            replaceInfo.Add("{{longdate}}", Convert.ToDateTime(userScubaData.Date).ToString("dddd, MMMM dd"));
            replaceInfo.Add("{{number_of_people}}", userScubaData.NumberOfPeople);
            replaceInfo.Add("{{phone}}", userScubaData.PersonalInfo.Phone);
            replaceInfo.Add("{{email}}", userScubaData.PersonalInfo.Email);
            replaceInfo.Add("{{name}}", userScubaData.PersonalInfo.Name);
            replaceInfo.Add("{{protein_preference}}", userScubaData.MealOptions.ProteinPreference);
            replaceInfo.Add("{{vegan}}", userScubaData.MealOptions.Vegan ? "Yes" : "No");
            replaceInfo.Add("{{allergy}}", userScubaData.MealOptions.Alergy);

            if (!string.IsNullOrEmpty(userScubaData.ChatWithUserUrl))
                replaceInfo.Add("{{url}}", userScubaData.ChatWithUserUrl);


         //   var subscriberCardText = await CardProvider.GetCardText("SubscriberNotification", replaceInfo);
           // var conversationCallback = GetConversationCallback(message, workingCredentials, subscriberCardText);

            await SendActionableMessage(userScubaData);

            foreach (var subscriber in _reservationSubscribers.Values)
            {
                //await adapter.ContinueConversationAsync(subscriber.Bot.Id, subscriber, conversationCallback);
            }
        }

          
        public static string GetUserName(string userId)
        {
            Tuple<ConversationReference, UserScubaData> foundReference;
            if (_recentReservations.TryGetValue(userId, out foundReference))
            {
                return foundReference.Item2.PersonalInfo.Name;
            }

            return string.Empty;
        }

        public static bool UserIsMessagingSubscriber(string userId)
        {
            return _subscriberToUser.ContainsKey(userId);
        }

        public static void RemoveUserConnectionToSubscriber(string userId)
        {
            ConversationReference reference = null;
            _subscriberToUser.TryRemove(userId, out reference);
        }

        public static async Task ForwardToReservationUser(string userId, IMessageActivity message, BotAdapter adapter,  ConversationReference contosoReference)
        {
            Tuple<ConversationReference, UserScubaData> foundReference;
            if (_recentReservations.TryGetValue(userId, out foundReference))
            {
                _subscriberToUser.AddOrUpdate(userId, contosoReference, (key, oldValue) => contosoReference);
                var conversationCallback = GetConversationCallback(message);
               // await adapter.ContinueConversationAsync(foundReference.Item1.Bot.Id, foundReference.Item1, conversationCallback);
            }
        }

        public static async Task<bool> ForwardedToSubscriber(string userId, IMessageActivity message, BotAdapter adapter, CancellationToken cancellationToken)
        {
            ConversationReference foundReference = null;
            if (_subscriberToUser.TryGetValue(userId, out foundReference))
            {
                var conversationCallback = GetConversationCallback(message);
               // await adapter.ContinueConversationAsync(foundReference.Bot.Id, foundReference, conversationCallback, cancellationToken );
                return true;
            }

            return false;
        }



        private static Func<ITurnContext, Task> GetConversationCallback(IMessageActivity message)
        {
            Func<ITurnContext, Task> conversationCallback = async (context) =>
            {

                context.Activity.SetReplyFields(message);

                await context.SendActivityAsync(message);
            };

            return conversationCallback;
        }
        private static Func<ITurnContext, Task> GetConversationCallback(string text, string fullMessageText = null)
        {
            Func<ITurnContext, Task> conversationCallback = async (context) =>
            {               

                 Activity reply = null;
                if (string.IsNullOrEmpty(fullMessageText))
                {
                    reply = context.Activity.CreateReply(text);
                }
                else
                {
                    reply = context.Activity.GetReplyFromText(fullMessageText);
                    reply.Text = text;
                }
                await context.SendActivityAsync(reply);
            };

            return conversationCallback;
        }
    }
}
