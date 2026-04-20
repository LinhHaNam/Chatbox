namespace SimsimiChat.Configuration;

/// <summary>
/// JWT Configuration settings
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// OpenAI Configuration settings
/// </summary>
public class OpenAiSettings
{
    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "gpt-4o-mini";
    public string DefaultRudeness { get; set; } = "Neutral";
}
