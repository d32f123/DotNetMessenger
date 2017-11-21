using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Script.Serialization;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WPFClient.Classes;

namespace DotNetMessenger.WPFClient
{
    public static class RestClient
    {
        private static readonly string _connectionString = @"http://localhost:58302/api/";
        private static readonly HttpClient Client;
        private static Guid _token = Guid.Empty;

        /// <summary>
        /// int: chatId
        /// Thread in the tuple: long-polling thread for notifications
        /// EventHandler: list of delegates to call
        /// </summary>
        private static readonly Dictionary<int, (Thread, bool)> Subscriptions =
            new Dictionary<int, (Thread, bool)>();
        private enum SubscriptionType : int
        {
            NewUser = -1,
            NewGroup = -2
        };

        public static int UserId { get; private set; } = -1;

        

        static RestClient()
        {
            Client = new HttpClient {BaseAddress = new Uri(_connectionString)};
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Users region

        public static async Task<User> GetUserAsync(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<User>().ConfigureAwait(false);
            }
            return null;
        }

        public static async Task<User> CreateUserAsync(string username, string password)
        {
            var response = await Client.PostAsJsonAsync("users", new UserCredentials {Username = username, Password = password});
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<User>();
            return null;
        }

        public static async Task<List<User>> GetAllUsersAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<User>>().ConfigureAwait(false);
            }
            return null;
        }

        public static async Task<List<Chat>> GetUserChatsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{UserId}/chats");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<Chat>>();
            }
            return null;
        }

        public static void SubscribeForNewUsers(int lastUserId, EventHandler<List<User>> handler)
        {
            lock (Subscriptions)
            {
                if (Subscriptions.ContainsKey((int) SubscriptionType.NewUser))
                    throw new ArgumentException("You are already subscribed");
                var thread = new Thread(NewUserLongPoller);
                Subscriptions.Add((int)SubscriptionType.NewUser, (thread, false));
                thread.Start((lastUserId, handler));
            }
        }

        public static void UnsubscribeFromNewUsers()
        {
            lock (Subscriptions)
            {
                if (!Subscriptions.ContainsKey((int)SubscriptionType.NewUser))
                    return;
                Subscriptions[(int)SubscriptionType.NewUser] =
                    (Subscriptions[(int)SubscriptionType.NewUser].Item1, true);
            }
        }

        // Tuple:
        // 1) id of the last user
        // 2) handler
        private static async void NewUserLongPoller(object tuple)
        {
            bool shouldExit;
            var client = new HttpClient { BaseAddress = new Uri(_connectionString) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            (var lastId, var handler) = ((int, EventHandler<List<User>>))tuple;
            do
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"users/subscribe/{lastId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                }
                catch
                {
                    response = null;
                }
                if (response != null && response.IsSuccessStatusCode)
                {
                    var list = await response.Content.ReadAsAsync<List<User>>();
                    lastId = list.Last().Id;
                    handler?.Invoke(null, list);
                }
                lock (Subscriptions)
                {
                    shouldExit = Subscriptions[(int)SubscriptionType.NewUser].Item2;
                }
            } while (!shouldExit);
        }

        #endregion

        #region Chats region

        public static async Task<Chat> GetChatAsync(int chatId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"chats/{chatId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Chat>();
            }
            return null;
        }

        public static async Task<Chat> GetDialogChatAsync(int otherId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"chats/dialog/{UserId}/{otherId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Chat>();
            }
            return null;
        }

        public static async Task<ChatUserInfo> GetChatUserInfoAsync(int chatId)
        {
            return await GetChatSpecificUserinfoAsync(chatId, UserId);
        }

        public static async Task<ChatUserInfo> GetChatSpecificUserinfoAsync(int chatId, int userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"chats/{chatId}/users/{userId}/info");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<ChatUserInfo>();
            }
            return null;
        }

        public static async Task<Chat> CreateNewGroupChat(string chatName, IEnumerable<int> users)
        {
            var userList = users as List<int> ?? users.ToList();
            userList.Add(UserId);
            var request = new HttpRequestMessage(HttpMethod.Post, "chats/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client
                .SendAsJsonAsync(request, new ChatCredentials {ChatType = ChatTypes.GroupChat, Title = chatName, Members = userList})
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Chat>();
            }
            return null;
        }

        public static void SubscribeForNewMesages(int chatId, int lastMessageId, EventHandler<(int, List<Message>)> newMessagesHandler)
        {
            lock (Subscriptions)
            {
                if (Subscriptions.ContainsKey(chatId))
                    throw new ArgumentException("You are already subscribed");
                var thread = new Thread(LongPollingThread);
                Subscriptions.Add(chatId, (thread, false));
                thread.Start((chatId, lastMessageId, newMessagesHandler));
            }
        }

        public static void UnsubscribeFromNewMessages(int chatId)
        {
            lock (Subscriptions)
            {
                if (!Subscriptions.ContainsKey(chatId))
                    return;
                Subscriptions[chatId] = (Subscriptions[chatId].Item1, true);
            }
        }

        public static void SubscribeForNewChats(int lastChatId, EventHandler<List<Chat>> newChatsHandler)
        {
            lock (Subscriptions)
            {
                if (Subscriptions.ContainsKey((int) SubscriptionType.NewGroup))
                    throw new ArgumentException("You are already subscribed for new chats");
                var thread = new Thread(NewChatLongPoller);
                Subscriptions.Add((int) SubscriptionType.NewGroup, (thread, false));
                thread.Start((lastChatId, newChatsHandler));
            }
        }

        public static void UnsubscribeFromNewChats()
        {
            lock (Subscriptions)
            {
                if (!Subscriptions.ContainsKey((int) SubscriptionType.NewGroup))
                    return;
                Subscriptions[(int) SubscriptionType.NewGroup] =
                    (Subscriptions[(int) SubscriptionType.NewGroup].Item1, true);
            }
        }

        // Tuple:
        // 1) id of the last chat
        // 2) handler
        private static async void NewChatLongPoller(object tuple)
        {
            bool shouldExit;
            HttpClient client = new HttpClient { BaseAddress = new Uri(_connectionString) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            (var lastId, var handler) = ((int, EventHandler<List<Chat>>))tuple;
            do
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"chats/subscribe/{lastId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                }
                catch
                {
                    response = null;
                }
                if (response != null && response.IsSuccessStatusCode)
                {
                    var list = await response.Content.ReadAsAsync<List<Chat>>();
                    lastId = list.Last().Id;
                    handler?.Invoke(null, list);
                }
                lock (Subscriptions)
                {
                    shouldExit = Subscriptions[(int)SubscriptionType.NewGroup].Item2;
                }
            } while (!shouldExit);
        }

        private static async void LongPollingThread(object chatAndMessageAndHandlerTuple)
        {
            bool shouldExit;
            HttpClient client = new HttpClient();
            client = new HttpClient { BaseAddress = new Uri(_connectionString) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            (var chatId, var lastMessageId, var newMessagesHandler) =
                ((int, int, EventHandler<(int, List<Message>)>))chatAndMessageAndHandlerTuple;
            do
            {

                var request = new HttpRequestMessage(HttpMethod.Get, $"chats/{chatId}/subscribe/{lastMessageId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                }
                catch
                {
                    response = null;
                }
                if (response != null && response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadAsAsync<List<Message>>();
                    lastMessageId = messages.Last().Id;
                    newMessagesHandler?.Invoke(null, (chatId, messages));
                }
                lock (Subscriptions)
                {
                    shouldExit = Subscriptions[chatId].Item2;
                }
            } while (!shouldExit);
        }

        #endregion

        #region Messages region

        public static async Task<List<Message>> GetChatMessagesAsync(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"messages/chats/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<Message>>();
            }
            return null;
        }

        public static async Task<Message> GetLatestChatMessageAsync(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"messages/{id}/last");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Message>();
            }
            return null;
        }

        private static async Task<HttpResponseMessage> SendAsJsonAsync<TModel>(this HttpClient client, HttpRequestMessage msg, TModel model)
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(model);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            msg.Content = stringContent;
            return await client.SendAsync(msg);
        }

        public static async Task SendMessageAsync(int chatId, Message message)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"messages/{chatId}/{UserId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            await Client.SendAsJsonAsync(request, message).ConfigureAwait(false);
        }

        #endregion

        #region Tokens region
        public static async Task<bool> LoginAsync(string login, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{login}:{password}".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            try
            {
                var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _token = Guid.Parse(new string(retString.Where(c => c != '"').ToArray()));
                    UserId = await GetLoggedUserIdAsync();
                    return true;
                }
                _token = Guid.Empty;
                UserId = -1;
                return false;
            }
            catch
            {
                _token = Guid.Empty;
                UserId = -1;
                return false;
            }
        }

        public static void LogOut()
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            Client.SendAsync(request);
            _token = Guid.Empty;
            UserId = -1;
        }

        public static async Task<int> GetLoggedUserIdAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            try
            {
                var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return response.IsSuccessStatusCode ? int.Parse(retString) : -1;
            }
            catch
            {
                return -1;
            }
        }
        #endregion
    }
}
