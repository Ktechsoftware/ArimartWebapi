using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace ArimartEcommerceAPI.Services.Services
{
    public class FcmPushService : IFcmPushService
    {
        private readonly string _projectId = "arimartretailapp";
        private readonly string _jsonPath;

        public FcmPushService(IWebHostEnvironment env) // Inject environment
        {
            _jsonPath = Path.Combine(env.ContentRootPath, "arimartretailapp-firebase-adminsdk-fbsvc-6d5850d47e.json");

            if (!File.Exists(_jsonPath))
                throw new FileNotFoundException("🔥 Firebase service account JSON file not found at:", _jsonPath);
        }

        public async Task<(bool success, string errorMessage)> SendNotificationAsync(string deviceToken, string title, string body)
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
            var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var json = JsonConvert.SerializeObject(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"HTTP {response.StatusCode}: {responseText}");
            }

            return (true, null);
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
