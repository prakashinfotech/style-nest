namespace StyleNest.Auth.API.Options;

public sealed class OtpDeliveryOptions
{
    public const string SectionName = "OtpDelivery";
    public string Provider { get; init; } = "Logging";
    public AzureCommunicationEmailOptions? AzureCommunication { get; init; }
    public SmtpEmailOptions? Smtp { get; init; }
}

public sealed class AzureCommunicationEmailOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string SenderAddress { get; init; } = "donotreply@stylenest.com";
}
