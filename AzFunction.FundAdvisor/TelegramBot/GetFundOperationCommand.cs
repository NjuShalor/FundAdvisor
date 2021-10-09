using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using DataAccess.Entities;
using Microsoft.Extensions.Logging;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class GetFundOperationCommand : Command
    {
        private readonly Options options;

        public GetFundOperationCommand(FundContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            IEnumerable<MongoFundOperationEntity> fundOperations = ((FundContext)this.Context).MongoDbClient
                .GetObjects<MongoFundOperationEntity>(this.options.Filter)
                .OrderByDescending(fo => DateTime.Parse(fo.Date));

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            IEnumerable<string> messages = fundOperations.Select(fo => fo.ToString()).ToList();
            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                string.Join("\n", messages),
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Get-FundOperation")]
        internal sealed class Options
        {
            [Option("Filter")]
            public string Filter { get; set; } = "{}";
        }
    }
}
