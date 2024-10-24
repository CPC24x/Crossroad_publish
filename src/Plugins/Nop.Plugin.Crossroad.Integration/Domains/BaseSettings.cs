using Nop.Core.Configuration;

namespace Nop.Plugin.Crossroad.Integration.Domains;

public class BaseSettings : ISettings
{
    public string Url { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }
}