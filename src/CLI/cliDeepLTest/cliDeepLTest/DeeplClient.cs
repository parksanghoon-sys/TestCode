using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace cliDeepLTest
{
    public record Translation(string DetectedSourceLanguage, string TranslatedText);
    internal class DeeplClient
    {
        private readonly string _apiKey;
        private  HttpClient _httpClient = new HttpClient();
        public DeeplClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<Translation> Translate(string text, string targetLange)
        {
            var response = await _httpClient.GetAsync(
                $"https://api-free.deepl.com/v2/translate?auth_key={_apiKey}" +
                $"&text={text}&target_lang={targetLange}");
            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("error in target Language");
            }
            //await Test(response);
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Translation>(await response.Content.ReadAsStringAsync()) ??
                    throw new InvalidOperationException("invalid json from api");
        }
        private async Task Test(HttpRequestMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);
            var translated = json["translations"]![0]!["text"];
            var assumeLanguage = json["translations"]![0]!["detected_source_language"];
        }
    }
}
