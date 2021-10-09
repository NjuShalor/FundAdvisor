using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzFunction.FundAdvisor.Functions;
using CommandLine;
using Microsoft.Azure.WebJobs;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal sealed class GetAzFunctionsCommand : Command
    {
        private readonly Options options;

        public GetAzFunctionsCommand(CommonContext context, Options options) : base(context)
        {
            this.options = options;
        }

        public override async Task Execute()
        {
            ConcurrentBag<AzureFunctionInfo> azureFunctionInfos = new ConcurrentBag<AzureFunctionInfo>();

            List<MethodInfo> methods = new List<MethodInfo>();
            methods.AddRange(typeof(FundMonitorFunctions).GetMethods());
            methods.AddRange(typeof(FundRunnerFunctions).GetMethods());
            methods.AddRange(typeof(TelegramBotFunctions).GetMethods());

            methods.AsParallel().ForAll(method =>
            {
                FunctionNameAttribute functionNameAttribute = method.GetCustomAttribute(typeof(FunctionNameAttribute)) as FunctionNameAttribute;
                List<Attribute> triggerAttributes = method.GetParameters()
                    .SelectMany(paramInfo => paramInfo.GetCustomAttributes())
                    .Where(attr => attr is TimerTriggerAttribute || attr is HttpTriggerAttribute).ToList();

                if (functionNameAttribute != null && triggerAttributes.Any())
                {
                    triggerAttributes.AsParallel().ForAll(attr =>
                    {
                        azureFunctionInfos.Add(new AzureFunctionInfo
                        {
                            Function = functionNameAttribute.Name,
                            Trigger = attr.GetType().Name
                        });
                    });
                }
            });

            VerbAttribute verbAttribute = typeof(Options).GetCustomAttribute(typeof(VerbAttribute)) as VerbAttribute;
            string title = $"[{verbAttribute.Name}]";
            await this.Context.BotClient.SendTextMessageWithDocumentFallbackAsync(
                this.Context.Message.Chat,
                title,
                string.Join("\n",
                    azureFunctionInfos
                        .OrderBy(info => info.Trigger)
                        .ThenBy(info => info.Function)
                        .Select(x => x.ToString())),
                replyToMessageId: this.Context.Message.MessageId);
        }

        private struct AzureFunctionInfo
        {
            public string Function { get; set; }
            public string Trigger { get; set; }

            public override string ToString()
            {
                return $"{nameof(Function)}: {Function}\n{nameof(Trigger)}: {Trigger}\n";
            }
        }

        [Verb("Get-AzFunctions", HelpText = "List all of azure functions")]
        internal sealed class Options { }
    }
}
