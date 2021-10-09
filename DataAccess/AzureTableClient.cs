using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Azure.Data.Tables;

namespace DataAccess
{
    public sealed class AzureTableClient
    {
        private readonly TableServiceClient tableServiceClient;

        private AzureTableClient(TableServiceClient tableServiceClient)
        {
            this.tableServiceClient = tableServiceClient;
        }

        public static AzureTableClient Create(TableServiceClient tableServiceClient)
        {
            return new AzureTableClient(tableServiceClient);
        }

        public void CreateTableIfNotExists(string tableName)
        {
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
        }

        public void NewObject<T>(T entity) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            tableClient.AddEntity(entity);
        }

        public void UpsertObject<T>(T entity) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            tableClient.UpsertEntity(entity);
        }

        public void UpdateObject<T>(T entity) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            tableClient.UpdateEntity(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public void DeleteObject<T>(T entity) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey);
        }

        public IEnumerable<T> GetObjects<T>(Expression<Func<T, bool>> filter) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            return tableClient.Query<T>(filter);
        }

        public IEnumerable<T> GetObjects<T>(string filter) where T : class, ITableEntity, new()
        {
            string tableName = typeof(T).Name;
            TableClient tableClient = this.tableServiceClient.GetTableClient(tableName);
            return tableClient.Query<T>(filter);
        }
    }
}
