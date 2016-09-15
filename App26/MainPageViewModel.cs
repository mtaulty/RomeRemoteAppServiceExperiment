namespace App26
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Windows.ApplicationModel;
  using Windows.ApplicationModel.AppService;
  using Windows.Foundation.Collections;
  using Windows.System.RemoteSystems;
  using Windows.UI.Popups;
  using Windows.UI.Xaml.Media.Imaging;

  public class MainPageViewModel : ViewModelBase, IDisplayAndProcessImages
  {
    public MainPageViewModel()
    {
      this.imageSource = new SoftwareBitmapSource();
      this.syncContext = SynchronizationContext.Current;
      this.isRemoteProcessing = false;
    }
    public List<PhotoEntryViewModel> UnProcessedPhotos
    {
      get { return (this.unprocessedPhotos); }
      set { base.SetProperty(ref this.unprocessedPhotos, value); }
    }
    public List<PhotoEntryViewModel> ProcessedPhotos
    {
      get { return (this.processedPhotos); }
      set { base.SetProperty(ref this.processedPhotos, value); }
    }
    public bool IsImageVisible
    {
      get { return (this.isImageVisible); }
      set { base.SetProperty(ref this.isImageVisible, value); }
    }
    public SoftwareBitmapSource ImageSource
    {
      get { return (this.imageSource); }
      set { base.SetProperty(ref this.imageSource, value); }
    }
    public object SelectedRemoteSystem
    {
      get { return (this.selectedRemoteSystem); }
      set { base.SetProperty<RemoteSystem>(ref this.selectedRemoteSystem, (RemoteSystem)value); }
    }
    public bool? IsRemoteProcessing
    {
      get { return (this.isRemoteProcessing); }
      set { base.SetProperty(ref this.isRemoteProcessing, value); }
    }
    public bool IsBusy
    {
      get { return (this.isBusy); }
      set { base.SetProperty(ref this.isBusy, value); }
    }
    public ObservableCollection<RemoteSystem> RemoteSystems
    {
      get { return (this.remoteSystems); }
      set { base.SetProperty(ref this.remoteSystems, value); }
    }
    public async Task InitialiseAsync()
    {
      this.storageManager = new AzurePhotoStorageManager(
        Constants.AZURE_STORAGE_ACCOUNT_NAME,
        Constants.AZURE_STORAGE_KEY);

      this.redactor = new PhotoFaceRedactor();

      await this.RefreshDataAsync();

      var result = await RemoteSystem.RequestAccessAsync();

      if (result == RemoteSystemAccessStatus.Allowed)
      {
        this.RemoteSystems = new ObservableCollection<RemoteSystem>();
        this.remoteWatcher = RemoteSystem.CreateWatcher();
        this.remoteWatcher.RemoteSystemAdded += OnRemoteSystemAdded;
        this.remoteWatcher.Start();
      }
    }
    public void OnHideImage()
    {
      this.IsImageVisible = false;
    }
    public async Task DisplayImageAsync(Uri uri)
    {
      var softwareBitmap = await this.storageManager.GetSoftwareBitmapForPhotoBlobAsync(uri);
      await this.imageSource.SetBitmapAsync(softwareBitmap);
      this.IsImageVisible = true;
    }
    public async Task ProcessAsync(Uri uri)
    {
      this.IsBusy = true;

      if ((bool)this.IsRemoteProcessing && (this.SelectedRemoteSystem != null))
      {
        await RemoteRedactPhotoAsync(uri);
      }
      else
      {
        await LocalRedactPhotoAsync(uri);
      }
      // Clearly, this could be done much better with observable collections etc.
      // so I'm using a sledgehammer here to crack a nut.
      await this.RefreshDataAsync();

      this.IsBusy = false;
    }
    public static string UriToFileName(Uri uri)
    {
      return (Path.GetFileNameWithoutExtension(uri.LocalPath));
    }
    void Dispatch(Action action)
    {
      this.syncContext.Post(_ => action(), null);
    }
    void OnRemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
    {
      this.Dispatch(
        () =>
        {
          this.remoteSystems.Add(args.RemoteSystem);

          if (this.SelectedRemoteSystem == null)
          {
            this.SelectedRemoteSystem = args.RemoteSystem;
          }
        }
      );
    }
    async Task RefreshDataAsync()
    {
      var unprocessed = await this.storageManager.GetUnprocessedPhotoUrisAsync();
      var processed = await this.storageManager.GetProcessedPhotoUrisAsync();
      this.UnProcessedPhotos = unprocessed.Select(i => new PhotoEntryViewModel(i, this)).ToList();
      this.ProcessedPhotos = processed.Select(i => new PhotoEntryViewModel(i, this)).ToList();
    }
    async Task LocalRedactPhotoAsync(Uri uri)
    {
      var bitmap = await this.storageManager.GetSoftwareBitmapForPhotoBlobAsync(uri);

      var tempFile = await this.redactor.RedactFacesToTempFileAsync(bitmap);

      await this.storageManager.PutFileForProcessedPhotoBlobAsync(
        $"{UriToFileName(uri)}.jpg", tempFile);

      await this.storageManager.DeletePhotoBlobAsync(uri);
    }
    async Task RemoteRedactPhotoAsync(Uri uri)
    {
      var request = new RemoteSystemConnectionRequest(this.selectedRemoteSystem);
      using (var connection = new AppServiceConnection())
      {
        connection.AppServiceName = Constants.APP_SERVICE_NAME;

        // Strangely enough, we're trying to talk to ourselves but on another
        // machine.
        connection.PackageFamilyName = Package.Current.Id.FamilyName;
        var remoteConnection = await connection.OpenRemoteAsync(request);

        if (remoteConnection == AppServiceConnectionStatus.Success)
        {
          var valueSet = new ValueSet();
          valueSet[Constants.APP_SERVICE_URI_PARAM_NAME] = uri.ToString();
          var response = await connection.SendMessageAsync(valueSet);

          if (response.Status != AppServiceResponseStatus.Success)
          {
            // Bit naughty throwing a UI dialog from this view model
            await this.DisplayErrorAsync($"Received a response of {response.Status}");
          }
        }
        else
        {
          await this.DisplayErrorAsync($"Received a status of {remoteConnection}");
        }
      }
    }
    async Task DisplayErrorAsync(string error)
    {
      var dialog = new MessageDialog(error);
      await dialog.ShowAsync();
    }
    bool? isRemoteProcessing;
    RemoteSystem selectedRemoteSystem;
    bool isBusy;
    SoftwareBitmapSource imageSource;
    bool isImageVisible;
    AzurePhotoStorageManager storageManager;
    PhotoFaceRedactor redactor;
    List<PhotoEntryViewModel> processedPhotos;
    List<PhotoEntryViewModel> unprocessedPhotos;
    ObservableCollection<RemoteSystem> remoteSystems;
    RemoteSystemWatcher remoteWatcher;
    SynchronizationContext syncContext;
  }
}