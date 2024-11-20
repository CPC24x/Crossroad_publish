using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Crossroad.Integration.Helpers;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixEditProductsUpdateTask : IScheduleTask
{
    private readonly OnixEditSyncTaskManager _onixSyncTaskManager;

    public OnixEditProductsUpdateTask(OnixEditSyncTaskManager onixSyncTaskManager)
    {
        _onixSyncTaskManager = onixSyncTaskManager;
    }

    public async Task ExecuteAsync()
    {
        if (!await _onixSyncTaskManager.TryStartSync())
            throw new NopException("OnixEdit currently already start");
    }

    public record ProgressReport(string Message, bool Successful = true);

}