using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace ArimartEcommerceAPI.Services.Services
{
    public class FcmPushService : IFcmPushService
    {
        private readonly string _projectId = "arimartretailapp"; // ✅ Your Firebase Project ID
        private readonly string _jsonPath = Path.Combine(AppContext.BaseDirectory, "FirebaseKeys", "arimartretailapp-509848c8e7b9.json");

        public async Task<bool> SendNotificationAsync(string deviceToken, string title, string body)
        {
            var message = new
            {
                message = new
                {
                    token = deviceToken,
                    notification = new { title, body },
                    android = new { priority = "high" }
                }
            };

            var accessToken = await GetAccessToken();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var jsonMessage = JsonConvert.SerializeObject(message);
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("🔥 FCM Error: " + error);
            }

            return response.IsSuccessStatusCode;
        }

        private async Task<string> GetAccessToken()
        {
            GoogleCredential credential = GoogleCredential
                .FromFile(_jsonPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }
    }



}
