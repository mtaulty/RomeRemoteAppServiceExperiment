namespace App26
{
  using Microsoft.WindowsAzure.Storage;
  using Microsoft.WindowsAzure.Storage.Auth;
  using Microsoft.WindowsAzure.Storage.Blob;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.Graphics.Imaging;
  using Windows.Storage;

  public class AzurePhotoStorageManager
  {
    public AzurePhotoStorageManager(
      string azureStorageAccountName,
      string azureStorageAccountKey,
      string unprocessedContainerName = "unprocessed",
      string processedContainerName = "processed")
    {
      this.azureStorageAccountName = azureStorageAccountName;
      this.azureStorageAccountKey = azureStorageAccountKey;
      this.unprocessedContainerName = unprocessedContainerName;
      this.processedContainerName = processedContainerName;
      this.InitialiseBlobClient();
    }
    void InitialiseBlobClient()
    {
      if (this.blobClient == null)
      {
        this.storageAccount = new CloudStorageAccount(
          new StorageCredentials(this.azureStorageAccountName, this.azureStorageAccountKey),
          true);

        this.blobClient = this.storageAccount.CreateCloudBlobClient();
      }
    }
    public async Task<IEnumerable<Uri>> GetProcessedPhotoUrisAsync()
    {
      var entries = await this.GetPhotoUrisAsync(this.processedContainerName);
      return (entries);
    }
    public async Task<IEnumerable<Uri>> GetUnprocessedPhotoUrisAsync()
    {
      var entries = await this.GetPhotoUrisAsync(this.unprocessedContainerName);
      return (entries);
    }
    public async Task<SoftwareBitmap> GetSoftwareBitmapForPhotoBlobAsync(Uri storageUri)
    {
      // This may not quite be the most efficient function ever known to man :-)
      var reference = await this.blobClient.GetBlobReferenceFromServerAsync(storageUri);
      await reference.FetchAttributesAsync();

      SoftwareBitmap bitmap = null;

      using (var memoryStream = new MemoryStream())
      {
        await reference.DownloadToStreamAsync(memoryStream);

        var decoder = await BitmapDecoder.CreateAsync(
          BitmapDecoder.JpegDecoderId,
          memoryStream.AsRandomAccessStream());

        // Going for BGRA8 and premultiplied here saves me a lot of pain later on
        // when using SoftwareBitmapSource or using CanvasBitmap from Win2D.
        bitmap = await decoder.GetSoftwareBitmapAsync(
          BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
      }
      return (bitmap);
    }
    public async Task PutFileForProcessedPhotoBlobAsync(
      string photoName,
      StorageFile file)
    {
      var container = this.blobClient.GetContainerReference(this.processedContainerName);

      var reference = container.GetBlockBlobReference(photoName);
      
      await reference.UploadFromFileAsync(file);
    }
    public async Task<bool> DeletePhotoBlobAsync(Uri storageUri)
    {
      var container = await this.blobClient.GetBlobReferenceFromServerAsync(storageUri);
      var result = await container.DeleteIfExistsAsync();
      return (result);
    }
    async Task<IEnumerable<Uri>> GetPhotoUrisAsync(string containerName)
    {
      var uris = new List<Uri>();
      var container = this.blobClient.GetContainerReference(containerName);

      BlobContinuationToken continuationToken = null;

      do
      {
        var results = await container.ListBlobsSegmentedAsync(continuationToken);

        if (results.Results?.Count() > 0)
        {
          uris.AddRange(results.Results.Select(r => r.Uri));
        }
        continuationToken = results.ContinuationToken;

      } while (continuationToken != null);

      return (uris);
    }
    CloudStorageAccount storageAccount;
    CloudBlobClient blobClient;
    string azureStorageAccountName;
    string azureStorageAccountKey;
    string unprocessedContainerName;
    string processedContainerName;
  }
}