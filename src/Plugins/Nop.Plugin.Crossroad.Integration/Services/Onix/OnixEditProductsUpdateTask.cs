using System.Threading.Tasks;
using Nop.Plugin.Crossroad.Integration.Helpers;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Crossroad.Integration.Services.Onix;

public class OnixEditProductsUpdateTask : IScheduleTask
{
    private readonly IndexingTaskManager _indexingTaskManager;

    public OnixEditProductsUpdateTask(IndexingTaskManager indexingTaskManager)
    {
        _indexingTaskManager = indexingTaskManager;
    }

    public async Task ExecuteAsync()
    {
        await _indexingTaskManager.StartSync();
    }
}