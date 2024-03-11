using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string textToTranslate = "Hello, world!";
        string targetLanguage = "DE"; // 대상 언어 코드 (예: DE는 독일어)

        string apiKey = "e2a7ad93-d7a1-4ea7-ac8d-a76e03829a2e:fx"; // DeepL API 키

        using (HttpClient client = new HttpClient())
        {
            string apiUrl = $"https://api.deepl.com/v2/translate?auth_key={apiKey}&text={textToTranslate}&target_lang={targetLanguage}";

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                // 여기서 응답을 적절히 처리합니다.
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
    }
}
