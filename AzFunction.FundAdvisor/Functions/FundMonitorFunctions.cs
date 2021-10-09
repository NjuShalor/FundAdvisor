using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzFunction.FundAdvisor.Notification;
using DataAccess;
using DataAccess.Entities;
using DotNet.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AzFunction.FundAdvisor.Functions
{
    public class FundMonitorFunctions
    {
        private readonly MongoDbClient mongoDbClient;
        private readonly IReadOnlyList<INotificationClient> notificationClients;

        public FundMonitorFunctions(MongoClient mongoClient, IReadOnlyList<INotificationClient> notificationClients)
        {
            this.mongoDbClient = MongoDbClient.Create(mongoClient.GetDatabase(Constants.FundDbInMongoDB));
            this.notificationClients = notificationClients;
        }

        [FunctionName("FundMonitor-BuyFundRegularCheck")]
        public async Task BuyFundRegularCheck([TimerTrigger("0 0 19 * * 1-5")] TimerInfo myTimer,ILogger logger)
        {
            Task task = FunctionExtensions.RunWithErrorHandlerAsync(async () =>
            {
                List<string> messages = new List<string>();
                this.mongoDbClient.GetObjects<MongoFundOperationEntity>(o => o.OperateDate == DateTime.Now.Date)
                    .Where(op => op.NotificationSent && op.ActualPurchaseAsset < 1e-3M)
                    .ToList()
                    .ForEach(op =>
                    {
                        messages.Add($"基金代码: {op.Id}");
                        messages.Add($"期待购入: {op.ExpectPurchaseAsset}");
                        messages.Add($"净值已出: {op.IsPublished}");
                        messages.Add($"通知已发送: {op.NotificationSent}");
                        messages.Add("");
                    });
                await this.notificationClients.SendMessagesWithRetry($"[基金实购更新操作 {DateTime.Now:yyyy-MM-dd HH:mm:ss}]", messages, logger);
            }, this.notificationClients.NotifyErrorHandler);

            await task;
        }

        [FunctionName("FundMonitor-MorningPost")]
        public async Task MorningPost([TimerTrigger("0 0 7 * * *")] TimerInfo myTimer, ILogger logger)
        {
            Task task = FunctionExtensions.RunWithErrorHandlerAsync(async () =>
            {
                var fundInfos = this.mongoDbClient
                    .GetObjects<MongoFundInfoEntity>(_ => true)
                    .Where(fundInfo => fundInfo.IsActive())
                    .Select(fundInfo => new { Id = fundInfo.Id, Name = fundInfo.Name })
                    .ToList();

                List<string> messages = this.mongoDbClient
                    .GetObjects<MongoFundOperationEntity>(fundOperation =>
                        fundOperation.OperateDate > Constants.MinimumQueryOperationDate)
                    .OrderByDescending(fundOperation => fundOperation.OperateDate)
                    .Take(fundInfos.Count)
                    .Where(fundOperation =>
                        fundInfos.Select(fundId => fundId.Id).Any(fundId => fundId == fundOperation.Id))
                    .SelectMany(fundOperation =>
                    {
                        MongoFundOperationEntity lastFundOperation = this.mongoDbClient
                            .GetObjects<MongoFundOperationEntity>(f =>
                                f.Id == fundOperation.Id &&
                                f.OperateDate > Constants.MinimumQueryOperationDate)
                            .Where(f => f.OperateDate < fundOperation.OperateDate.Date)
                            .OrderByDescending(_ => _.OperateDate)
                            .FirstOrDefault();

                        return new List<string>
                        {
                            $"基金编号: {fundOperation.Id}",
                            $"基金名称: {fundInfos.Single(fundInfo => fundInfo.Id == fundOperation.Id).Name}",
                            $"净值日期: {fundOperation.Date}",
                            $"单位净值: {fundOperation.NetAssetValue}",
                            $"净值增长: {(lastFundOperation == null ? 0 : (fundOperation.NetAssetValue - lastFundOperation.NetAssetValue) / lastFundOperation.NetAssetValue):P}",
                            $"当前资产: {fundOperation.Asset}",
                            $"今日收益: {(lastFundOperation == null ? fundOperation.Profit : fundOperation.Profit - lastFundOperation.Profit)}",
                            $"基金份额: {fundOperation.Shares}",
                            $"基金收益: {fundOperation.Profit}",
                            $"基金成本: {fundOperation.Cost}",
                            $"总收益率: {fundOperation.ProfitRatio:P}",
                            $"净值已出: {fundOperation.IsPublished}",
                            $"通知已发: {fundOperation.NotificationSent}",
                            ""
                        };
                    }).ToList();

                List<MongoFundDailySummaryEntity> latest2Summaries = this.mongoDbClient
                    .GetObjects<MongoFundDailySummaryEntity>(s => s.OperateDate > Constants.MinimumQueryOperationDate)
                    .OrderByDescending(_ => _.OperateDate)
                    .Take(2)
                    .ToList();

                MongoFundDailySummaryEntity currentSummary = latest2Summaries.First();
                MongoFundDailySummaryEntity lastSummary = latest2Summaries.Count == 2 ? latest2Summaries.Last() : null;

                messages.AddRange(new List<string>
                {
                    "基金收益汇总",
                    $"日期  : {currentSummary.Date}",
                    $"总收益: {currentSummary.Profit}",
                    $"日收益: {(lastSummary == null ? currentSummary.Profit : currentSummary.Profit - lastSummary.Profit)}",
                    $"成本  : {currentSummary.Cost}",
                    $"收益率: {currentSummary.ProfitRatio:P}"
                });

                string title = $"[每日基金报告 {DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
                logger.LogInformation(string.Join("\n", messages));
                await notificationClients.SendMessagesWithRetry(title, messages, logger);

            }, this.notificationClients.NotifyErrorHandler);

            await task;
        }

        [FunctionName("FundMonitor-ReferenceProfitPost")]
        public async Task ReferenceProfitPost([TimerTrigger("0 55 14 * * 1-5")] TimerInfo myTimer, ILogger logger)
        {
            Task task = FunctionExtensions.RunWithErrorHandlerAsync(async () =>
            {
                IEnumerable<MongoFundInfoEntity> fundInfos =
                    mongoDbClient.GetObjects<MongoFundInfoEntity>(_ => true).Where(f => f.IsActive());

                List<string> messages = fundInfos
                    .Select(fi => $"Id: {fi.Id}\n" +
                                  $"Name: {fi.Name}\n" +
                                  "Profits:\n" +
                                  $"Today:{mongoDbClient.CalculateProfit(fi.Id, 1):P}\n" +
                                  $"Avg 7d:{mongoDbClient.CalculateProfit(fi.Id, 7):P}\n" +
                                  $"Avg 14d:{mongoDbClient.CalculateProfit(fi.Id, 14):P}\n" +
                                  $"Avg 1m:{mongoDbClient.CalculateProfit(fi.Id, 30):P}\n" +
                                  $"Avg 2m:{mongoDbClient.CalculateProfit(fi.Id, 60):P}\n" +
                                  $"Avg 3m:{mongoDbClient.CalculateProfit(fi.Id, 90):P}\n" +
                                  $"Avg 6m:{mongoDbClient.CalculateProfit(fi.Id, 180):P}\n" +
                                  $"Avg 1y:{mongoDbClient.CalculateProfit(fi.Id, 360):P}\n" +
                                  $"Fund Max Profit Record:\n{mongoDbClient.GetMaxProfitFundOperation(fi.Id)}")
                    .ToList();

                string title = $"[收盘前基金收益参考 {DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
                logger.LogInformation(string.Join("\n", messages));
                await notificationClients.SendMessagesWithRetry(title, messages, logger);
            }, notificationClients.NotifyErrorHandler);

            await task;
        }
    }
}
