using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataAccess.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DataAccess
{
    public sealed class MongoDbClient
    {
        private readonly IMongoDatabase mongoDatabaseClient;

        private MongoDbClient(IMongoDatabase database)
        {
            this.mongoDatabaseClient = database;
        }

        public static MongoDbClient Create(IMongoDatabase database)
        {
            return new MongoDbClient(database);
        }

        public void NewObject<T>(T entity) where T : class, IMongoDbEntity, new()
        {
            IMongoCollection<T> collection = this.mongoDatabaseClient.GetCollection<T>(typeof(T).Name);
            collection.InsertOne(entity);
        }

        public void UpsertObject<T>(T entity) where T : class, IMongoDbEntity, new()
        {
            T entityInStore = this.GetObjects<T>(x => x.UniqueObjectId == entity.UniqueObjectId).SingleOrDefault();
            if (entityInStore == null) this.NewObject(entity);
            else this.UpdateObject(entity);
        }

        public void UpdateObject<T>(T entity) where T : class, IMongoDbEntity, new()
        {
            IMongoCollection<T> collection = this.mongoDatabaseClient.GetCollection<T>(typeof(T).Name);
            collection.ReplaceOne(x => x.UniqueObjectId == entity.UniqueObjectId, entity);
        }

        public void DeleteObject<T>(T entity) where T : class, IMongoDbEntity, new()
        {
            IMongoCollection<T> collection = this.mongoDatabaseClient.GetCollection<T>(typeof(T).Name);
            collection.DeleteOne(x => x.UniqueObjectId == entity.UniqueObjectId);
        }

        public IEnumerable<T> GetObjects<T>(Expression<Func<T, bool>> filter) where T : class, IMongoDbEntity, new()
        {
            IMongoCollection<T> collection = this.mongoDatabaseClient.GetCollection<T>(typeof(T).Name);
            return collection.AsQueryable().Where(filter);
        }

        public IEnumerable<T> GetObjects<T>(string filter) where T : class, IMongoDbEntity, new()
        {
            IMongoCollection<T> collection = this.mongoDatabaseClient.GetCollection<T>(typeof(T).Name);
            FilterDefinition<T> mongoFilter = filter;
            IEnumerable<T> result = collection.Find(mongoFilter).ToList();
            return result;
        }
    }
}
