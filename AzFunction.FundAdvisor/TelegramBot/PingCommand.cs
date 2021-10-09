using System;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class PingCommand : Command
    {
        private readonly Options options;

        public PingCommand(CommonContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";


            string text = string.Join("\n",
                $"It Works! Welcome {this.Context.Message.Chat.FirstName}({this.Context.Message.Chat.Username}-{this.Context.Message.Chat.Id}), I am {Environment.MachineName}",
                $"{nameof(Environment.UserName)}: {Environment.UserName}",
                $"{nameof(Environment.UserDomainName)}: {Environment.UserDomainName}",
                $"{nameof(Environment.UserInteractive)}: {Environment.UserInteractive}",
                $"{nameof(Environment.OSVersion)}: {Environment.OSVersion}",
                $"{nameof(Environment.Is64BitOperatingSystem)}: {Environment.Is64BitOperatingSystem}",
                $"{nameof(Environment.Is64BitProcess)}: {Environment.Is64BitProcess}",
                $"{nameof(Environment.CommandLine)}: {Environment.CommandLine}",
                $"{nameof(Environment.CurrentDirectory)}: {Environment.CurrentDirectory}",
                $"{nameof(Environment.CurrentManagedThreadId)}: {Environment.CurrentManagedThreadId}",
                $"{nameof(Environment.SystemDirectory)}: {Environment.SystemDirectory}",
                $"{nameof(Environment.SystemPageSize)}: {Environment.SystemPageSize}",
                $"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}",
                $"{nameof(Environment.TickCount)}: {Environment.TickCount}",
                $"{nameof(Environment.TickCount64)}: {Environment.TickCount64}",
                $"{nameof(Environment.Version)}: {Environment.Version}",
                $"{nameof(Environment.WorkingSet)}: {Environment.WorkingSet}",
                $"Assembly FullName: {Assembly.GetExecutingAssembly().FullName}",
                $"Assembly CodeBase: {Assembly.GetExecutingAssembly().CodeBase}",
                $"Assembly EntryPoint: {Assembly.GetExecutingAssembly().EntryPoint}",
                $"{nameof(AppDomain.CurrentDomain.FriendlyName)}: {AppDomain.CurrentDomain.FriendlyName}",
                $"{nameof(AppDomain.CurrentDomain.BaseDirectory)}: {AppDomain.CurrentDomain.BaseDirectory}",
                $"{nameof(AppDomain.CurrentDomain.SetupInformation.ApplicationBase)}: {AppDomain.CurrentDomain.SetupInformation.ApplicationBase}",
                $"{nameof(AppContext.BaseDirectory)}: {AppContext.BaseDirectory}");

            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                text,
                replyToMessageId: this.Context.Message.MessageId);
        }

        [Verb("Ping")]
        internal sealed class Options { }
    }
}
