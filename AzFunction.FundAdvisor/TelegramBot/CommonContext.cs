using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal class CommonContext : IContext
    {
        public ITelegramBotClient BotClient { get; set; }
        public Message Message { get; set; }
        public ILogger Logger { get; set; }
    }
}
