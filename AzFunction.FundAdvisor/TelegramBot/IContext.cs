using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal interface IContext
    {
        ITelegramBotClient BotClient { get; set; }
        Message Message { get; set; }
        ILogger Logger { get; set; }
    }
}
