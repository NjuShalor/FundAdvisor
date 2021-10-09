using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Entities;
using DotNet.Extensions;
using MongoDB.Bson;

namespace DataAccess
{
    internal static class MongoFundActionExtensions
    {
        public static void UpdateDailySummary(this MongoDbClient mongoDbClient, DateTime operateDate)
        {
            IEnumerable<MongoFundOperationEntity> fundOperations = mongoDbClient
                .GetObjects<MongoFundOperationEntity>(o => o.IsPublished && o.OperateDate == operateDate.Date)
                .ToList();

            if (!fundOperations.Any()) return;

            MongoFundDailySummaryEntity dailySummary = fundOperations.Select(o => new MongoFundDailySummaryEntity()
            {
                Year = o.OperateDate.Year.ToString(),
                Date = o.OperateDate.ToString("yyyy-MM-dd"),
                OperateDate = o.OperateDate,
                Profit = o.Profit,
                Cost = o.Cost,
                ProfitRatio = o.ProfitRatio
            })
            .Aggregate((result, item) =>
            {
                result.Profit += item.Profit;
                result.Cost += item.Cost;
                result.ProfitRatio = result.Profit / result.Cost;
                return result;
            });

            if (dailySummary == null) return;

            MongoFundDailySummaryEntity dailySummaryInStore = mongoDbClient
                .GetObjects<MongoFundDailySummaryEntity>(o => o.Date == dailySummary.Date)
                .SingleOrDefault();

            dailySummary.UniqueObjectId = dailySummaryInStore?.UniqueObjectId ?? ObjectId.GenerateNewId();
            mongoDbClient.UpsertObject(dailySummary);
        }

        public static void BuyFund(this MongoDbClient mongoDbClient, string fundId, decimal value)
        {
            MongoFundInfoEntity fundInfo = mongoDbClient.GetObjects<MongoFundInfoEntity>(fi => fi.Id == fundId).SingleOrDefault();
            ArgumentValidator.ThrowIfNull($"FundInfo {fundId}", fundInfo);

            MongoFundOperationEntity fundOperation = mongoDbClient
                .GetObjects<MongoFundOperationEntity>(fo => fo.Id == fundId && fo.OperateDate >= DateTime.Now.Date.AddDays(-30))
                .OrderByDescending(fo => fo.OperateDate)
                .FirstOrDefault();

            if (fundOperation != null && fundInfo.IsActive())
            {
                fundOperation.IsPublished = false;
                fundOperation.ActualPurchaseAsset += value;
                mongoDbClient.UpdateObject(fundOperation);
            }
            else
            {
                fundOperation = new MongoFundOperationEntity()
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Id = fundId,
                    Date = DateTime.Now.Date.ToString("yyyy-MM-dd"),
                    OperateDate = DateTime.Now.Date,
                    ExpectAsset = value,
                    ActualPurchaseAsset = value,
                    Shares = null,
                    Cost = 0,
                    IsPublished = false
                };

                mongoDbClient.NewObject(fundOperation);
                fundInfo.EndTime = null;
                mongoDbClient.UpdateObject(fundInfo);
            }
        }

        public static void SellFund(this MongoDbClient mongoDbClient, string fundId, decimal shares, bool doesSellAll = false)
        {
            MongoFundInfoEntity fundInfo = mongoDbClient
                .GetObjects<MongoFundInfoEntity>(f => f.Id == fundId)
                .SingleOrDefault();

            MongoFundOperationEntity fundOperation = mongoDbClient
                .GetObjects<MongoFundOperationEntity>(f => f.OperateDate == DateTime.Now.Date && f.Id == fundId)
                .SingleOrDefault();

            ArgumentValidator.ThrowIfNull($"[{fundId}] fund info", fundInfo);
            ArgumentValidator.ThrowIfNull($"[{fundId} - {fundInfo.Name}] fund operation", fundOperation);
            ArgumentValidator.ThrowIfNotNull($"[{fundId} - {fundInfo.Name}] fund EndTime", fundInfo.EndTime);

            if (doesSellAll)
            {
                ArgumentValidator.ThrowIfFalse($"[{fundId} - {fundInfo.Name}] today fund operation's publish status", fundOperation.IsPublished);
                fundInfo.EndTime = DateTime.Now.Date;
                mongoDbClient.UpdateObject(fundInfo);
            }
            else
            {
                ArgumentValidator.ThrowIfLessThan($"[{fundId} - {fundInfo.Name}] current shares {fundOperation.Shares.Value}", fundOperation.Shares.Value, shares);
                decimal sellRatio = shares / fundOperation.Shares.Value;

                fundOperation.Shares = fundOperation.Shares - shares;
                fundOperation.Asset = fundOperation.Shares.Value * fundOperation.NetAssetValue;
                fundOperation.Cost = fundOperation.Cost * (1 - sellRatio);
                fundOperation.ExpectAsset = fundOperation.ExpectAsset * (1 - sellRatio);
                fundOperation.Profit = fundOperation.Profit * (1 - sellRatio);
                fundOperation.ProfitRatio = fundOperation.Profit / fundOperation.Cost;
                fundOperation.IsPublished = false;

                mongoDbClient.UpdateObject(fundOperation);
            }
        }

        public static decimal? CalculateProfit(this MongoDbClient mongoDbClient, string fundId, int days)
        {
            List<MongoFundOperationEntity> fundOperations = mongoDbClient
                    .GetObjects<MongoFundOperationEntity>(f => f.Id == fundId)
                    .OrderByDescending(f => f.OperateDate)
                    .ToList();

            decimal? averageProfit = days <= fundOperations.Count
                ? fundOperations.Take(days).Select(x => x.ProfitRatio).Sum() / days
                : (decimal?)null;

            return averageProfit;
        }

        public static MongoFundOperationEntity GetMaxProfitFundOperation(this MongoDbClient mongoDbClient, string fundId)
        {
            MongoFundOperationEntity maxProfitFundOperation = mongoDbClient
                .GetObjects<MongoFundOperationEntity>(f => f.Id == fundId)
                .OrderByDescending(f => f.ProfitRatio)
                .ThenByDescending(f => f.OperateDate)
                .First();

            return maxProfitFundOperation;
        }

        [Obsolete]
        public static void SyncDataToMongoDb(this AzureTableClient tableClient, MongoDbClient mongoDbClient)
        {
            List<AzureFundInfo> fundInfos = tableClient.GetObjects<AzureFundInfo>(_ => true).ToList();
            fundInfos.AsParallel().ForAll(f =>
            {
                MongoFundInfoEntity mongoFundInfo =
                    mongoDbClient.GetObjects<MongoFundInfoEntity>(m => m.Id == f.PartitionKey && m.Name == f.RowKey).SingleOrDefault();

                MongoFundInfoEntity entity = new MongoFundInfoEntity
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Id = f.PartitionKey,
                    Name = f.RowKey,
                    StartTime = f.StartTime,
                    EndTime = f.EndTime,
                    ExpectGrowthRate = (decimal) f.ExpectGrowthRate,
                    PurchaseRate = (decimal) f.PurchaseRate,
                    TakeProfitRatio = (decimal) f.TakeProfitRatio,
                };

                entity.UniqueObjectId = mongoFundInfo?.UniqueObjectId ?? ObjectId.GenerateNewId();
                mongoDbClient.UpsertObject(entity);
            });

            List<AzureFundDailySummary> fundDailySummaries = tableClient.GetObjects<AzureFundDailySummary>(_ => true).ToList();
            fundDailySummaries.AsParallel().ForAll(f =>
            {
                MongoFundDailySummaryEntity mongoFundDailySummary =
                    mongoDbClient.GetObjects<MongoFundDailySummaryEntity>(m => m.Year == f.PartitionKey && m.Date == f.RowKey).SingleOrDefault();

                MongoFundDailySummaryEntity entity = new MongoFundDailySummaryEntity()
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Year = f.PartitionKey,
                    Date = f.RowKey,
                    OperateDate = f.OperateDate,
                    Cost = (decimal)f.Cost,
                    Profit = (decimal)f.Profit,
                    ProfitRatio = (decimal)f.ProfitRatio
                };

                entity.UniqueObjectId = mongoFundDailySummary?.UniqueObjectId ?? ObjectId.GenerateNewId();
                mongoDbClient.UpsertObject(entity);
            });

            List<AzureFundOperation> fundOperations = tableClient.GetObjects<AzureFundOperation>(_ => true).ToList();
            fundOperations.AsParallel().ForAll(f =>
            {
                MongoFundOperationEntity mongoFundOperation =
                    mongoDbClient.GetObjects<MongoFundOperationEntity>(m => m.Id == f.PartitionKey && m.Date == f.RowKey).SingleOrDefault();

                MongoFundOperationEntity entity = new MongoFundOperationEntity()
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Id = f.PartitionKey,
                    Date = f.RowKey,
                    OperateDate = f.OperateDate,
                    ExpectAsset = (decimal) f.ExpectAsset,
                    Asset = (decimal) f.Asset,
                    ExpectPurchaseAsset = (decimal) f.ExpectPurchaseAsset,
                    ActualPurchaseAsset = (decimal) f.ActualPurchaseAsset,
                    NetAssetValue = (decimal) f.NetAssetValue,
                    Shares = (decimal?) f.Shares,
                    Profit = (decimal) f.Profit,
                    Cost = (decimal) f.Cost,
                    ProfitRatio = (decimal) f.ProfitRatio,
                    IsPublished = f.IsPublished,
                    NotificationSent = f.NotificationSent,
                };

                entity.UniqueObjectId = mongoFundOperation?.UniqueObjectId ?? ObjectId.GenerateNewId();
                mongoDbClient.UpsertObject(entity);
            });
        }
    }
}
