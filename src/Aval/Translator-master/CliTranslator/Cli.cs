using System;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DeeplWrapper;
using Microsoft.Extensions.Configuration;

namespace CliTranslator
{
    internal class Cli
    {
        private readonly DeeplClient Deepl;
        static string apiKey = "e2a7ad93-d7a1-4ea7-ac8d-a76e03829a2e:fx"; // DeepL API 키        
        string textToTranslate = "Hello, world!";
        string targetLanguage = "KO"; // 대상 언어 코드 (예: DE는 독일어)
        private Cli(string apikey)
        {
            Deepl = new DeeplClient(apikey);
        }

        public static void Main()
        {
            //var config = new ConfigurationBuilder().AddUserSecrets<Cli>().Build();
            var program = new Cli(apiKey);
            program.AskToUser();
        }

        private static void DisplayTranslate(Translation datas)
        {
            var (assumeLanguage, translated) = datas;
            Console.WriteLine($"le texte traduit est: {translated}");
            Console.WriteLine($"La langue reconnue est {assumeLanguage}");
        }

        private void AskToUser()
        {
            string text;
            do
            {
                Console.WriteLine("veuillez saisir le texte à traduire");
                text = Console.ReadLine();
                if (text != null) continue;
                throw new ApplicationException("error reading input");
            } while (string.IsNullOrWhiteSpace(text));

            Console.WriteLine("veuillez saisir la langue de traduction");
            var targetLang = Console.ReadLine();
            if (targetLang == null)
            {
                Console.WriteLine(" Error cannot read line");
                throw new ApplicationException("stdin closed"); 
            }
            text = HttpUtility.UrlEncode(text);
            Request(text,targetLang);

        }
        private async void Request(string text, string targetLang)
        {
            var response = Deepl.Translate(text,targetLang);
             DisplayTranslate(await response);
        }
    }
}