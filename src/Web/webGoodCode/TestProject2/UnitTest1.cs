using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using webGoodCode.Controllers;
using webGoodCode.Models;
using webGoodCode.Restarant.RestApi;

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
        [Fact]
        public async Task PostValidReservation()
        {
            var response = await PostReservation(new
            {
                date = "2023-03-1-",
                email = "example@example.com",
                name = "park",
                quantity = 2
            });
            Assert.True(response.IsSuccessStatusCode, $"Actural status code : {response.StatusCode}");
        }

        private async Task<HttpResponseMessage> PostReservation(object value)
        {
            using var factory = new WebApplicationFactory<webGoodCode.Program>();
            var client = factory.CreateClient();
            
            string json = JsonSerializer.Serialize(value);
            using var content = new StringContent( json );
            content.Headers.ContentType!.MediaType = "application/json";
            return await client.PostAsync("reservations", content);
        }
        [Fact]
        public async Task PostValidReservationWhenDatabaseIsEmpty()
        {
            var db = new FakeDatabase();
            var sut = new ReservationsController(db);
            var dto = new ReservationDto
            {
                Id = Guid.NewGuid().ToString(),
                At = DateTime.Now.ToString("yyyy-MM-dd"),
                Email = "email",
                Name = "name",
                Quantity = 4
            };
            
            await sut.Post(dto);

            var expected = new Reservation(
                Guid.Parse(dto.Id),
                DateTime.Parse(dto.At, CultureInfo.InvariantCulture),
                new Email(dto.Email),
                new Name(dto.Name ?? ""),
                dto.Quantity);
            Assert.Contains(expected, db.Grandfather);
        }
    }
}
