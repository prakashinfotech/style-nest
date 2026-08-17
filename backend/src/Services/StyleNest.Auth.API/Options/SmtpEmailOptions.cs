namespace StyleNest.Auth.API.Options;

public sealed class SmtpEmailOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public string SenderAddress { get; init; } = "noreply@stylenest.com";
    public string? Username { get; init; }
    public string? Password { get; init; }
}
