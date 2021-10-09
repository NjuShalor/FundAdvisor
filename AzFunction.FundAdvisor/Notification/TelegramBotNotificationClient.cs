using System.Collections.Generic;
using System.Threading.Tasks;
using AzFunction.FundAdvisor.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AzFunction.FundAdvisor.Notification
{
    internal class TelegramBotNotificationClient : INotificationClient
    {
        private readonly ITelegramBotClient botClient;

        public TelegramBotNotificationClient(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
        }

        async Task INotificationClient.SendMessageAsync(string title, IEnumerable<string> messages)
        {
            string messageToSend =string.Join("\n", messages);

            await botClient
                .SendTextMessageWithDocumentFallbackAsync(
                    new ChatId(Constants.TelegramBotUserId),
                    title,
                    messageToSend);
        }
    }
}
