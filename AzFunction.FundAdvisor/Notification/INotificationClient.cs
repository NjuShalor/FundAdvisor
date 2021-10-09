using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzFunction.FundAdvisor.Notification
{
    public interface INotificationClient
    {
        Task SendMessageAsync(string title, IEnumerable<string> message);
    }
}
