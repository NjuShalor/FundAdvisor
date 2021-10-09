using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AzFunction.FundAdvisor.Notification
{
    internal static class NotificationExtensions
    {
        public static async Task SendMessagesWithRetry(
            this IReadOnlyList<INotificationClient> notificationClients,
            string title,
            IReadOnlyList<string> messages,
            ILogger logger = null,
            int maxRetry = 3)
        {
            if (!messages.Any()) return;

            bool isOk = false;
            for (int i = 0; i <= maxRetry + 1 && !isOk; i++)
            {
                isOk = true;
                foreach (INotificationClient client in notificationClients)
                {
                    try
                    {
                        await client.SendMessageAsync(title, messages);
                    }
                    catch (Exception ex)
                    {
                        isOk = false;
                        logger?.LogWarning(ex.ToString());
                    }
                }
            }
        }

        public static async Task NotifyErrorHandler(this IReadOnlyList<INotificationClient> notifiers, Exception ex)
        {
            await notifiers.SendMessagesWithRetry(ex.Message, new List<string> { ex.ToString(), "\n", ex.InnerException?.ToString() });
        }
    }
}
