using System;
using System.Threading.Tasks;

namespace NexNux.App.Utilities;

public static class TaskHelper
{
    public static async Task<T?> TryRunAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            DialogHelper.ShowDialog("Error", ex.Message);
            return default;
        }
    }
}