using System;
using Azure;
using Azure.Data.Tables;
using DotNet.Extensions;

namespace DataAccess.Entities
{
    [Obsolete]
    internal class AzureFundInfo : ITableEntity
    {
        public string PartitionKey { get; set; } // Fund Id
        public string RowKey { get; set; } // Fund Name
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => this._startTime.Kind == DateTimeKind.Utc
                ? this._startTime.ConvertToChinaTimeFromUtc()
                : this._startTime;
            set => this._startTime = value; 
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => this._endTime.HasValue && this._endTime.Value.Kind == DateTimeKind.Utc
                ? this._endTime.Value.ConvertToChinaTimeFromUtc()
                : this._endTime;
            set => this._endTime = value;
        }

        // TODO: 第一档以内申购费率
        public double PurchaseRate { get; set; }

        // 止盈率
        public double TakeProfitRatio { get; set; }

        // 期望增长率
        public double ExpectGrowthRate { get; set; }

        public override string ToString()
        {
            return $"基金代码(Id/PartitionKey): {PartitionKey}\n基金名称(Name/RowKey): {RowKey}\n申购费率({nameof(PurchaseRate)}): {PurchaseRate:P}\n止盈率({nameof(TakeProfitRatio)}): {TakeProfitRatio:P}\n日期望增长率({nameof(ExpectGrowthRate)}): {ExpectGrowthRate:P}\n开始投资时间({nameof(StartTime)}): {StartTime:yyyy-MM-dd:zzz}\n结束投资时间({nameof(EndTime)}): {EndTime.GetValueOrDefault():yyyy-MM-dd:zzz}\n";
        }

        public bool IsActive()
        {
            return this.StartTime.Date <= DateTime.Now.Date &&
               (!this.EndTime.HasValue || DateTime.Now.Date <= this.EndTime.Value.Date);
        }
    }
}
