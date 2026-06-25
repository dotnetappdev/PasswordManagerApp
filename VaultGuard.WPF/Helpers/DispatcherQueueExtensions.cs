using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Extension methods for WPF Dispatcher to provide async operations similar to DispatcherQueue
/// </summary>
public static class DispatcherExtensions
{
    public static Task InvokeAsync(this Dispatcher dispatcher, Func<Task> function)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        dispatcher.InvokeAsync(async () =>
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
    
    public static Task InvokeAsync(this Dispatcher dispatcher, Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        dispatcher.InvokeAsync(() =>
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
    public static Task<T> InvokeAsync<T>(this Dispatcher dispatcher, Func<T> function)
    {
        var tcs = new TaskCompletionSource<T>();
        
        dispatcher.InvokeAsync(() =>
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
