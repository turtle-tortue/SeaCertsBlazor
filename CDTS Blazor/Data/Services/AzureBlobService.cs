﻿namespace CDNApplication.Data.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.Storage.Blob;

    public class AzureBlobService : IAzureBlobService
    {
        private readonly IAzureBlobConnectionFactory azureBlobConnectionFactory;

        public AzureBlobService(IAzureBlobConnectionFactory azureBlobConnectionFactory)
        {
            this.azureBlobConnectionFactory = azureBlobConnectionFactory;
        }

        /// <inheritdoc/>
        public async Task<CloudBlockBlob> UploadFileAsync(IFormFile file, string container = null)
        {
            // Perhaps we can fail more gracefully then just throwing an exception
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var blobContainer = await this.azureBlobConnectionFactory.GetBlobContainer().ConfigureAwait(false);

            var blobName = AzureBlobService.UniqueFileName(file.FileName);

            // Create the blob to hold the data
            var blob = blobContainer.GetBlockBlobReference(blobName);

            // Send the file to the cloud storage
            using (var stream = file.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
            }

            return blob;
        }

        /// <inheritdoc/>
        public async Task<List<CloudBlockBlob>> UploadMultipleFilesAsync(IFormFileCollection files, string container = null)
        {
            // Perhaps we can fail more gracefully then just throwing an exception
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            // Get the container
            var blobContainer = await this.azureBlobConnectionFactory.GetBlobContainer(container).ConfigureAwait(false);

            var list = new List<CloudBlockBlob>();

            for (int i = 0; i < files.Count; i++)
            {
                // Create the blob to hold the data
                var blob = blobContainer.GetBlockBlobReference(AzureBlobService.UniqueFileName(files[i].FileName));

                // Send the file to the cloud
                using (var stream = files[i].OpenReadStream())
                {
                    await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
                }

                list.Add(blob);
            }

            return list;
        }

        private static string UniqueFileName(string currentFileName)
        {
            string ext = Path.GetExtension(currentFileName);

            string nameWithNoExt = Path.GetFileNameWithoutExtension(currentFileName);

            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}{2}", nameWithNoExt, DateTime.UtcNow.Ticks, ext);
        }
    }
}
