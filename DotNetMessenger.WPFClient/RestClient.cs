using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using DotNetMessenger.Model;
using DotNetMessenger.WPFClient.Classes;

namespace DotNetMessenger.WPFClient
{
    public static class RestClient
    {
        private static readonly string _connectionString = @"http://localhost:58302/api/";
        private static readonly HttpClient Client;

        static RestClient()
        {
            Client = new HttpClient {BaseAddress = new Uri(_connectionString)};
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Users region

        public static async Task<User> GetUserAsync(int id, Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
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

        public static async Task<List<User>> GetAllUsersAsync(Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<User>>();
            }
            return null;
        }

        public static async Task<List<Chat>> GetUserChats(Guid token, int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{id}/chats");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<Chat>>();
            }
            return null;
        }

        #endregion

        #region Chats region


        #endregion

        #region Messages region

        public static async Task<List<Message>> GetChatMessages(Guid token, int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"messages/chats/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<Message>>();
            }
            return null;
        }

        #endregion

        #region Tokens region
        public static async Task<Guid> GetUserTokenAsync(string login, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{login}:{password}".ToBase64String());
            var response = await Client.SendAsync(request).ConfigureAwait(false);
            try
            {
                var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return response.IsSuccessStatusCode
                    ? Guid.Parse(new string(retString.Where(c => c != '"').ToArray()))
                    : Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public static void InvalidateUserToken(Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
            Client.SendAsync(request);
        }

        public static async Task<int> GetUserIdByTokenAsync(Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
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
