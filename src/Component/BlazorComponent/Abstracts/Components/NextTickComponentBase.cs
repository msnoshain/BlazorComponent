﻿namespace BlazorComponent;

public class NextTickComponentBase : ComponentBase, IDisposable
{
    private readonly Queue<(Func<Task>, Func<bool>)> _nextTickQueue = new();

    protected bool IsDisposed { get; private set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_nextTickQueue.Count > 0)
        {
            var queue = _nextTickQueue.ToArray();
            _nextTickQueue.Clear();

            foreach (var item in queue)
            {
                if (IsDisposed)
                {
                    return;
                }

                var (callback, moveNext) = item;

                if (moveNext())
                {
                    _nextTickQueue.Enqueue(item);
                }
                else
                {
                    await callback();
                }
            }
        }
    }

    private void NextTick(Func<Task> callback, Func<bool> moveNext)
    {
        _nextTickQueue.Enqueue((callback, moveNext));
    }

    protected void NextTick(Func<Task> callback)
    {
        NextTick(callback, () => false);
    }

    protected void NextTick(Action callback)
    {
        NextTick(() =>
        {
            callback.Invoke();
            return Task.CompletedTask;
        });
    }

    protected async Task NextTickIf(Func<Task> callback, Func<bool> @if)
    {
        if (@if.Invoke())
        {
            NextTick(callback);
        }
        else
        {
            await callback.Invoke();
        }
    }

    protected void NextTickIf(Action callback, Func<bool> @if)
    {
        if (@if.Invoke())
        {
            NextTick(callback);
        }
        else
        {
            callback.Invoke();
        }
    }

    protected void NextTickWhile(Func<Task> callback, Func<bool> @while)
    {
        if (@while.Invoke())
        {
            NextTick(callback, @while);
        }
        else
        {
            callback.Invoke();
        }
    }

    protected void NextTickWhile(Action callback, Func<bool> @while)
    {
        if (@while.Invoke())
        {
            NextTick(() =>
            {
                callback.Invoke();
                return Task.CompletedTask;
            }, @while);
        }
        else
        {
            callback.Invoke();
        }
    }

    protected async Task Retry(Func<Task> callback, Func<bool> @while, int retryTimes = 20, int delay = 100,
        CancellationToken cancellationToken = default)
    {
        if (retryTimes > 0 && !cancellationToken.IsCancellationRequested)
        {
            if (@while.Invoke())
            {
                retryTimes--;

                await Task.Delay(delay, cancellationToken);

                await Retry(callback, @while, retryTimes, delay, cancellationToken);
            }
            else
            {
                await callback.Invoke();
            }
        }
    }


    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;
        IsDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NextTickComponentBase()
    {
        // Finalizer calls Dispose(false)
        Dispose(false);
    }
}
