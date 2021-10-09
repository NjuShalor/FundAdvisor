using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using DataAccess;
using DataAccess.Entities;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class SellFundCommand : Command
    {
        private readonly Options options;

        public SellFundCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            // fund sell fundid all
            FundContext context = (FundContext) this.Context;
            context.MongoDbClient.SellFund(this.options.Id, this.options.Shares, this.options.DoesSellAll);

            MongoFundInfoEntity fundInfo = context.MongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == this.options.Id).SingleOrDefault();
            MongoFundOperationEntity fundOperation = context.MongoDbClient
                .GetObjects<MongoFundOperationEntity>(f => f.OperateDate == DateTime.Now.Date && f.Id == this.options.Id)
                .SingleOrDefault();

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";
            string message = string.Join("\n", new List<string>
            {
                    $"基金赎回: {fundInfo.Name} - {fundInfo.Id}",
                    this.options.DoesSellAll ? "赎回全部份额\n" : $"赎回部分份额 {this.options.Shares}\n",
                    "",
                    "Fund Info",
                    $"{fundInfo}",
                    "",
                    "Fund Operation",
                    $"{fundOperation}"
            });

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                message,
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Sell-Fund")]
        internal sealed class Options
        {
            [Option("Id", Required = true)]
            public string Id { get; set; }

            [Option("DoesSellAll")]
            public bool DoesSellAll { get; set; } = false;

            [Option("Shares", Required = true)]
            public decimal Shares { get; set; }
        }
    }
}
