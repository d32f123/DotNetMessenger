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
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:");
            var response = await Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<User>();
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

        #endregion

        #region Tokens region
        public static async Task<Guid> GetUserTokenAsync(string login, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{login}:{password}");
            var response = await Client.SendAsync(request);
            try
            {
                return response.IsSuccessStatusCode
                    ? Guid.Parse(await response.Content.ReadAsStringAsync())
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:");
            Client.SendAsync(request);
        }

        public static async Task<int> GetUserIdByTokenAsync(int id, Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:");
            var response = await Client.SendAsync(request);
            try
            {
                return response.IsSuccessStatusCode ? int.Parse(await response.Content.ReadAsStringAsync()) : -1;
            }
            catch
            {
                return -1;
            }
        }
        #endregion
    }
}
