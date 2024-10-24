using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Crossroad.Integration.Areas.Admin.Models;
using Nop.Plugin.Crossroad.Integration.Domains.Onix;
using Nop.Plugin.Crossroad.Integration.Domains.OpenKm;
using Nop.Services.Configuration;

namespace Nop.Plugin.Crossroad.Integration.Areas.Admin.Factories;

public class IntegrationModelFactory : IIntegrationModelFactory
{
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    public IntegrationModelFactory(ISettingService settingService,
                                   IStoreContext storeContext)
    {
        _settingService = settingService;
        _storeContext = storeContext;
    }

    public async Task<IntegrationSettingsModel> PrepareIntegrationSettingsModelAsync()
    {
        OnixEditSettings onixEditSettings = await _settingService.LoadSettingAsync<OnixEditSettings>();

        OpenKMSettings openKMPlanSettings = await _settingService.LoadSettingAsync<OpenKMSettings>();

        return new IntegrationSettingsModel
        {
            OnixEditSettingsModel = new OnixEditSettingsModel
            {
                Password = onixEditSettings.Password,
                Username = onixEditSettings.Username,
                Url = onixEditSettings.Url
            },
            OpenKMSettingsModel = new OpenKMSettingsModel
            {
                Password = openKMPlanSettings.Password,
                Username = openKMPlanSettings.Username,
                Url = openKMPlanSettings.Url
            }
        };
    }

    public async Task SaveIntegrationSettingsAsync(IntegrationSettingsModel settings)
    {
        int storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        OpenKMSettings openKMSettings = await _settingService.LoadSettingAsync<OpenKMSettings>(storeId);

        OnixEditSettings onixEditSettings = await _settingService.LoadSettingAsync<OnixEditSettings>(storeId);

        openKMSettings.Username= settings.OpenKMSettingsModel.Username;
        openKMSettings.Password = settings.OpenKMSettingsModel.Password;
        openKMSettings.Url = settings.OpenKMSettingsModel.Url;

        await _settingService.SaveSettingAsync(openKMSettings, storeId);

        onixEditSettings.Username = settings.OnixEditSettingsModel.Username;
        onixEditSettings.Password = settings.OnixEditSettingsModel.Password;
        onixEditSettings.Url = settings.OnixEditSettingsModel.Url;

        await _settingService.SaveSettingAsync(onixEditSettings, storeId);
    }
}