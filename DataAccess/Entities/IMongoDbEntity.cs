using MongoDB.Bson;

namespace DataAccess.Entities
{
    public interface IMongoDbEntity
    {
        public ObjectId UniqueObjectId { get; set; }
    }
}
