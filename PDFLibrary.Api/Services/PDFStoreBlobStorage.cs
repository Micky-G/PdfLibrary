using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PDFLibrary.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace PDFLibrary.Api.Services
{

    public class PDFStoreBlobStorage : IPDFStoreBlobStorage
    {
        const string ORDERINDEX = "OrderIndex";

        private readonly IOptions<Config> _config;

        public PDFStoreBlobStorage(IOptions<Config> config)
        {
            _config = config;

            CloudBlobContainer container = GetContainer();

            container.CreateIfNotExistsAsync();
        }

        public async Task<List<PdfFileListItem>> List()
        {
            List<PdfFileListItem> blobs = new List<PdfFileListItem>();
            CloudBlobContainer container = GetContainer();

            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);
            foreach (var item in resultSegment.Results.Cast<CloudBlockBlob>().OrderBy(b => b.Metadata[ORDERINDEX]))
            {
                blobs.Add(
                    new PdfFileListItem()
                    { Name = item.Name, Location = item.Uri.AbsoluteUri, FileSize = item.Properties.Length });
            }

            return blobs;
        }



        public async Task Add(PdfFile file)
        {

            CloudBlobContainer container = GetContainer();

            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);

            int newOrderIndex = 0;
            if (resultSegment.Results.Count() > 0)
            {
                int maxCurrent = resultSegment.Results.Max(r =>
                {
                    return int.TryParse(((CloudBlockBlob)r).Metadata[ORDERINDEX], out int maxOrderIndex) ? maxOrderIndex : 0;
                });

                newOrderIndex = maxCurrent + 1;
            }

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(file.Name);
            blockBlob.Metadata[ORDERINDEX] = newOrderIndex.ToString();

            await blockBlob.UploadFromStreamAsync(file.Content);
        }

        public async Task<PdfFile> Download(string fileName)
        {
            PdfFile result;
            MemoryStream ms = new MemoryStream();

            CloudBlobContainer container = GetContainer();

            CloudBlob file = container.GetBlobReference(fileName);

            await file.DownloadToStreamAsync(ms);
            Stream blobStream = await file.OpenReadAsync();
            result = new PdfFile()
            { Content = blobStream, ContentType = file.Properties.ContentType, Name = file.Name };

            return result;
        }

        public async Task Delete(string fileName)
        {
            CloudBlobContainer container = GetContainer();

            CloudBlob file = container.GetBlobReference(fileName);

            await file.DeleteAsync();
        }


        public async Task<bool> CheckExists(string fileName)
        {
            CloudBlobContainer container = GetContainer();

            return await container.GetBlobReference(fileName).ExistsAsync();
        }

        public async Task ReOrder(List<string> newOrder)
        {
            CloudBlobContainer container = GetContainer();

            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);
            int newIndex = 0;
            foreach (string fileName in newOrder)
            {
                CloudBlockBlob blob = resultSegment.Results.Cast<CloudBlockBlob>().First(b => b.Name == fileName);

                blob.Metadata[ORDERINDEX] = newIndex++.ToString();

                await blob.SetMetadataAsync();
            }
        }        
        
        private CloudBlobContainer GetContainer()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config.Value.StorageConnection);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(_config.Value.Container);
            return container;
        }
    }
}
