namespace Todo.Application.Identity.Model
{
    public record JwtSettings(string Key, string Issuer, string Audience, double DurationInMinutes);
}
