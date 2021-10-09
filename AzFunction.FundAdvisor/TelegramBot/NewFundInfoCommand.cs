using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using DataAccess;
using DataAccess.Entities;
using MongoDB.Bson;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class NewFundInfoCommand : Command
    {
        private readonly Options options;

        public NewFundInfoCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            MongoDbClient mongoDbClient = ((FundContext) this.Context).MongoDbClient;
            MongoFundInfoEntity fundInfo = new MongoFundInfoEntity()
            {
                Id = this.options.Id,
                Name = this.options.Name,
                StartTime = DateTime.Now.Date,
                EndTime = null,
                PurchaseRate = this.options.PurchaseRate,
                TakeProfitRatio = this.options.TakeProfitRatio,
                ExpectGrowthRate = this.options.ExpectGrowthRate
            };

            MongoFundInfoEntity fundInfoInStore = mongoDbClient
                .GetObjects<MongoFundInfoEntity>(f => f.Id == this.options.Id)
                .SingleOrDefault();

            fundInfo.UniqueObjectId = fundInfoInStore?.UniqueObjectId ?? ObjectId.GenerateNewId();
            mongoDbClient.NewObject(fundInfo);

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                fundInfo.ToString(),
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("New-FundInfo")]
        internal sealed class Options
        {
            [Option("Id", Required = true)]
            public string Id { get; set; }

            [Option("Name", Required = true)]
            public string Name { get; set; }

            [Option("PurchaseRate", Required = true)]
            public decimal PurchaseRate { get; set; }

            [Option("TakeProfitRatio", Required = true)]
            public decimal TakeProfitRatio { get; set; }

            [Option("ExpectGrowthRate", Required = true)]
            public decimal ExpectGrowthRate { get; set; }
        }
    }
}
