namespace ACS.UI.Services;

public class BackendSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5100;
    public string BaseUrl => $"http://{Host}:{Port}";
}
