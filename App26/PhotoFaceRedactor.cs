namespace App26
{
  using Microsoft.Graphics.Canvas;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.InteropServices.WindowsRuntime;
  using System.Threading.Tasks;
  using Windows.Foundation;
  using Windows.Graphics.Imaging;
  using Windows.Media.FaceAnalysis;
  using Windows.Storage;
  using Windows.UI;

  public class PhotoFaceRedactor
  {
    public async Task<StorageFile> RedactFacesToTempFileAsync(SoftwareBitmap incomingBitmap)
    {
      StorageFile tempFile = null;

      await this.CreateFaceDetectorAsync();

      // We assume our incoming bitmap format won't be supported by the face detector. 
      // We can check at runtime but I think it's unlikely.
      IList<DetectedFace> faces = null;
      var pixelFormat = FaceDetector.GetSupportedBitmapPixelFormats().First();

      using (var faceBitmap = SoftwareBitmap.Convert(incomingBitmap, pixelFormat))
      {
        faces = await this.faceDetector.DetectFacesAsync(faceBitmap);
      }
      if (faces?.Count > 0)
      {
        // We assume that our bitmap is in decent shape to be used by CanvasBitmap
        // as it should already be BGRA8 and Premultiplied alpha.
        var device = CanvasDevice.GetSharedDevice();

        using (var target = new CanvasRenderTarget(
          CanvasDevice.GetSharedDevice(),
          incomingBitmap.PixelWidth,
          incomingBitmap.PixelHeight,
          96.0f))
        {
          using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(device, incomingBitmap))
          {
            using (var session = target.CreateDrawingSession())
            {
              session.DrawImage(canvasBitmap,
                new Rect(0, 0, incomingBitmap.PixelWidth, incomingBitmap.PixelHeight));

              foreach (var face in faces)
              {
                session.FillRectangle(
                  new Rect(
                    face.FaceBox.X,
                    face.FaceBox.Y,
                    face.FaceBox.Width,
                    face.FaceBox.Height),
                  Colors.Black);
              }
            }
          }
          var fileName = $"{Guid.NewGuid()}.jpg";

          tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
            fileName, CreationCollisionOption.GenerateUniqueName);

          using (var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
          {
            await target.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg);
          }
        }
      }
      return (tempFile);
    }
    async Task CreateFaceDetectorAsync()
    {
      if (this.faceDetector == null)
      {
        this.faceDetector = await FaceDetector.CreateAsync();
      }
    }
    FaceDetector faceDetector;
  }
}