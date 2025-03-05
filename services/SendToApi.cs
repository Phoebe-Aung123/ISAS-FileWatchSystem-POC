using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcherService.services
{
    public class SendToApi: ISendToApi
    {
        private readonly IConfiguration _configuration; 
        private readonly HttpClient _httpClient;
        private readonly string _endpoint; 
        private readonly string _apiKey;

        public SendToApi(IConfiguration configuration, IHttpClientFactory httpClientFactory){
            _configuration = configuration;
            _httpClient =  httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json"); //set up default headers
            _endpoint = _configuration["ApiEndpoint"];
            _apiKey = _configuration["ApiKey"];

            if (!string.IsNullOrEmpty(_apiKey)){
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task PostAsJsonAsync(string JsonPath){
            try {
                //read json data
                string jsonContent = await File.ReadAllTextAsync(JsonPath);
                var content = new StringContent(JsonPath, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_endpoint, content);

                if (response.IsSuccessStatusCode){
                    Console.WriteLine("Successfully sent to the API.");
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {responseContent}");
                }
                else { 
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error sending data to API: {ex.Message}");
            }

            
        }
    }
}
