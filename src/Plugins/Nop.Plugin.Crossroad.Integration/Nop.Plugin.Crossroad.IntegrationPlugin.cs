using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Crossroad.Integration;

/// <summary>
/// Rename this file and change to the correct type
/// </summary>
public class IntegrationCustomPlugin : BasePlugin, IAdminMenuPlugin
{
    private readonly ILocalizationService _localizationService;

    public IntegrationCustomPlugin(ILocalizationService localizationService) => _localizationService = localizationService;

    public async Task ManageSiteMapAsync(SiteMapNode rootNode)
    {
        var configurationItem = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Configuration"));

        configurationItem?.ChildNodes.Insert(1, new SiteMapNode
        {
            Visible = true,
            SystemName = "Integration settings",
            Title = await _localizationService.GetResourceAsync("Admin.Configuration.Settings.Integration.MenuItem"),
            ControllerName = "Integration",
            ActionName = "Settings",
            IconClass = "far fa-dot-circle",
            RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
        });
    }

}