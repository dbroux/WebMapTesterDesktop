using System.Windows.Data;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;
using Esri.ArcGISRuntime.WebMap;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WebMapTester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string BingKey = "AhSZtOGb51X9fKt5KT8Cxi4CkcMIvPYei7QmT0plKbUuZLQjgCU3CUz-7eCaoR7y";

		public MainWindow()
		{
			InitializeComponent();
			_toggleEditorCommand.SetElement(webMapEditor);
			_toggleLegendCommand.SetElement(legend);
			var query = Properties.Settings.Default.LastQuery;
			webMapSelector.QueryString = query;
			DataContext = this;

			// Set IdentityManager
			IdentityManager.Current.ChallengeMethod = info =>
			{
				var im = IdentityManager.Current;

				var serverInfo = im.FindServerInfo(info.ServiceUri);

				bool needPortalToken = info.AuthenticationType == IdentityManager.AuthenticationType.Token && serverInfo != null && !string.IsNullOrEmpty(serverInfo.OwningSystemUri);
				if (needPortalToken)
				{
					// Don't change portal login on the fly. The user has to log out first.
					var crd = im.FindCredential(info.ServiceUri);
					if (crd != null) // already logged
					{
						var tcs = new TaskCompletionSource<IdentityManager.Credential>();
						// We are logged but have no access to this resource
						tcs.SetException(new Exception("You have no access to this resource."));
						return tcs.Task;
					}
				}

				// Use the Toolkit SignInDialog
				return SignInDialog.DoSignIn(info);
			};
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();
			base.OnClosing(e);
		}

		#region DP WebMapViewModel
		public WebMapViewModel WebMapViewModel
		{
			get { return (WebMapViewModel)GetValue(WebMapViewModelProperty); }
			set { SetValue(WebMapViewModelProperty, value); }
		}


		public static readonly DependencyProperty WebMapViewModelProperty =
			DependencyProperty.Register("WebMapViewModel", typeof(WebMapViewModel), typeof(MainWindow), new PropertyMetadata(null));

		#endregion

		// Bing Token used for Bing layers
		public string BingToken {get { return BingKey; }}

		public WebMapSelector WebMapSelector { get { return webMapSelector; } }


		private readonly ToggleVisibilityCommand _toggleEditorCommand = new ToggleVisibilityCommand();
		public ICommand ToggleEditorCommand
		{
			get { return _toggleEditorCommand; }
		}

		private readonly ToggleVisibilityCommand _toggleLegendCommand = new ToggleVisibilityCommand();
		public ICommand ToggleLegendCommand
		{
			get { return _toggleLegendCommand; }
		}



		/// <summary>
		/// Command toggling the visibility of an UI Element
		/// </summary>
		private class ToggleVisibilityCommand : ActiveCommand
		{
			private UIElement _element;

			public override void Execute(object parameter)
			{
				_element.Visibility = _element.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
				SetIsActive();
			}

			public void SetElement(UIElement element)
			{
				_element = element;
				SetIsActive();
			}

			private void SetIsActive()
			{
				IsActive = _element.Visibility == Visibility.Visible;
			}
		}

		private void RemoveBinding_OnClick(object sender, RoutedEventArgs e)
		{
			BindingOperations.ClearBinding(MyMapView, MapView.MinScaleProperty);
			BindingOperations.ClearBinding(MyMapView, MapView.MaxScaleProperty);
			MyMapView.MinScale = 0.0;// double.NaN;
			MyMapView.MaxScale = 0.0; // double.NaN;
		}

		private void SetBinding_OnClick(object sender, RoutedEventArgs e)
		{
			var binding = new Binding("WebMapViewModel.RecommendedMinScale");
			MyMapView.SetBinding(MapView.MinScaleProperty, binding);
			var binding2 = new Binding("WebMapViewModel.RecommendedMaxScale");
			MyMapView.SetBinding(MapView.MaxScaleProperty, binding2);
		}

	}


}
