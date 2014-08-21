using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Security;

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
				new PortalInfo("https://www.arcgis.com/sharing/rest", "ArcGIS Online SSL"),
				new PortalInfo("http://www.arcgis.com/sharing/rest", "ArcGIS Online With OAuth", "arcgisoperationsdashboardwindows"),
				new PortalInfo("http://nitro.maps.arcgis.com/sharing/rest", "Nitro ArcGIS OnLine Organization"),
				new PortalInfo("http://devext.arcgis.com/sharing/rest"),
				new PortalInfo("http://dev.arcgis.com/sharing/rest"),
				new PortalInfo("https://portalpki.esri.com/gis/sharing/rest", "Portal PKI"),
				new PortalInfo("https://portaliwa.esri.com/gis/sharing/rest", "Portal IWA"),
				new PortalInfo("http://qaext.arcgis.com/sharing/rest"),
				new PortalInfo("http://nation.maps.arcgis.com/sharing/rest"),
				new PortalInfo("http://energy.mapsdevext.arcgis.com/sharing/rest", "Energy on devext With OAuth", "oakG4fjoNS74s8SH"),
				new PortalInfo("http://demoesrifrance2.maps.arcgis.com/sharing/rest"),
				new PortalInfo("http://clancy.maps.arcgis.com/sharing/rest"),
				new PortalInfo("https://serverlinux.esri.com/arcgis/sharing/rest", "LDAP Server linux"),
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
			if (!string.IsNullOrEmpty(portalInfo.OAuthClientId))
			{
				// Server needs to be registered to use OAuth
				IdentityManager.Current.RegisterServer(new ServerInfo
				{
					ServerUri = portalInfo.Url,
					TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode,
					OAuthClientInfo = new OAuthClientInfo
					{
						ClientId = portalInfo.OAuthClientId,
						RedirectUri = "urn:ietf:wg:oauth:2.0:oob"
					}
				});
			}
			else
			{
				// Be sure the registered server doesn't use OAuth
				var serverInfo = IdentityManager.Current.FindServerInfo(portalInfo.Url);
				if (serverInfo != null)
				{
					serverInfo.TokenAuthenticationType = TokenAuthenticationType.ArcGISToken;
				}
			}
		}
		
		#endregion
	}
}
