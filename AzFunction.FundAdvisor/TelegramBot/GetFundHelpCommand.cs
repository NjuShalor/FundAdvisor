using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class GetFundHelpCommand : Command
    {
        private readonly Options options;

        public GetFundHelpCommand(CommonContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";

            string[] commands = new string[]
            {
                "Ping",
                "Get-AzFunctions",
                "Get-FundHelp",
                "Buy-Fund --Id <string> --Value <decimal>]",
                "Sell-Fund --Id <string> [--DoesSellAll <bool>] --Shares <decimal>",
                "New-FundInfo --Id <string> --Name <string> --PurchaseRate <decimal> --TakeProfitRatio <decimal> --ExpectGrowthRate <decimal>",
                "Update-FundSummary [--Date <DateTime, \"2021-09-10T00:00:00+08:00\">]",
                "Get-FundInfo [--Filter <string, e.g: \"{Id: '110022'}\">]",
                "Get-FundOperation [--Filter <string, e.g: \"{Date: '2021-09-10'}\">]",
                "Get-FundSummary [--LastDays <int>]"
            };

            string text = string.Join("\n", commands);

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                text,
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Get-FundHelp")]
        internal sealed class Options { }
    }
}
