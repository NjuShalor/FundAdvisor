using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using DataAccess;
using DataAccess.Entities;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class GetFundInfoCommand : Command
    {
        private readonly Options options;

        public GetFundInfoCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            MongoDbClient mongoDbClient = ((FundContext) this.Context).MongoDbClient;
            IEnumerable<MongoFundInfoEntity> fundInfos = mongoDbClient.GetObjects<MongoFundInfoEntity>(this.options.Filter);

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            IEnumerable<string> messages = fundInfos
                .Select(fi => $"{fi}\n" +
                              "Profits:\n" +
                              $"Avg 7d:{mongoDbClient.CalculateProfit(fi.Id, 7):P}\n" +
                              $"Avg 14d:{mongoDbClient.CalculateProfit(fi.Id, 14):P}\n" +
                              $"Avg 1m:{mongoDbClient.CalculateProfit(fi.Id, 30):P}\n" +
                              $"Avg 2m:{mongoDbClient.CalculateProfit(fi.Id, 60):P}\n" +
                              $"Avg 3m:{mongoDbClient.CalculateProfit(fi.Id, 90):P}\n" +
                              $"Avg 6m:{mongoDbClient.CalculateProfit(fi.Id, 180):P}\n" +
                              $"Avg 1y:{mongoDbClient.CalculateProfit(fi.Id, 360):P}\n" +
                              $"Fund Max Profit Record:\n{mongoDbClient.GetMaxProfitFundOperation(fi.Id)}")
                .ToList();

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                string.Join("\n", messages),
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Get-FundInfo", HelpText = "Get fund info")]
        internal sealed class Options
        {
            [Option("Filter")]
            public string Filter { get; set; } = "{}";
        }
    }
}
