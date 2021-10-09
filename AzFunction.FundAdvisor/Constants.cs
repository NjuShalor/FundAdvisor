using System;

namespace AzFunction.FundAdvisor
{
    internal static class Constants
    {
        public static readonly string BarkDeviceId = Environment.GetEnvironmentVariable("Bark.DeviceId", EnvironmentVariableTarget.Process);
        public static readonly string TelegramBotToken = Environment.GetEnvironmentVariable("TelegramBot.Token", EnvironmentVariableTarget.Process);
        public static readonly string TelegramBotUserId = Environment.GetEnvironmentVariable("TelegramBot.UserId", EnvironmentVariableTarget.Process);
        public static readonly string MongoDbConnectionString= Environment.GetEnvironmentVariable("MongoDb.ConnectionString", EnvironmentVariableTarget.Process);

        public static readonly string FundDbInMongoDB = "Fund";

        public static readonly TimeSpan FundClosingTime  = TimeSpan.FromHours(14.9166666667);
        public static readonly TimeSpan FundStartTime  = TimeSpan.FromHours(10);
        public static readonly TimeSpan FundEndTime  = TimeSpan.FromHours(15);

        public static DateTime MinimumQueryOperationDate => DateTime.Now.AddDays(-30).Date;
    }
}
