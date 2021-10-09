using System.Collections.Generic;
using AzFunction.FundAdvisor.Notification;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Telegram.Bot;

[assembly: FunctionsStartup(typeof(AzFunction.FundAdvisor.Functions.Startup))]
namespace AzFunction.FundAdvisor.Functions
{
    public class Startup: FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<MongoClient>(s =>
            {
                MongoClient mongoClient = new MongoClient(Constants.MongoDbConnectionString);
                return mongoClient;
            });

            builder.Services.AddSingleton<IReadOnlyList<INotificationClient>>(s => new List<INotificationClient>
            {
                new TelegramBotNotificationClient(new TelegramBotClient(Constants.TelegramBotToken))
            });
        }
    }
}
