using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Areas.Admin.Models;

namespace Nop.Plugin.Crossroad.Integration.Areas.Admin.Factories;

public interface IIntegrationModelFactory
{
    Task<IntegrationSettingsModel> PrepareIntegrationSettingsModelAsync();

    Task SaveIntegrationSettingsAsync(IntegrationSettingsModel settings);
}