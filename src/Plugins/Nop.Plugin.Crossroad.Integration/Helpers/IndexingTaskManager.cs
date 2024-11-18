#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Crossroad.Integration.Services.Onix;
using Nop.Plugin.Crossroad.Integration.Services.Products;
using Nop.Services.Helpers;
using Nop.Services.Messages;
using Serilog;

namespace Nop.Plugin.Crossroad.Integration.Helpers;

public class IndexingTaskManager
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRunning = false;
    private ILogger? _logger;

    public IndexingTaskManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = null;
    }
    private void StartRunning(string batchId)
    {
        _isRunning = true;
        Log.CloseAndFlush();
        _logger = new LoggerConfiguration()
             .WriteTo.File($"Logs/Batch_{batchId}.log", rollingInterval: RollingInterval.Infinite)
            .CreateLogger();

    }
    private void Complete(DateTime dateTime)
    {
        var now = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        //Progress = new IndexingProgress($"Indexing done on {now}");
        LogInformation($"Onix edit product update done on {now}");
        _isRunning = false;
    }

    private void LogInformation(string message)
    {
        _logger?.Information(message);
    }

    private void LogError(string message, Exception ex)
    {
        _logger?.Error(ex, message);
    }

    public async Task<IList<(NotifyType Type, string Message)>> StartSync()
    {
        var messages = new List<(NotifyType, string)>();
        if (_isRunning)
        {
            LogInformation("Onix edit product update is already running");
            messages.Add((NotifyType.Error, "Onix edit product update is already running"));
            return messages;
        }


        string batchId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        StartRunning(batchId);

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
                    LogInformation("Onix start");

                    await onixLoginService.GetTokenAsync();
                    var i = 1;
                    LogInformation($"in Page {i}");
                    while (_isRunning)
                    {
                        var onixProducts = await onixEditService.GetOnixProductsAsync(page: i, pageSize: 100);
                        await persistenceService.PersistProducts(onixProducts, messages =>
                        {
                            foreach (var message in messages)
                                LogInformation(message);
                        });
                        await persistenceService.UpdatePricesForBooksBasedOnTypes();

                        if (onixProducts.Count < 100)
                        {
                            var now = dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, TimeZoneInfo.Utc, dateTimeHelper.DefaultStoreTimeZone);
                            Complete(now);
                        }

                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, ex);
            }
        });

        messages.Add((NotifyType.Success, "Onix edit product update is started"));
        return messages;
    }
}

