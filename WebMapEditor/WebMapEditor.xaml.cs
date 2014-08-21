using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.WebMap;

namespace WebMapTester
{
	/// <summary>
	/// Main editor window with all layers
	/// </summary>
	public partial class WebMapEditor : UserControl
	{
		public WebMapEditor()
		{
			InitializeComponent();
			DataContextChanged += (sender, args) => webMapLayerDetail.Visibility = Visibility.Collapsed;
		}


		// Private methods for adding/removing operational layers or basemap layers
		private void RemoveWebMapLayer(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			var webMapLayer = ((FrameworkElement) sender).DataContext as WebMapLayer;
			if (webMapViewModel == null || webMapLayer == null)
				return;
			if (webMapViewModel.OperationalLayers != null && webMapViewModel.OperationalLayers.Contains(webMapLayer))
				webMapViewModel.OperationalLayers.Remove(webMapLayer);
			if (webMapViewModel.Basemap != null && webMapViewModel.Basemap.Layers != null && webMapViewModel.Basemap.Layers.Contains(webMapLayer))
				webMapViewModel.Basemap.Layers.Remove(webMapLayer);
		}

		private void ResetOperationalLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			webMapViewModel.OperationalLayers = null;
		}
		private void ClearOperationalLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null || webMapViewModel.OperationalLayers == null)
				return;
			webMapViewModel.OperationalLayers.Clear();
		}
		private void NewOperationalLayer(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			if (webMapViewModel.OperationalLayers == null)
				webMapViewModel.OperationalLayers = new ObservableCollection<WebMapLayer>();
			var webMapLayer = new WebMapLayer();
			webMapViewModel.OperationalLayers.Add(webMapLayer);
			//ShowDetails(webMapLayer);
		}

		private void ResetBasemap(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			webMapViewModel.Basemap = null;
		}
		private void NewBasemapLayer(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			if (webMapViewModel.Basemap == null)
				webMapViewModel.Basemap = new Basemap();
			if (webMapViewModel.Basemap.Layers == null)
				webMapViewModel.Basemap.Layers = new ObservableCollection<WebMapLayer>();
			var webMapLayer = new WebMapLayer { ShowLegend = false, LayerType = WebMapLayerType.OpenStreetMap};
			webMapViewModel.Basemap.Layers.Add(webMapLayer);
			//ShowDetails(webMapLayer);
		}

		private void ResetBasemapLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null || webMapViewModel.Basemap == null)
				return;
			webMapViewModel.Basemap.Layers = null;
		}

		private void ClearBasemapLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null || webMapViewModel.Basemap == null || webMapViewModel.Basemap.Layers == null)
				return;
			webMapViewModel.Basemap.Layers.Clear();
		}

		// Go down in the hierarchy of a webmaplayer object
		private void ShowDetails(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			var webMapLayer = ((FrameworkElement)sender).DataContext as WebMapLayer;
			if (webMapViewModel == null || webMapLayer == null)
				return;
			ShowDetails(webMapLayer);
		}

		private void ShowDetails(WebMapLayer webMapLayer)
		{
			webMapLayerDetail.Visibility = Visibility.Visible;
			webMapLayerDetail.WebMapObject = webMapLayer;

			var webMapViewModel = DataContext as WebMapViewModel;
			string title = webMapViewModel != null && webMapViewModel.OperationalLayers != null && webMapViewModel.OperationalLayers.Contains(webMapLayer)
				               ? "WebMap \u2011>OperationalLayers[]"
				               : "WebMap \u2011>Basemap \u2011>Layers[]";
			webMapLayerDetail.Title = title;
		}
	}
}
