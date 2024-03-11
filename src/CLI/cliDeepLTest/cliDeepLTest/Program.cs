using cliDeepLTest;
using DeepL.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    string apiKey = "e2a7ad93-d7a1-4ea7-ac8d-a76e03829a2e:fx"; // DeepL API 키
    private DeeplClient Deepl;
    string textToTranslate = "Hello, world!";
    string targetLanguage = "KO"; // 대상 언어 코드 (예: DE는 독일어)

    
    static async Task Main(string[] args)
    {    
        var program = new Program();
        await program.AskToUser();
    }
    public Program()
    {
        Deepl = new(apiKey);
    }

    private async Task AskToUser()
    {
        var response = Deepl.Translate(textToTranslate, targetLanguage);
        DisplayTranslate(await response);
    }
    private static void DisplayTranslate(Translation datas)
    {
        var (assumeLanguage, translated) = datas;
        Console.WriteLine($"le texte traduit est: {translated}");
        Console.WriteLine($"La langue reconnue est {assumeLanguage}");
    }
}
