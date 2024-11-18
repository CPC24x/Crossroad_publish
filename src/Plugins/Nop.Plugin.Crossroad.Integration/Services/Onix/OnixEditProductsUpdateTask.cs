using System;
using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Services.Products;
using Nop.Services.Helpers;
using Nop.Services.ScheduleTasks;
using Microsoft.Extensions.DependencyInjection;
using Nop.Services.Configuration;
using Serilog;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixEditProductsUpdateTask : IScheduleTask
{
    private readonly IPersistenceService _persistenceService;
    private readonly OnixEditService _onixEditService;
    private readonly OnixLoginService _onixLoginService;
    private readonly IServiceProvider _serviceProvider;
    public bool IsRunning = false;
    private ILogger _logger;

    public OnixEditProductsUpdateTask(
        IPersistenceService persistenceService,
        OnixEditService onixEditService,
        OnixLoginService onixLoginService,
        IServiceProvider serviceProvider)
    {
        _persistenceService = persistenceService;
        _onixEditService = onixEditService;
        _onixLoginService = onixLoginService;
        _serviceProvider = serviceProvider;
        _logger = null;
    }

    public void StartRunning(string batchId)
    {
        IsRunning = true;
        Log.CloseAndFlush();
        _logger = new LoggerConfiguration()
             .WriteTo.File($"Logs/Batch_{batchId}.log", rollingInterval: RollingInterval.Infinite)
            .CreateLogger();

    }

    public void Complete(DateTime dateTime)
    {
        var now = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        //Progress = new IndexingProgress($"Indexing done on {now}");
        LogInformation($"Onix edit product update done on {now}");
        IsRunning = false;
    }
    public void LogInformation(string message)
    {
        _logger?.Information(message);
    }

    public void LogError(string message, Exception ex)
    {
        _logger?.Error(ex, message);
    }

    public async Task ExecuteAsync()
    {

        if (IsRunning)
            LogInformation("Onix edit product update is already running");

        string batchId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        StartRunning(batchId);

        // TBA cancellation token
        _ = Task.Run(async () =>
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    LogInformation("Onix start");

                    await _onixLoginService.GetTokenAsync();
                    var i = 1;
                    while (IsRunning)
                    {
                        var onixProducts = await _onixEditService.GetOnixProductsAsync(page: i, pageSize: 100);

                        await _persistenceService.PersistProducts(onixProducts);
                        await _persistenceService.UpdatePricesForBooksBasedOnTypes();

                        if (onixProducts.Count <= 100)
                        {
                            var dateTimeHelper = scope.ServiceProvider.GetRequiredService<IDateTimeHelper>();
                            var now = dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, TimeZoneInfo.Utc, dateTimeHelper.DefaultStoreTimeZone);
                            Complete(now);
                        }
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                //ErrorMessage += "Error occur while indexing the data " + ex.Message;
                //IsRunning = false;
                LogError(ex.Message, ex);
            }
        });

    }
}