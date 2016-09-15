namespace App26
{
  using System;
  using System.Threading.Tasks;

  static class RedactionController
  {
    public static async Task RedactPhotoAsync(Uri photoBlobUri, string newName)
    {
      var storageManager = new AzurePhotoStorageManager(
        Constants.AZURE_STORAGE_ACCOUNT_NAME,
        Constants.AZURE_STORAGE_KEY);

      var photoRedactor = new PhotoFaceRedactor();

      using (var bitmap = await storageManager.GetSoftwareBitmapForPhotoBlobAsync(photoBlobUri))
      {
        var tempFile = await photoRedactor.RedactFacesToTempFileAsync(bitmap);

        await storageManager.PutFileForProcessedPhotoBlobAsync(newName, tempFile);

        await storageManager.DeletePhotoBlobAsync(photoBlobUri);
      }
    }
  }
}
