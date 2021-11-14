using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Genius.PriceChecker.Core.Services;

internal sealed class ProductPriceTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasksCollection = new();
    private readonly Thread _mainThread;

    public ProductPriceTaskScheduler()
    {
        _mainThread = new Thread(new ThreadStart(Execute));
        if (!_mainThread.IsAlive)
        {
            _mainThread.Start();
        }
    }

    public void Dispose()
    {
        _mainThread.Interrupt();
        _tasksCollection.CompleteAdding();
        _tasksCollection.Dispose();
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return _tasksCollection.ToArray();
    }

    protected override void QueueTask(Task task)
    {
        if (task != null)
        {
            _tasksCollection.Add(task);
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }

    private void Execute()
    {
        try
        {
            foreach (var task in _tasksCollection.GetConsumingEnumerable())
            {
                TryExecuteTask(task);
            }
        }
        catch (ObjectDisposedException)
        {
            // Just break
        }
    }
}
