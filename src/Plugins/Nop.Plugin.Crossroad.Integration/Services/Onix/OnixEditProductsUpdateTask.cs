using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Services.Products;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixEditProductsUpdateTask : IScheduleTask
{
    private readonly IPersistenceService _persistenceService;
    private readonly OnixEditService _onixEditService;
    private readonly OnixLoginService _onixLoginService;

    public OnixEditProductsUpdateTask(IPersistenceService persistenceService,
                                      OnixEditService onixEditService,
                                      OnixLoginService onixLoginService)
    {
        _persistenceService = persistenceService;
        _onixEditService = onixEditService;
        _onixLoginService = onixLoginService;
    }

    public async Task ExecuteAsync()
    {
        await _onixLoginService.GetTokenAsync();

        var onixProducts = await _onixEditService.GetOnixProductsAsync();

        await _persistenceService.PersistProducts(onixProducts);

        await _persistenceService.UpdatePricesForBooksBasedOnTypes();
    }
}