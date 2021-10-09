using System;
using Azure;
using Azure.Data.Tables;
using DotNet.Extensions;

namespace DataAccess.Entities
{
    [Obsolete]
    internal class AzureFundDailySummary : ITableEntity
    {
        public string PartitionKey { get; set; } // Fund Year
        public string RowKey { get; set; } // Fund Date
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

        public double Profit { get; set; }
        public double Cost { get; set; }
        public double ProfitRatio { get; set; }

        public override string ToString()
        {
            return $"年份(Year/PartitionKey): {PartitionKey}\n日期(Date/RowKey): {RowKey}\n日期({nameof(OperateDate)}): {OperateDate}\n收益({nameof(Profit)}): {Profit}\n成本({nameof(Cost)}): {Cost}\n收益率({nameof(ProfitRatio)}): {ProfitRatio:P}\n";
        }
    }
}
