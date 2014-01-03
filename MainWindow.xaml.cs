using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;
using Esri.ArcGISRuntime.WebMap;
using System.Windows;

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


		// Attached Property  for initializing initial extent and SR of a map from WebMapViewModel
		// That's a temporary workaround, new .net API is supposed to manage that internally
		public static Envelope GetMapInitialExtent(DependencyObject obj)
		{
			return (Envelope)obj.GetValue(MapInitialExtentProperty);
		}

		public static void SetMapInitialExtent(DependencyObject obj, Envelope value)
		{
			obj.SetValue(MapInitialExtentProperty, value);
		}

		public static readonly DependencyProperty MapInitialExtentProperty =
			DependencyProperty.RegisterAttached("MapInitialExtent", typeof(Envelope), typeof(MainWindow), new PropertyMetadata(OnMapInitialExtentChanged));

		private static void OnMapInitialExtentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var map = d as Map;
			var extent = e.NewValue as Envelope;
			if (map == null || extent == null)
				return;

			if (map.SpatialReference != null && !SpatialReference.AreEqual(map.SpatialReference, extent.SpatialReference, true))
			{
				// The SR changes. We need to set a null extent while no layer in map TODO
				//LayerCollection layers = map.Layers;
				//map.Layers = new LayerCollection();
				////map.Extent = null;
				//map.Layers = layers;
				//map.ZoomTo(extent);
			}
			else
			{
				if (map.SpatialReference != null)
					map.ZoomTo(extent);
				else
				{
					PropertyChangedEventHandler handler = null;
					handler = delegate(object sender, PropertyChangedEventArgs args)
						          {
							          var m = sender as Map;
							          if (m != null && args.PropertyName == "SpatialReference")
							          {
								          m.PropertyChanged -= handler;
								          m.ZoomTo(extent);
							          }
						          };
					map.PropertyChanged += handler;
				}
			}
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

	}


}
