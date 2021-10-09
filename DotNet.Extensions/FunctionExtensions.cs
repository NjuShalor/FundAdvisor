using System;
using System.Threading.Tasks;

namespace DotNet.Extensions
{
    internal static class FunctionExtensions
    {
        public static async Task RunWithErrorHandlerAsync(Func<Task> action, Func<Exception, Task> errorHandler = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await errorHandler?.Invoke(ex);
            }
        }
    }
}
