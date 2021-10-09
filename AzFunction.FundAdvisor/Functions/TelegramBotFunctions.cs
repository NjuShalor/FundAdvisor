using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzFunction.FundAdvisor.TelegramBot;
using CommandLine;
using DataAccess;
using DotNet.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AzFunction.FundAdvisor.Functions
{
    public class TelegramBotFunctions
    {
        private readonly MongoDbClient mongoDbClient;
        private readonly ITelegramBotClient botClient;

        public TelegramBotFunctions(MongoClient mongoClient)
        {
            this.mongoDbClient = MongoDbClient.Create(mongoClient.GetDatabase(Constants.FundDbInMongoDB));
            this.botClient = new TelegramBotClient(Constants.TelegramBotToken);
        }

        [FunctionName("TelegramBot-WebHook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "telegrambot/message")] HttpRequest req,
            ILogger logger)
        {
            logger.LogInformation("Invoke telegram update function");

            string body = await req.ReadAsStringAsync();
            Update update = JsonConvert.DeserializeObject<Update>(body);

            // TODO: AllowList for register users
            if (update?.Type == UpdateType.Message &&
                update.Message.Chat.Id == long.Parse(Constants.TelegramBotUserId))
            {
                Message message = update.Message;

                await FunctionExtensions.RunWithErrorHandlerAsync(
                async () =>
                {
                    IEnumerable<string> args = CommandLineExtensions.SplitCommandLineIntoArguments(message.Text.Trim(), removeHashComments: true);
                    TextWriter parserOutputWriter = new StringWriter();
                    Parser parser = new Parser(settings => settings.HelpWriter = parserOutputWriter);
                    await this.ParseArguments(parser, args, message, parserOutputWriter, logger);
                },
                async exception =>
                {
                    string exceptionMessage = $"{exception}\n{exception.InnerException}";
                    await this.botClient.SendTextMessageWithDocumentFallbackAsync(
                        message.Chat,
                        $"[{exception.Message}]",
                        exceptionMessage,
                        replyToMessageId: message.MessageId);
                });
            }
            else if (update?.Type == UpdateType.CallbackQuery)
            {
                // TODO: Coming soon
                throw new NotImplementedException();
            }

            return new OkResult();
        }

        private Task ParseArguments(Parser parser, IEnumerable<string> args, Message message, TextWriter parserOutputWriter, ILogger logger)
        {
            Func<Func<Task>, int> RunWithSuccessCode = action => { action().Wait(); return 0; };
            parser.ParseArguments<
                    /* CommonContext Commands*/
                    GetFundHelpCommand.Options,
                    PingCommand.Options,
                    GetAzFunctionsCommand.Options,
                    /* FundContext Commands*/
                    BuyFundCommand.Options,
                    SellFundCommand.Options,
                    NewFundInfoCommand.Options,
                    GetFundInfoCommand.Options,
                    GetFundOperationCommand.Options,
                    GetFundSummaryCommand.Options,
                    UpdateFundSummaryCommand.Options>(args)
                .MapResult(
                    /* CommonContext Commands*/
                    (GetFundHelpCommand.Options o) =>
                        RunWithSuccessCode(new GetFundHelpCommand(this.GetCommonContext(message, logger), o).Execute),
                    (PingCommand.Options o) =>
                        RunWithSuccessCode(new PingCommand(this.GetCommonContext(message, logger), o).Execute),
                    (GetAzFunctionsCommand.Options o) =>
                        RunWithSuccessCode(new GetAzFunctionsCommand(this.GetCommonContext(message, logger), o).Execute),
                    /* FundContext Commands*/
                    (BuyFundCommand.Options o) =>
                        RunWithSuccessCode(new BuyFundCommand(this.GetFundContext(message, logger), o).Execute),
                    (SellFundCommand.Options o) =>
                        RunWithSuccessCode(new SellFundCommand(this.GetFundContext(message, logger), o).Execute),
                    (NewFundInfoCommand.Options o) =>
                        RunWithSuccessCode(new NewFundInfoCommand(this.GetFundContext(message, logger), o).Execute),
                    (GetFundInfoCommand.Options o) =>
                        RunWithSuccessCode(new GetFundInfoCommand(this.GetFundContext(message, logger), o).Execute),
                    (GetFundOperationCommand.Options o) =>
                        RunWithSuccessCode(new GetFundOperationCommand(this.GetFundContext(message, logger), o).Execute),
                    (GetFundSummaryCommand.Options o) =>
                        RunWithSuccessCode(new GetFundSummaryCommand(this.GetFundContext(message, logger), o).Execute),
                    (UpdateFundSummaryCommand.Options o) =>
                        RunWithSuccessCode(new UpdateFundSummaryCommand(this.GetFundContext(message, logger), o).Execute),
                    // TODO: process errors, combine with HelpWriter result
                    errors =>
                    {
                        List<string> errorMessages = errors.Select(err => string.Join(",", err.Tag, err.StopsProcessing, err.ToString())).ToList();
                        errorMessages.Add(parserOutputWriter.ToString());
                        logger.LogWarning($"{string.Join("\n", errorMessages)}");
                        return errors.Count();
                    });

            return Task.CompletedTask;
        }

        private CommonContext GetCommonContext(Message message, ILogger logger)
        {
            CommonContext context = new CommonContext
            {
                BotClient = this.botClient,
                Logger = logger,
                Message = message
            };

            return context;
        }

        private FundContext GetFundContext(Message message, ILogger logger)
        {
            FundContext context = new FundContext
            {
                BotClient = this.botClient,
                Logger = logger,
                Message = message,
                MongoDbClient = this.mongoDbClient
            };

            return context;
        }
    }
}
