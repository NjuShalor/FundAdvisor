using System;
using DotNet.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccess.Entities
{
    internal class MongoFundOperationEntity : IMongoDbEntity
    {
        [BsonId]
        public ObjectId UniqueObjectId { get; set; }
        public string Id { get; set; } // 基金Id
        public string Date { get; set; } // 日期

        private DateTime _operateDate;
        public DateTime OperateDate
        {
            get => this._operateDate.Kind == DateTimeKind.Utc
                ? this._operateDate.ConvertToChinaTimeFromUtc()
                : this._operateDate;
            set => this._operateDate = value;
        }

        public decimal ExpectAsset { get; set; } // 目标资产
        public decimal Asset { get; set; } // 当前资产
        public decimal ExpectPurchaseAsset { get; set; } // 应购资产
        public decimal ActualPurchaseAsset { get; set; } // 实购资产
        public decimal NetAssetValue { get; set; } // 单位净值
        public decimal? Shares { get; set; } // 当前份额
        public decimal Profit { get; set; } // 当前收益
        public decimal Cost { get; set; } // 当前成本
        public decimal ProfitRatio { get; set; } // 收益率
        public bool IsPublished { get; set; } // 净值已出
        public bool NotificationSent { get; set; } // 是否发出加仓通知

        public override string ToString()
        {
            return $"基金代码({nameof(Id)}): {this.Id}\n日期({nameof(Date)}): {this.Date}\n日期({nameof(OperateDate)}): {this.OperateDate}\n目标资产({nameof(ExpectAsset)}): {this.ExpectAsset}\n当前资产({nameof(Asset)}): {this.Asset}\n应购资产({nameof(ExpectPurchaseAsset)}): {this.ExpectPurchaseAsset}\n实购资产({nameof(ActualPurchaseAsset)}): {this.ActualPurchaseAsset}\n单位净值({nameof(NetAssetValue)}): {this.NetAssetValue}\n当前份额({nameof(Shares)}): {this.Shares}\n当前收益:({nameof(Profit)}): {this.Profit}\n当前成本({nameof(Cost)}): {this.Cost}\n收益率({nameof(ProfitRatio)}): {this.ProfitRatio:P}\n净值已出({nameof(IsPublished)}): {this.IsPublished}\n通知已发({nameof(NotificationSent)}): {this.NotificationSent}\n";
        }
    }
}
