using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Helpers;

public static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this DispatcherQueue dispatcher, Func<Task> function)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        dispatcher.TryEnqueue(async () =>
        {
            try
            {
                await function();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    public static Task EnqueueAsync(this DispatcherQueue dispatcher, Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        dispatcher.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
}
