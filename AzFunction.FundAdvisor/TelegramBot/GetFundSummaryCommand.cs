using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using DataAccess.Entities;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class GetFundSummaryCommand : Command
    {
        private readonly Options options;

        public GetFundSummaryCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            IEnumerable<MongoFundDailySummaryEntity> dailySummaries = ((FundContext)this.Context).MongoDbClient
                .GetObjects<MongoFundDailySummaryEntity>(_ => true)
                .OrderByDescending(ds => ds.OperateDate)
                .Take(this.options.LastDays);

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            IEnumerable<string> messages = dailySummaries.Select(s => s + "\n").ToList();

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                string.Join("\n", messages),
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Get-FundSummary")]
        internal sealed class Options
        {
            [Option("LastDays")]
            public int LastDays { get; set; } = 5;
        }
    }
}
