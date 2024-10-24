namespace Nop.Plugin.Crossroad.Integration.Areas.Admin.Models;

public record IntegrationSettingsModel
{
    public OpenKMSettingsModel OpenKMSettingsModel { get; set; }
    public OnixEditSettingsModel OnixEditSettingsModel { get; set; }
}