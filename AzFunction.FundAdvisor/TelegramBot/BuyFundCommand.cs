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
    internal sealed class BuyFundCommand : Command
    {
        private readonly Options options;

        public BuyFundCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            ((FundContext)this.Context).MongoDbClient.BuyFund(this.options.Id, this.options.Value);

            MongoFundInfoEntity fundInfo = ((FundContext) this.Context).MongoDbClient
                .GetObjects<MongoFundInfoEntity>(fi => fi.Id == this.options.Id)
                .Single();

            MongoFundOperationEntity fundOperation = ((FundContext) this.Context).MongoDbClient
                .GetObjects<MongoFundOperationEntity>(o => o.Id == this.options.Id && o.OperateDate == DateTime.Now.Date)
                .Single();

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";
            string message = string.Join("\n", new List<string>
            {
                $"基金Id: {fundInfo.Id}",
                $"基金名称: {fundInfo.Name}",
                $"购买日期: {fundOperation.Date}",
                $"期望买入: {fundOperation.ExpectPurchaseAsset}",
                $"实际买入:{fundOperation.ActualPurchaseAsset}"
            });

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                message,
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Buy-Fund")]
        internal sealed class Options
        {
            [Option("Id", Required = true)]
            public string Id { get; set; }

            [Option("Value", Required = true)]
            public decimal Value { get; set; }
        }
    }
}