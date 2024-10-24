using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Areas.Admin.Factories;
using Nop.Plugin.Crossroad.Integration.Areas.Admin.Models;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Controllers;

namespace Nop.Plugin.Crossroad.Integration.Areas.Admin.Controllers;

public class IntegrationController : BaseAdminController
{
    private readonly IPermissionService _permissionService;
    private readonly IIntegrationModelFactory _integrationModelFactory;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    public IntegrationController(IPermissionService permissionService,
                                 IIntegrationModelFactory integrationModelFactory,
                                 INotificationService notificationService,
                                 ILocalizationService localizationService)
    {
        _permissionService = permissionService;
        _integrationModelFactory = integrationModelFactory;
        _notificationService = notificationService;
        _localizationService = localizationService;
    }

    public async Task<IActionResult> Settings()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings)) return AccessDeniedView();

        var model = await _integrationModelFactory.PrepareIntegrationSettingsModelAsync();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(IntegrationSettingsModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings)) return AccessDeniedView();

        await _integrationModelFactory.SaveIntegrationSettingsAsync(model);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

        var newModel = await _integrationModelFactory.PrepareIntegrationSettingsModelAsync();

        return View(newModel);
    }
}