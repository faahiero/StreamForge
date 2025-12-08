namespace StreamForge.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
}
