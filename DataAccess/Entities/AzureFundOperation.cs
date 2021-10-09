using System;
using Azure;
using Azure.Data.Tables;
using DotNet.Extensions;

namespace DataAccess.Entities
{
    [Obsolete]
    internal class AzureFundOperation : ITableEntity
    {
        public string PartitionKey { get; set; } // 基金Id
        public string RowKey { get; set; } // 日期
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        private DateTime _operateDate;
        public DateTime OperateDate
        {
            get => this._operateDate.Kind == DateTimeKind.Utc
                    ? this._operateDate.ConvertToChinaTimeFromUtc()
                    : this._operateDate;
            set => this._operateDate = value;
        }

        public double ExpectAsset { get; set; } // 目标资产
        public double Asset { get; set; } // 当前资产
        public double ExpectPurchaseAsset { get; set; } // 应购资产
        public double ActualPurchaseAsset { get; set; } // 实购资产
        public double NetAssetValue { get; set; } // 单位净值
        public double? Shares { get; set; } // 当前份额
        public double Profit { get; set; } // 当前收益
        public double Cost { get; set; } // 当前成本
        public double ProfitRatio { get; set; } // 收益率
        public bool IsPublished { get; set; } // 净值已出
        public bool NotificationSent { get; set; } // 是否发出加仓通知

        public override string ToString()
        {
            return $"基金代码(Id/PartitionKey): {this.PartitionKey}\n日期(Date/RowKey): {this.RowKey}\n日期({nameof(OperateDate)}): {this.OperateDate}\n目标资产({nameof(ExpectAsset)}): {this.ExpectAsset}\n当前资产({nameof(Asset)}): {this.Asset}\n应购资产({nameof(ExpectPurchaseAsset)}): {this.ExpectPurchaseAsset}\n实购资产({nameof(ActualPurchaseAsset)}): {this.ActualPurchaseAsset}\n单位净值({nameof(NetAssetValue)}): {this.NetAssetValue}\n当前份额({nameof(Shares)}): {this.Shares}\n当前收益:({nameof(Profit)}): {this.Profit}\n当前成本({nameof(Cost)}): {this.Cost}\n收益率({nameof(ProfitRatio)}): {this.ProfitRatio:P}\n净值已出({nameof(IsPublished)}): {this.IsPublished}\n通知已发({nameof(NotificationSent)}): {this.NotificationSent}\n";
        }
    }
}
