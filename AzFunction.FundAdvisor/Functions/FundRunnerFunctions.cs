using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzFunction.FundAdvisor.Notification;
using DataAccess;
using DataAccess.Entities;
using DotNet.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AzFunction.FundAdvisor.Functions
{
    public class FundRunnerFunctions
    {
        private readonly MongoDbClient mongoDbClient;
        private readonly IReadOnlyList<INotificationClient> notificationClients;

        public FundRunnerFunctions(MongoClient mongoClient, IReadOnlyList<INotificationClient> notificationClients)
        {
            this.mongoDbClient = MongoDbClient.Create(mongoClient.GetDatabase(Constants.FundDbInMongoDB));
            this.notificationClients = notificationClients;
        }

        [FunctionName("FundRunner-MainTask")]
        public async Task MainTask(
            [TimerTrigger("0 * 7-23 * * *")]TimerInfo myTimer,
            ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IEnumerable<MongoFundInfoEntity> fundInfos = this.mongoDbClient
                .GetObjects<MongoFundInfoEntity>(_ => true)
                .Where(fundInfo => fundInfo.IsActive())
                .ToArray();

            logger.LogInformation($"Process Fund, total count {fundInfos.Count()}");
            await Task.WhenAll(fundInfos.Select(fundInfo => Task.Run(async () =>
            {
                await FunctionExtensions.RunWithErrorHandlerAsync(
                    async () => await ProcessFund(fundInfo, logger),
                    this.notificationClients.NotifyErrorHandler);
            })));

            logger.LogInformation($"Calculate the daily summary for {DateTime.Now:yyyy-MM-dd}");
            this.mongoDbClient.UpdateDailySummary(DateTime.Now);
        }

        private static bool IsTradeTime(FundValuation fundValuation)
        {
            // 如果当前是交易日和交易时间, 并且估值时间在交易时间内
            // only 10:00-15:00 for gz fund value
            return DateTime.Now.TimeOfDay >= Constants.FundStartTime &&
                   DateTime.Now.TimeOfDay <= Constants.FundEndTime &&
                   fundValuation.gztime >= DateTime.Now.Date.AddHours(10) &&
                   fundValuation.gztime <= DateTime.Now.Date.AddHours(15);
        }

        private async Task ProcessFund(MongoFundInfoEntity fundInfo, ILogger logger)
        {
            FundValuation fundValuation = await FundCommon.GetFundValuation(fundInfo.Id);
            logger.LogInformation($"Get FundValuation {fundValuation}");

            if (IsTradeTime(fundValuation))
            {
                logger.LogInformation($"on trade time {DateTime.Now.TimeOfDay}");

                MongoFundOperationEntity curOperation = this.mongoDbClient.GetObjects<MongoFundOperationEntity>(
                        o => o.OperateDate == DateTime.Now.Date && o.Id == fundInfo.Id &&
                             !o.IsPublished)
                    .SingleOrDefault();

                if (curOperation == null)
                {
                    // 根据之前生成今日
                    MongoFundOperationEntity prevOperation = this.mongoDbClient.GetObjects<MongoFundOperationEntity>(
                            o => o.OperateDate < DateTime.Now.Date && o.Id == fundInfo.Id)
                        .OrderByDescending(o => o.OperateDate)
                        .FirstOrDefault();

                    ArgumentValidator.ThrowIfNull($"Prev Operation {fundInfo.Id} - {DateTime.Now}", prevOperation);

                    curOperation = new MongoFundOperationEntity
                    {
                        UniqueObjectId = ObjectId.GenerateNewId(),
                        Id = prevOperation.Id,
                        Date = $"{DateTime.Now.Date:yyyy-MM-dd}",
                        OperateDate = DateTime.Now.Date,
                        // TODO: ExpectGrowthRate/TakeProfitRate will be moved to operation and calculate per day, and algorithm to implement
                        ExpectAsset = prevOperation.ExpectAsset + prevOperation.Shares.Value * prevOperation.NetAssetValue * fundInfo.ExpectGrowthRate,
                        Shares = prevOperation.Shares,
                        Cost = prevOperation.Cost,
                        IsPublished = false
                    };
                }

                // first time invest fund shares is null, will not be covered here
                if (curOperation.Shares != null)
                {
                    // TODO: refactor below this.tableClient operation
                    curOperation.NetAssetValue = fundValuation.gsz;
                    curOperation.Asset = curOperation.NetAssetValue * curOperation.Shares.Value;
                    curOperation.ExpectPurchaseAsset = Math.Max(curOperation.ExpectAsset - curOperation.Asset, 0);
                    curOperation.Profit = curOperation.Asset - curOperation.Cost;
                    curOperation.ProfitRatio = curOperation.Profit / curOperation.Cost;
                    this.mongoDbClient.UpsertObject(curOperation);
                }

                bool shouldNotify = curOperation.ExpectPurchaseAsset > 0 &&
                                    !curOperation.NotificationSent &&
                                    DateTime.Now.TimeOfDay >= Constants.FundClosingTime;

                if (shouldNotify)
                {
                    curOperation.NotificationSent = true;
                    this.mongoDbClient.UpsertObject(curOperation);

                    await this.notificationClients.SendMessagesWithRetry($"{fundInfo.Name} 需要加仓", new List<string>
                    {
                        $"基金代码: {fundInfo.Id}",
                        $"基金名称: {fundInfo.Name}",
                        $"加仓金额: {curOperation.ExpectPurchaseAsset}",
                        $"昨日净值: {fundValuation.dwjz}",
                        $"今日估值: {fundValuation.gsz}",
                        $"估算增值率: {fundValuation.gszzl}%",
                        $"估值时间: {fundValuation.gztime}"
                    }, logger);
                }

                bool shouldTakeProfitNotify = curOperation.ProfitRatio >= fundInfo.TakeProfitRatio &&
                                              DateTime.Now.TimeOfDay >= Constants.FundClosingTime;
                if (shouldTakeProfitNotify)
                {
                    await notificationClients.SendMessagesWithRetry($"{fundInfo.Name} 达到设定止盈率", new List<string>
                    {
                        $"基金代码: {fundInfo.Id}",
                        $"基金名称: {fundInfo.Name}",
                        $"基金份额: {curOperation.Shares}",
                        $"基金收益: {curOperation.Profit}",
                        $"基金成本: {curOperation.Cost}",
                        $"收益率: {curOperation.ProfitRatio:P}",
                        $"止盈率: {fundInfo.TakeProfitRatio:P}"
                    }, logger);
                }
            }
            else // 非交易时间，check final fund value
            {
                logger.LogInformation($"not trade time {DateTime.Now:s}");
                MongoFundOperationEntity curOperation = this.mongoDbClient.GetObjects<MongoFundOperationEntity>(
                        o => o.Id == fundInfo.Id && !o.IsPublished)
                    .OrderByDescending(o => o.OperateDate)
                    .FirstOrDefault();
                if (curOperation == null) return;

                FundNetValue fundNetValue = await FundCommon.GetFundNetValue(curOperation.Id);

                if (fundNetValue.jzrq.Date == curOperation.OperateDate.Date)
                {
                    // TODO: Refactor below logic
                    curOperation.Cost += curOperation.ActualPurchaseAsset;
                    curOperation.NetAssetValue = fundNetValue.dwjz;
                    if (curOperation.Shares != null)
                    {
                        curOperation.ExpectPurchaseAsset =
                            Math.Max(
                                curOperation.ExpectAsset - curOperation.NetAssetValue * curOperation.Shares.Value,
                                0);
                    }

                    curOperation.Shares = (curOperation.Shares ?? 0) + curOperation.ActualPurchaseAsset /
                        curOperation.NetAssetValue * (1 - fundInfo.PurchaseRate);
                    curOperation.Asset = curOperation.NetAssetValue * curOperation.Shares.Value;
                    curOperation.Profit = curOperation.Asset - curOperation.Cost;
                    curOperation.ProfitRatio = curOperation.Profit / curOperation.Cost;
                    curOperation.IsPublished = true;
                    this.mongoDbClient.UpsertObject(curOperation);

                    logger.LogInformation($"Final net asset value published: {fundNetValue.dwjz} for {curOperation}");

                    await notificationClients.SendMessagesWithRetry($"{curOperation.Id} 今日基金净值已更新", new List<string>
                    {
                        $"市值: {curOperation.Asset}",
                        $"净值: {curOperation.NetAssetValue}",
                        $"份额: {curOperation.Shares}",
                        $"买入: {curOperation.ActualPurchaseAsset}",
                        $"收益: {curOperation.Profit}",
                        $"成本: {curOperation.Cost}",
                        $"收益率: {curOperation.ProfitRatio:P}",
                        ""
                    }, logger);
                }
            }
        }
    }
}
