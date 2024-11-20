#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Crossroad.Integration.Services.Onix;
using Nop.Plugin.Crossroad.Integration.Services.Products;
using Nop.Services.Helpers;
using Serilog;

namespace Nop.Plugin.Crossroad.Integration.Helpers;

public class OnixEditSyncTaskManager
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRunning = false;
    private ILogger? _logger;

    public OnixEditSyncTaskManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = null;
    }

    private bool TryStart()
    {
        if (_isRunning)
            return false;

        _isRunning = true;
        string batchId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Log.CloseAndFlush();
        _logger = new LoggerConfiguration()
             .WriteTo.File($"Logs/Batch_{batchId}.log", rollingInterval: RollingInterval.Infinite)
            .CreateLogger();

        return true;
    }

    private void Complete()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LogInformation($"Onix edit product update done on {now}");
        _isRunning = false;
        Log.CloseAndFlush();
        _logger = null;
    }

    private void LogInformation(string message)
    {
        _logger?.Information(message);
    }

    private void LogError(string message)
    {
        _logger?.Error(message);
    }

    public async Task<bool> TryStartSync()
    {
        if (!TryStart())
            return false;

        // TBA cancellation token
        _ = Task.Run(async () =>
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dateTimeHelper = scope.ServiceProvider.GetRequiredService<IDateTimeHelper>();
                    var persistenceService = scope.ServiceProvider.GetRequiredService<IPersistenceService>();
                    var onixLoginService = scope.ServiceProvider.GetRequiredService<OnixLoginService>();
                    var onixEditService = scope.ServiceProvider.GetRequiredService<OnixEditService>();
                    LogInformation("OnixEditSync start");

                    await onixLoginService.GetTokenAsync();
                    var i = 0;
                    LogInformation($"Processing Page {i + 1}");
                    while (_isRunning)
                    {
                        LogInformation($"Getting Product From OnixEdit");
                        var onixProducts = await onixEditService.GetOnixProductsAsync(page: i, pageSize: 100);
                        LogInformation($"Number of Products obtained from OnixEdit: {onixProducts.Count}");

                        LogInformation($"Start persisting Product");
                        await persistenceService.PersistProducts(onixProducts, progress =>
                        {
                            if (progress.Successful)
                                LogInformation(progress.Message);
                            else
                                LogError(progress.Message);
                        });

                        LogInformation($"Start Update Prices For Books Based On Types");
                        await persistenceService.UpdatePricesForBooksBasedOnTypes(progress =>
                        {
                            if (progress.Successful)
                                LogInformation(progress.Message);
                            else
                                LogError(progress.Message);
                        });

                        if (onixProducts.Count < 100)
                        {
                            Complete();
                        }

                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                _isRunning = false;
                Log.CloseAndFlush();
                _logger = null;
            }
        });

        return true;
    }
}

