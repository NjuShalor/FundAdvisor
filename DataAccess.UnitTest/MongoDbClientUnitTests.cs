using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataAccess.UnitTest
{
    [TestClass]
    public class MongoDbClientUnitTests
    {
        private const string TestDatabaseName = "DatabaseForUnitTest";
        private MongoClient mongoClient;
        private MongoDbClient mongoDbClient;

        [TestInitialize]
        public void TestInitialize()
        {
            this.mongoClient = new MongoClient("<ConnectionString PlaceHolder>");
            this.mongoDbClient = MongoDbClient.Create(mongoClient.GetDatabase(TestDatabaseName));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.mongoClient.DropDatabase(TestDatabaseName);
        }

        [TestMethod]
        public void TestNewObject()
        {
            MongoFundInfoEntity entity = new MongoFundInfoEntity
            {
                UniqueObjectId = ObjectId.GenerateNewId(),
                Id = "110022",
                Name = "测试基金名称",
                StartTime = DateTime.UtcNow,
                ExpectGrowthRate = 1.123456789123456789123456789M,
                PurchaseRate= 1.123456789123456789123456789M,
                TakeProfitRatio = 1.123456789123456789123456789M
            };

            this.mongoDbClient.NewObject<MongoFundInfoEntity>(entity);
            MongoFundInfoEntity entityInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").Single();

            Assert.IsTrue(AreEqual(entityInStore, entity));
        }

        [TestMethod]
        public void TestUpsertObject()
        {
            MongoFundInfoEntity entity = new MongoFundInfoEntity
            {
                UniqueObjectId = ObjectId.GenerateNewId(),
                Id = "110022",
                Name = "测试基金名称",
                StartTime = DateTime.UtcNow,
                ExpectGrowthRate = 1.123456789123456789123456789M,
                PurchaseRate= 1.123456789123456789123456789M,
                TakeProfitRatio = 1.123456789123456789123456789M
            };

            this.mongoDbClient.UpsertObject<MongoFundInfoEntity>(entity);
            MongoFundInfoEntity entityInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").Single();

            Assert.IsTrue(AreEqual(entityInStore, entity));
        }

        [TestMethod]
        public void TestUpdateObject()
        {
            MongoFundInfoEntity entity = new MongoFundInfoEntity
            {
                UniqueObjectId = ObjectId.GenerateNewId(),
                Id = "110022",
                Name = "测试基金名称",
                StartTime = DateTime.UtcNow,
                ExpectGrowthRate = 1.123456789123456789123456789M,
                PurchaseRate = 1.123456789123456789123456789M,
                TakeProfitRatio = 1.123456789123456789123456789M
            };

            this.mongoDbClient.NewObject<MongoFundInfoEntity>(entity);
            MongoFundInfoEntity entityInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").Single();
            Assert.IsTrue(AreEqual(entityInStore, entity));

            entity.Name = "New Name";
            this.mongoDbClient.UpdateObject<MongoFundInfoEntity>(entity);

            entityInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").Single();
            Assert.IsTrue(AreEqual(entityInStore, entity));
        }

        [TestMethod]
        public void TestDeleteObject()
        {
            MongoFundInfoEntity entity = new MongoFundInfoEntity
            {
                UniqueObjectId = ObjectId.GenerateNewId(),
                Id = "110022",
                Name = "测试基金名称",
                StartTime = DateTime.UtcNow,
                ExpectGrowthRate = 1.123456789123456789123456789M,
                PurchaseRate = 1.123456789123456789123456789M,
                TakeProfitRatio = 1.123456789123456789123456789M
            };

            this.mongoDbClient.NewObject<MongoFundInfoEntity>(entity);
            MongoFundInfoEntity entityInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").Single();

            this.mongoDbClient.DeleteObject<MongoFundInfoEntity>(entity);

            IEnumerable<MongoFundInfoEntity> entitiesInStore = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "110022").ToList();
            Assert.AreEqual(entitiesInStore.Count(), 0);
        }

        [TestMethod]
        public void TestGetObjects()
        {
            Enumerable.Range(1, 5).ToList().ForEach(index =>
            {
                MongoFundInfoEntity entity = new MongoFundInfoEntity
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Id = $"{index}",
                    Name = $"测试基金名称-{index}",
                    StartTime = DateTime.UtcNow,
                    ExpectGrowthRate = 1.123456789123456789123456789M,
                    PurchaseRate = 1.123456789123456789123456789M,
                    TakeProfitRatio = 1.123456789123456789123456789M
                };

                this.mongoDbClient.NewObject<MongoFundInfoEntity>(entity);
            });

            IEnumerable<MongoFundInfoEntity> entities = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(_ => true);
            Assert.AreEqual(entities.Count(), 5);

            MongoFundInfoEntity entity = this.mongoDbClient.GetObjects<MongoFundInfoEntity>(f => f.Id == "1").Single();
            Assert.AreEqual(entity.Id, "1");
            Assert.AreEqual(entity.Name, "测试基金名称-1");
        }

        [TestMethod]
        public void TestGetObjectsWithStringFilter()
        {
            Enumerable.Range(1, 5).ToList().ForEach(index =>
            {
                MongoFundInfoEntity entity = new MongoFundInfoEntity
                {
                    UniqueObjectId = ObjectId.GenerateNewId(),
                    Id = $"{index}",
                    Name = $"测试基金名称-{index}",
                    StartTime = DateTime.UtcNow,
                    ExpectGrowthRate = 1.123456789123456789123456789M,
                    PurchaseRate = 1.123456789123456789123456789M,
                    TakeProfitRatio = 1.123456789123456789123456789M
                };

                this.mongoDbClient.NewObject<MongoFundInfoEntity>(entity);
            });

            IEnumerable<MongoFundInfoEntity> entities = this.mongoDbClient.GetObjects<MongoFundInfoEntity>("{}");
            Assert.AreEqual(entities.Count(), 5);

            MongoFundInfoEntity entity = this.mongoDbClient.GetObjects<MongoFundInfoEntity>("{Id: '1'}").Single();
            Assert.AreEqual(entity.Id, "1");
            Assert.AreEqual(entity.Name, "测试基金名称-1");
        }

        private static bool AreEqual(MongoFundInfoEntity left, MongoFundInfoEntity right)
        {
            return (left.UniqueObjectId == right.UniqueObjectId) &&
                   (left.Id == right.Id) &&
                   (left.Name == right.Name) &&
                   (left.StartTime.Date == right.StartTime.Date) &&
                   (left.EndTime?.Date == right.EndTime?.Date) &&
                   (left.ExpectGrowthRate == right.ExpectGrowthRate) &&
                   (left.PurchaseRate == right.PurchaseRate) &&
                   (left.TakeProfitRatio == right.TakeProfitRatio);
        }
    }
}
