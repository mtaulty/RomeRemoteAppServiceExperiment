using System;
namespace App26
{
  public class PhotoEntryViewModel
  {
    public PhotoEntryViewModel(Uri uri, IDisplayAndProcessImages imageDisplay)
    {
      this.Uri = uri;
      this.imageDisplay = imageDisplay;
    }
    public async void View()
    {
      await this.imageDisplay.DisplayImageAsync(this.Uri);
    }
    public async void Process()
    {
      await this.imageDisplay.ProcessAsync(this.Uri);
    }
    public Uri Uri { get; }
    IDisplayAndProcessImages imageDisplay;
  }
}
