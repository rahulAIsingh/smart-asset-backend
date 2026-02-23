namespace SmartAssetManager.Api.Services;

public class SmtpEmailOptions
{
    public string MailFrom { get; set; } = string.Empty;
    public string EPassword { get; set; } = string.Empty;
    public string Smtp { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
}
