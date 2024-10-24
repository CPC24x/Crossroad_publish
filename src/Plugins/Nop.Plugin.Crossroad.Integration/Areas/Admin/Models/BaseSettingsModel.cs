using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Crossroad.Integration.Areas.Admin.Models;

public record BaseSettingsModel
{
    [NopResourceDisplayName("Admin.Configuration.Settings.Integration.Url")]
    public string Url { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Integration.Username")]
    public string Username { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Integration.Password")]
    public string Password { get; set; }
}