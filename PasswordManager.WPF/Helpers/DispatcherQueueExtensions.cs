using System.Windows.Dispatching;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Helpers;

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
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
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
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    // Overload for synchronous functions that return values
    public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<T> function)
    {
        var tcs = new TaskCompletionSource<T>();
        
        dispatcher.TryEnqueue(() =>
        {
            try
            {
                var result = function();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        
        return tcs.Task;
    }
}
