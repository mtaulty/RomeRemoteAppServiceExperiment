namespace App26
{
  using Windows.UI.Xaml.Controls;

  public sealed partial class MainPage : Page
  {
    public MainPage()
    {
      this.InitializeComponent();

      ViewModel = new MainPageViewModel();

      this.Loaded += async (s, e) =>
      {
        await this.ViewModel.InitialiseAsync();
      };
    }
    public MainPageViewModel ViewModel
    {
      get;
      set;
    }
  }
}