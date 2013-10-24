using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WebMapTester
{

	/// <summary>
	/// Manage the selection of a portal
	/// </summary>
	public class PortalSelector : Control
	{

		private readonly ObservableCollection<PortalInfo> _portals = new ObservableCollection<PortalInfo>
			{
				new PortalInfo("http://www.arcgis.com/sharing/rest", "ArcGIS Online"),
				new PortalInfo("http://nitro.maps.arcgis.com/sharing/rest", "Nitro ArcGIS OnLine Organization"),
				new PortalInfo("http://devext.arcgis.com/sharing/rest"),
				new PortalInfo("http://dev.arcgis.com/sharing/rest"),
				new PortalInfo("https://portalpki.esri.com/gis/sharing/rest", "Portal PKI"),
				new PortalInfo("https://portaliwa.esri.com/gis/sharing/rest", "Portal IWA"),
				new PortalInfo("http://qaext.arcgis.com/sharing/rest"),
				new PortalInfo("http://nation.maps.arcgis.com/sharing/rest"),
				new PortalInfo("http://demoesrifrance2.maps.arcgis.com/sharing/rest"),
				new PortalInfo("http://clancy.maps.arcgis.com/sharing/rest"),
			};

		public PortalSelector()
		{
			var url = Properties.Settings.Default.LastPortalUrl;
			if (!string.IsNullOrEmpty(url))
				PortalInfo = _portals.FirstOrDefault(i => i.Url == url);
		}

		static PortalSelector()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PortalSelector), new FrameworkPropertyMetadata(typeof(PortalSelector)));
		}

		public ObservableCollection<PortalInfo> PortalInfos { get { return _portals; } }


		#region DP PortalInfo
		public PortalInfo PortalInfo
		{
			get { return (PortalInfo)GetValue(PortalInfoProperty); }
			set { SetValue(PortalInfoProperty, value); }
		}

		public static readonly DependencyProperty PortalInfoProperty =
			DependencyProperty.Register("PortalInfo", typeof(PortalInfo), typeof(PortalSelector), new PropertyMetadata(OnPortalInfoChanged));

		static void OnPortalInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var portalInfo = e.NewValue as PortalInfo;
			if (portalInfo != null)
				Properties.Settings.Default.LastPortalUrl = portalInfo.Url;
		}
		
		#endregion
	}
}
