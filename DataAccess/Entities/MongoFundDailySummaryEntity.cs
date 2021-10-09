using System;
using DotNet.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccess.Entities
{
    internal class MongoFundDailySummaryEntity : IMongoDbEntity
    {
        [BsonId]
        public ObjectId UniqueObjectId { get; set; }
        public string Year { get; set; } // Fund Year
        public string Date { get; set; } // Fund Date

        private DateTime _operateDate;
        public DateTime OperateDate
        {
            get => this._operateDate.Kind == DateTimeKind.Utc
                ? this._operateDate.ConvertToChinaTimeFromUtc()
                : this._operateDate;
            set => this._operateDate = value;
        }

        public decimal Profit { get; set; }
        public decimal Cost { get; set; }
        public decimal ProfitRatio { get; set; }

        public override string ToString()
        {
            return $"年份({nameof(Year)}): {Year}\n日期({nameof(Date)}): {Date}\n日期({nameof(OperateDate)}): {OperateDate}\n收益({nameof(Profit)}): {Profit}\n成本({nameof(Cost)}): {Cost}\n收益率({nameof(ProfitRatio)}): {ProfitRatio:P}\n";
        }
    }
}
