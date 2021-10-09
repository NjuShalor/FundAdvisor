using DataAccess;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal class FundContext : IContext
    {
        public ITelegramBotClient BotClient { get; set; }
        public Message Message { get; set; }
        public ILogger Logger { get; set; }
        public MongoDbClient MongoDbClient { get; set; }
    }
}
