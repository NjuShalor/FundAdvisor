using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace DataAccess
{
    public sealed class AzureBlobClient
    {
        private readonly BlobContainerClient blobContainerClient;

        private AzureBlobClient(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }

        public static AzureBlobClient Create(BlobServiceClient blobServiceClient, string containerName)
        {
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            blobContainerClient.CreateIfNotExists();
            return new AzureBlobClient(blobContainerClient);
        }

        public async Task UploadBlobAsync(string blobName, string text)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(text);
            await this.UploadBlobAsync(blobName, byteArray);
        }

        public async Task UploadBlobAsync(string blobName, byte[] byteArray)
        {
            BlobClient blobClient = this.blobContainerClient.GetBlobClient(blobName);
            MemoryStream stream = new MemoryStream(byteArray);
            await blobClient.UploadAsync(stream);
        }

        public async Task<Uri> GenerateBlobSasUriAsync(string blobName, TimeSpan? expiredTimeOffset = null, BlobSasPermissions blobSasPermissions = BlobSasPermissions.Read)
        {
            BlobClient blobClient = this.blobContainerClient.GetBlobClient(blobName);
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiredTimeOffset ?? TimeSpan.FromMinutes(10))
            };

            sasBuilder.SetPermissions(blobSasPermissions);
            Uri sasUri = blobClient.CanGenerateSasUri
                ? blobClient.GenerateSasUri(sasBuilder)
                : new Uri(string.Empty);

            return await Task.FromResult(sasUri);
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            BlobClient blobClient = this.blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
