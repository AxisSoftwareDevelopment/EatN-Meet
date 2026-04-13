namespace eatMeet.Views.UserInterface;

public partial class CV_ActivityIndicator : ContentView
{
	public CV_ActivityIndicator()
	{
		InitializeComponent();
	}

	public void Show()
	{
		_activityIndicator.IsRunning = true;
		IsVisible = true;
	}

	public void Hide()
	{
		IsVisible = false;
		_activityIndicator.IsRunning = false;
	}
}