using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics.CodeAnalysis;

namespace TestProject2
{
    public class UnitTest1
    {
        [Fact]
        [SuppressMessage("Usage", "CA2234:Pass Ystem uri objects instead of strings",
       Justification = "URL isn't passed as variable, but as literal.")]
        public async Task HomeRetrun()
        {
            using var factory = new WebApplicationFactory<webGoodCode.Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/");                

            Assert.True(response.IsSuccessStatusCode, $"Actural status code : {response.StatusCode}");
        }
        [Fact]
        [SuppressMessage("Usage", "CA2234:Pass Ystem uri objects instead of strings",
            Justification = "URL isn't passed as variable, but as literal.")]
        public async Task HomeRetrunJson()
        {
            using var factory = new WebApplicationFactory<webGoodCode.Program>();
            var client = factory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, "");
            request.Headers.Accept.ParseAdd("application/json");            
            var response = await client.SendAsync(request);

            Assert.True(response.IsSuccessStatusCode, $"Actural status code : {response.StatusCode}");
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }
    }
}
