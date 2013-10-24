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


		// Private methods for adding/removing operationla layers or basemap layers
		private void RemoveWebMapLayer(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			var webMapLayer = ((FrameworkElement) sender).DataContext as WebMapLayer;
			if (webMapViewModel == null || webMapLayer == null)
				return;
			if (webMapViewModel.OperationalLayers != null && webMapViewModel.OperationalLayers.Contains(webMapLayer))
				webMapViewModel.OperationalLayers.Remove(webMapLayer);
			if (webMapViewModel.BaseMap != null && webMapViewModel.BaseMap.Layers != null && webMapViewModel.BaseMap.Layers.Contains(webMapLayer))
				webMapViewModel.BaseMap.Layers.Remove(webMapLayer);
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

		private void ResetBaseMap(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			webMapViewModel.BaseMap = null;
		}
		private void NewBaseMapLayer(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null)
				return;
			if (webMapViewModel.BaseMap == null)
				webMapViewModel.BaseMap = new BaseMap();
			if (webMapViewModel.BaseMap.Layers == null)
				webMapViewModel.BaseMap.Layers = new ObservableCollection<WebMapLayer>();
			var webMapLayer = new WebMapLayer { ShowLegend = false, Type = "OpenStreetMap"};
			webMapViewModel.BaseMap.Layers.Add(webMapLayer);
			//ShowDetails(webMapLayer);
		}

		private void ResetBaseMapLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null || webMapViewModel.BaseMap == null)
				return;
			webMapViewModel.BaseMap.Layers = null;
		}

		private void ClearBaseMapLayers(object sender, RoutedEventArgs e)
		{
			var webMapViewModel = DataContext as WebMapViewModel;
			if (webMapViewModel == null || webMapViewModel.BaseMap == null || webMapViewModel.BaseMap.Layers == null)
				return;
			webMapViewModel.BaseMap.Layers.Clear();
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
			string title = webMapViewModel != null && webMapViewModel.OperationalLayers.Contains(webMapLayer)
				               ? "WebMap \u2011>OperationalLayers[]"
				               : "WebMap \u2011>BaseMap \u2011>Layers[]";
			webMapLayerDetail.Title = title;
		}
	}
}
