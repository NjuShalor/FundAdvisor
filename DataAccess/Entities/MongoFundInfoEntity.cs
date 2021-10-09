using System;
using DotNet.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccess.Entities
{
    internal class MongoFundInfoEntity : IMongoDbEntity
    {
        [BsonId]
        public ObjectId UniqueObjectId { get; set; }

        public string Id { get; set; } 

        public string Name { get; set; } 

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
        public decimal PurchaseRate { get; set; }

        // 止盈率
        public decimal TakeProfitRatio { get; set; }

        // 期望增长率
        public decimal ExpectGrowthRate { get; set; }

        public override string ToString()
        {
            return $"基金代码({nameof(Id)}): {Id}\n基金名称({nameof(Name)}): {Name}\n申购费率({nameof(PurchaseRate)}): {PurchaseRate:P}\n止盈率({nameof(TakeProfitRatio)}): {TakeProfitRatio:P}\n日期望增长率({nameof(ExpectGrowthRate)}): {ExpectGrowthRate:P}\n开始投资时间({nameof(StartTime)}): {StartTime:yyyy-MM-dd:zzz}\n结束投资时间({nameof(EndTime)}): {EndTime.GetValueOrDefault():yyyy-MM-dd:zzz}\n";
        }

        public bool IsActive()
        {
            return this.StartTime.Date <= DateTime.Now.Date &&
                   (!this.EndTime.HasValue || DateTime.Now.Date <= this.EndTime.Value.Date);
        }
    }
}
