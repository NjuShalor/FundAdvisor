using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using DataAccess;
using DataAccess.Entities;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class UpdateFundSummaryCommand:Command
    {
        private readonly Options options;

        public UpdateFundSummaryCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            ((FundContext) this.Context).MongoDbClient.UpdateDailySummary(this.options.Date);

            MongoFundDailySummaryEntity dailySummary = ((FundContext) this.Context).MongoDbClient
                .GetObjects<MongoFundDailySummaryEntity>(ds => ds.OperateDate == this.options.Date)
                .SingleOrDefault();

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                dailySummary.ToString(),
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Update-FundSummary")]
        internal sealed class Options
        {
            [Option("Date")]
            public DateTime Date { get; set; } = DateTime.Now;
        }
    }
}
