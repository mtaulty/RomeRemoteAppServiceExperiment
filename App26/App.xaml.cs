namespace App26
{
  using System;
  using Windows.ApplicationModel;
  using Windows.ApplicationModel.Activation;
  using Windows.ApplicationModel.AppService;
  using Windows.ApplicationModel.Background;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;

  sealed partial class App : Application
  {
    public App()
    {
      this.InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
      Frame rootFrame = Window.Current.Content as Frame;

      // Do not repeat app initialization when the Window already has content,
      // just ensure that the window is active
      if (rootFrame == null)
      {
        // Create a Frame to act as the navigation context and navigate to the first page
        rootFrame = new Frame();

        // Place the frame in the current Window
        Window.Current.Content = rootFrame;
      }

      if (e.PrelaunchActivated == false)
      {
        if (rootFrame.Content == null)
        {
          rootFrame.Navigate(typeof(MainPage), e.Arguments);
        }
        // Ensure the current window is active
        Window.Current.Activate();
      }
    }
    protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
    {
      this.taskDeferral = args.TaskInstance.GetDeferral();
      args.TaskInstance.Canceled += OnBackgroundTaskCancelled;

      var details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;

      if ((details != null) && (details.Name == Constants.APP_SERVICE_NAME))
      {
        this.appServiceConnection = details.AppServiceConnection;
        this.appServiceConnection.RequestReceived += OnRequestReceived;        
      }
    }
    void OnBackgroundTaskCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
      this.appServiceConnection.Dispose();
      this.appServiceConnection = null;
      this.taskDeferral?.Complete();
    }
    async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
    {
      var deferral = args.GetDeferral();

      var incomingUri = args.Request.Message[Constants.APP_SERVICE_URI_PARAM_NAME] as string;

      var uri = new Uri(incomingUri);

      // TODO: Move this function off the viewmodel into some utiliy class.
      await RedactionController.RedactPhotoAsync(uri, MainPageViewModel.UriToFileName(uri));

      deferral.Complete();
    }
    AppServiceConnection appServiceConnection;
    BackgroundTaskDeferral taskDeferral;
  }
}
