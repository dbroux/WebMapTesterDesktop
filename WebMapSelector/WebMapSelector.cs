using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.WebMap;

namespace WebMapTester
{
	[TemplatePart(Name = "PortalItemSelector", Type = typeof(Selector))]
	public class WebMapSelector : Control, INotifyPropertyChanged
	{
		internal Selector PortalItemSelector;
		private ICommand _newWebMapCommand;
		private ICommand _cancelCommand;
		private ICommand _selectWebMapCommand;
		private ICommand _selectBaseMapCommand;
		private ICommand _backToResultsCommand;
		private ICommand _saveAsCommand;
		private ICommand _showWebMapCommand;
		private ActiveCommands _activeCommands;


		static WebMapSelector()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WebMapSelector), new FrameworkPropertyMetadata(typeof(WebMapSelector)));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			PortalItemSelector = GetTemplateChild("PortalItemSelector") as Selector;
			if (PortalItemSelector == null)
				throw new Exception("PortalItemSelector not defined");

			SelectWebMapCommand = new SelectWebMapCommandImpl(this);
			SelectBaseMapCommand = new SelectBaseMapCommandImpl(this);
			SaveAsCommand = new SaveAsCommandImpl(this);
			NewWebMapCommand = new NewWebMapCommandImpl(this);
			BackToResultsCommand = new BackToResultsCommandImpl(this);

			_activeCommands = new ActiveCommands(new[]
				                                     {
					                                     (IActiveCommand) SelectWebMapCommand,
					                                     (IActiveCommand) SelectBaseMapCommand,
					                                     (IActiveCommand) SaveAsCommand,
					                                     (IActiveCommand) NewWebMapCommand,
					                                     (IActiveCommand) BackToResultsCommand
				                                     });
			ShowWebMapCommand = new ShowWebMapCommandImpl(this);

			CancelCommand = _activeCommands.CancelCommand;
			_activeCommands.PropertyChanged += (s, e) =>
				                                   {
					                                   if (e.PropertyName == "IsBusy") IsBusy = _activeCommands.IsBusy;
					                                   else if (e.PropertyName == "IsActive") IsActive = _activeCommands.IsActive;
				                                   };
		}

		#region DP ArcGISPortal
		public ArcGISPortal ArcGISPortal
		{
			get { return (ArcGISPortal)GetValue(ArcGISPortalProperty); }
			set { SetValue(ArcGISPortalProperty, value); }
		}

		public static readonly DependencyProperty ArcGISPortalProperty =
			DependencyProperty.Register("ArcGISPortal", typeof(ArcGISPortal), typeof(WebMapSelector), new PropertyMetadata(null, OnArcGISPortalChanged));

		static void OnArcGISPortalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WebMapSelector)d).OnArcGISPortalChanged();
		}

		private void OnArcGISPortalChanged()
		{
			if (WebMapViewModel == null || WebMapViewModel.ArcGISPortal != ArcGISPortal)
			{
				CancelCommand.Execute(null);
				PortalItemSelector.ItemsSource = null;
				_currentPortalItemId = null;
				_activeCommands.OnCanExecuteChanged();
				((ActiveCommand)ShowWebMapCommand).OnCanExecuteChanged();

				InitWebMapViewModel();
				SelectWebMapCommand.Execute(null);
			}
		} 
		#endregion

		#region DP WebMapViewModel
		public WebMapViewModel WebMapViewModel
		{
			get { return (WebMapViewModel)GetValue(WebMapViewModelProperty); }
			set { SetValue(WebMapViewModelProperty, value); }
		}

		public static readonly DependencyProperty WebMapViewModelProperty =
			DependencyProperty.Register("WebMapViewModel", typeof(WebMapViewModel), typeof(WebMapSelector), new PropertyMetadata(OnWebmapViewModelChanged));

		static void OnWebmapViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WebMapSelector)d).OnWebMapViewModelChanged(e.OldValue as WebMapViewModel, e.NewValue as WebMapViewModel);
		}

		void OnWebMapViewModelChanged(WebMapViewModel oldValue, WebMapViewModel newValue)
		{
			if (oldValue != null)
				oldValue.LoadErrorsChanged -= OnLoadErrorsChanged;
			if (newValue != null)
			{
				ArcGISPortal = newValue.ArcGISPortal; // useful when one sets the WebMapViewModel to start with (todo: test)
				newValue.LoadErrorsChanged += OnLoadErrorsChanged;
				ShowLoadErrors(newValue.LoadErrors);
			}
		}

		void OnLoadErrorsChanged(object sender, KeyValuePair<WebMapLayer, Exception> e)
		{
			ShowLoadError(e);
		}

		internal void InitWebMapViewModel()
		{
			if (ArcGISPortal != null)
			{
				WebMapViewModel = new WebMapViewModel(ArcGISPortal);
				WebMapName = "New WebMap";
			}
			else
			{
				WebMapViewModel = null;
				WebMapName = null;
			}
		}

		internal async Task InitWebMapViewModel(WebMap webMap, string webMapName, CancellationToken cancellationToken)
		{
			WebMapViewModel = await WebMapViewModel.LoadAsync(webMap, ArcGISPortal, cancellationToken, BingToken);
			WebMapName = webMapName;
		}
		#endregion

		#region DP QueryString
		public string QueryString
		{
			get { return (string)GetValue(QueryStringProperty); }
			set { SetValue(QueryStringProperty, value); }
		}

		public static readonly DependencyProperty QueryStringProperty =
			DependencyProperty.Register("QueryString", typeof(string), typeof(WebMapSelector), new PropertyMetadata(OnQueryStringChanged));

		private static void OnQueryStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WebMapSelector)d).OnQueryStringChanged();
		}

		private void OnQueryStringChanged()
		{
			var command = SelectWebMapCommand as ActiveCommand;
			if (command != null)
				command.OnCanExecuteChanged();
		} 
		#endregion

		public string BingToken
		{
			get { return (string)GetValue(BingTokenProperty); }
			set { SetValue(BingTokenProperty, value); }
		}

		public static readonly DependencyProperty BingTokenProperty =
			DependencyProperty.Register("BingToken", typeof(string), typeof(WebMapSelector), null);


		public string WebMapName
		{
			get { return (string)GetValue(WebMapNameProperty); }
			private set { SetValue(WebMapNameProperty, value); }
		}
		public static readonly DependencyProperty WebMapNameProperty =
			DependencyProperty.Register("WebMapName", typeof(string), typeof(WebMapSelector), new PropertyMetadata(null));


		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			private set { SetValue(IsBusyProperty, value); }
		}

		public static readonly DependencyProperty IsBusyProperty =
			DependencyProperty.Register("IsBusy", typeof(bool), typeof(WebMapSelector), new PropertyMetadata(false));

		private bool _isActive;
		public bool IsActive
		{
			get { return _isActive; }
			set
			{
				if (_isActive != value)
				{
					_isActive = value;
					OnPropertyChanged();
				}
			}
		}

		// Commands Properties
		public ICommand SelectWebMapCommand
		{
			get { return _selectWebMapCommand; }
			private set
			{
				if (_selectWebMapCommand != value)
				{
					_selectWebMapCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand SelectBaseMapCommand
		{
			get { return _selectBaseMapCommand; }
			private set
			{
				if (_selectBaseMapCommand != value)
				{
					_selectBaseMapCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand SaveAsCommand
		{
			get { return _saveAsCommand; }
			private set
			{
				if (_saveAsCommand != value)
				{
					_saveAsCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand ShowWebMapCommand
		{
			get { return _showWebMapCommand; }
			private set
			{
				if (_showWebMapCommand != value)
				{
					_showWebMapCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand CancelCommand
		{
			get { return _cancelCommand; }
			private set
			{
				if (_cancelCommand != value)
				{
					_cancelCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand NewWebMapCommand
		{
			get { return _newWebMapCommand; }
			private set
			{
				if (_newWebMapCommand != value)
				{
					_newWebMapCommand = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand BackToResultsCommand
		{
			get { return _backToResultsCommand; }
			private set
			{
				if (_backToResultsCommand != value)
				{
					_backToResultsCommand = value;
					OnPropertyChanged();
				}
			}
		}

		//Private methods
		private void ExpandSelector(bool expand)
		{
			PortalItemSelector.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
			VerticalContentAlignment = expand ? VerticalAlignment.Stretch : VerticalAlignment.Top;
			//if (!expand)
			//	PortalItemSelector.ItemsSource = null;
		}

		// Showing errors
		private void ShowError(Exception error)
		{
			if (!(error is TaskCanceledException))
			{
				var message = error.Message;
				if (error.InnerException != null)
					message += Environment.NewLine + error.InnerException.Message;
				ShowError(message);
			}
	}

		private void ShowError(string message)
		{
			IsBusy = false;
			MessageBox.Show(message, null, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void ShowLoadErrors(IEnumerable<KeyValuePair<WebMapLayer, Exception>> loadErrors)
		{
			if (loadErrors != null && loadErrors.Any())
			{
				var message = new StringBuilder();
				bool first = true;
				foreach (KeyValuePair<WebMapLayer, Exception> loadError in loadErrors)
				{
					if (first)
						first = false;
					else
						message.Append(Environment.NewLine);
					message.Append(GetErrorMessage(loadError));
				}
				ShowError(message.ToString());
			}
		}

		private void ShowLoadError(KeyValuePair<WebMapLayer, Exception> loadError)
		{
			ShowError(GetErrorMessage(loadError));
		}

		private static string GetErrorMessage(KeyValuePair<WebMapLayer, Exception> loadError)
		{
			var webMapLayer = loadError.Key;
			var error = loadError.Value;
			string message = "Error loading WebMapLayer ";
			if (!string.IsNullOrEmpty(webMapLayer.Title))
				message += webMapLayer.Title;
			else if (!string.IsNullOrEmpty(webMapLayer.Id))
				message += webMapLayer.Id;
			else if (!string.IsNullOrEmpty(webMapLayer.Type))
				message += webMapLayer.Type;

			message += ": " + error.Message;
			return message;
		}

		private string _currentPortalItemId;

		// INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName]string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}


		// Commands implementation
		internal class SelectWebMapCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public SelectWebMapCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
				_webMapSelector.PortalItemSelector.SelectionChanged += PortalItemSelectorOnSelectionChanged;
			}

			public override bool IsActive
			{
				protected set
				{
					if (IsActive != value)
					{
						base.IsActive = value;
						_webMapSelector.ExpandSelector(IsActive);
					}
				}
			}

			public override bool CanExecute(object parameter)
			{
				return !string.IsNullOrEmpty(_webMapSelector.QueryString) && _webMapSelector.ArcGISPortal != null;
			}

			public override void Execute(object parameter)
			{
				IsActive = true;
				if (parameter is IEnumerable<ArcGISPortalItem>)
					_webMapSelector.PortalItemSelector.ItemsSource = parameter as IEnumerable<ArcGISPortalItem>;
				else
					DoSearch();
			}

			private async void DoSearch()
			{
				var portal = _webMapSelector.ArcGISPortal;
				var queryString = _webMapSelector.QueryString;
				if (queryString == null || string.IsNullOrEmpty(queryString.Trim()) || portal == null)
				{
					Cancel();
					return;
				}
				var query = string.Format("{0} type:\"web map\" NOT \"web mapping application\"", queryString.Trim());
				//if (portal.CurrentUser != null && portal.ArcGISPortalInfo != null && !string.IsNullOrEmpty(portal.ArcGISPortalInfo.Id))
				//	queryString = string.Format("{0} and orgid: \"{1}\"", queryString, portal.ArcGISPortalInfo.Id);
				var searchParameters = new SearchParameters
				{
					QueryString = query,
					//SortField = "modified",  // "avgrating",
					//SortOrder = QuerySortOrder.Descending,
					Limit = 30
				};
				SearchResultInfo<ArcGISPortalItem> searchResult = null;
				Exception error = null;
				IsBusy = true;
				var cts = CancellationTokenSource;
				try
				{
					searchResult = await portal.SearchItemsAsync(searchParameters);
				}
				catch (Exception e)
				{
					error = e;
				}
				if (!cts.IsCancellationRequested) // else request changed meanwhile
				{
					IsBusy = false;
					if (searchResult != null && searchResult.Results != null && searchResult.Results.Any())
					{
						Properties.Settings.Default.LastQuery = queryString;
						_webMapSelector.PortalItemSelector.ItemsSource = searchResult.Results;
						((ActiveCommand) _webMapSelector.BackToResultsCommand).OnCanExecuteChanged();
					}
					else
					{
						var message = error == null ? "No webmap found" : error.Message;
						if (error != null && error.InnerException != null)
							message += Environment.NewLine + error.InnerException.Message;
						_webMapSelector.ShowError(message);
						Cancel();
					}
				}
			}


			private void PortalItemSelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var portalItem = _webMapSelector.PortalItemSelector.SelectedItem as ArcGISPortalItem;
				if (IsActive && portalItem != null)
				{
					_webMapSelector._currentPortalItemId = portalItem.Id;
					((ActiveCommand)_webMapSelector.ShowWebMapCommand).OnCanExecuteChanged();
					DisplayWebMap(portalItem);
				}
			}

			private async void DisplayWebMap(ArcGISPortalItem portalItem)
			{
				Debug.Assert(portalItem != null);
				IsBusy = true;
				CancellationTokenSource cts = CancellationTokenSource;
				try
				{
					var webMap = await WebMap.FromPortalItemAsync(portalItem);
					if (webMap != null && !cts.IsCancellationRequested)
					{
						await _webMapSelector.InitWebMapViewModel(webMap, portalItem.Title, cts.Token);
					}
				}
				catch (Exception error)
				{
					_webMapSelector.ShowError(error); // won't display canceled error
				}
				Cancel();
			}
		}

		internal class SelectBaseMapCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public SelectBaseMapCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
				_webMapSelector.PortalItemSelector.SelectionChanged += PortalItemSelectorOnSelectionChanged;
			}


			public override bool IsActive
			{
				protected set
				{
					if (IsActive != value)
					{
						base.IsActive = value;
						_webMapSelector.ExpandSelector(IsActive);
					}
				}
			}

			public override bool CanExecute(object parameter)
			{
				return _webMapSelector.ArcGISPortal != null;
			}

			public override void Execute(object parameter)
			{
				IsActive = true;
				DoSearch();
			}

			private async void DoSearch()
			{
				var portal = _webMapSelector.ArcGISPortal;
				if (portal == null)
				{
					Cancel();
					return;
				}

				var searchParameters = new SearchParameters
				{
					SortField = "avgrating",
					SortOrder = QuerySortOrder.Descending,
					Limit = 20
				};

				IsBusy = true;
				CancellationTokenSource cts = CancellationTokenSource;
				SearchResultInfo<ArcGISPortalItem> searchResult = null;
				Exception error = null;
				try
				{
					searchResult = await portal.ArcGISPortalInfo.SearchBasemapGalleryAsync(searchParameters);
				}
				catch (Exception e)
				{
					error = e;
				}
				if (!cts.IsCancellationRequested) // else request changed meanwhile
				{
					IsBusy = false;
					if (searchResult != null)
					{
						_webMapSelector.PortalItemSelector.ItemsSource = searchResult.Results;
						((ActiveCommand)_webMapSelector.BackToResultsCommand).OnCanExecuteChanged();
					}
					else
					{
						if (error != null)
						{
							_webMapSelector.ShowError(error);
						}
						Cancel();
					}
				}
			}


			private void PortalItemSelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var portalItem = _webMapSelector.PortalItemSelector.SelectedItem as ArcGISPortalItem;
				if (IsActive && portalItem != null)
					DisplayBasemap(portalItem);
			}

			private async void DisplayBasemap(ArcGISPortalItem portalItem)
			{
				Debug.Assert(portalItem != null);
				IsBusy = true;
				CancellationTokenSource cts = CancellationTokenSource;
				try
				{
					var webMap = await WebMap.FromPortalItemAsync(portalItem);
					if (webMap != null && !cts.IsCancellationRequested)
					{
						_webMapSelector.WebMapViewModel.BaseMap = webMap.BaseMap;
					}
				}
				catch (Exception error)
				{
					_webMapSelector.ShowError(error);
				}
				Cancel();
			}
		}

		internal class SaveAsCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public SaveAsCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
			}

			public override bool CanExecute(object parameter)
			{
				return _webMapSelector.ArcGISPortal != null && _webMapSelector.ArcGISPortal.CurrentUser != null
					&& _webMapSelector.WebMapViewModel != null && _webMapSelector.WebMapViewModel.WebMap != null;
			}

			public override void Execute(object parameter)
			{
				IsActive = true;
				SaveWebMap();
			}

			private async void SaveWebMap()
			{
				if (!CanExecute(null))
				{
					Cancel();
					return;
				}
				var webMapViewModel = _webMapSelector.WebMapViewModel;
				string title = string.Format("{0}'s WebMap for testing (can be deleted)", _webMapSelector.ArcGISPortal.CurrentUser.FullName);
				ArcGISPortal portal = webMapViewModel.ArcGISPortal;
				string json = webMapViewModel.WebMap.ToJson();
				Envelope extent = webMapViewModel.Map.InitialExtent;
				if (extent != null && !SpatialReference.AreEqual(SpatialReferences.Wgs84, extent.SpatialReference))
				{
					extent = GeometryEngine.Project(extent, SpatialReferences.Wgs84) as Envelope;
				}

				IsBusy = true;
				CancellationTokenSource cts = CancellationTokenSource;
				SearchResultInfo<ArcGISPortalItem> result = await portal.SearchItemsAsync(new SearchParameters { Limit = 1, QueryString = "title:\"" + title + "\" owner:" + _webMapSelector.ArcGISPortal.CurrentUser.UserName });

				if (!cts.IsCancellationRequested)
				{
					if (result.TotalCount == 1)
					{
						// Update the item
						ArcGISPortalItem portalItem = result.Results.First();
						portalItem.Extent = extent;
						try
						{
							await portalItem.UpdateAsync(json);
							//MessageBox.Show(string.Format("Item updated user {0}: {1}", portal.CurrentUser.FullName, portalItem.Title));
							if (!cts.IsCancellationRequested)
								await DisplayWebMap(portalItem);
						}
						catch (Exception error)
						{
							_webMapSelector.ShowError(error);
						}
					}
					else
					{
						// Create a new item
						var portalItem = new ArcGISPortalItem(portal, ItemType.WebMap, title) { Description = title, Extent = extent };

						try
						{
							ArcGISPortalItem item = await portal.AddItemAsync(portalItem, json);
							//MessageBox.Show(string.Format("Item created user {0}: {1}", portal.CurrentUser.FullName, portalItem.Title));
							if (!cts.IsCancellationRequested)
								await DisplayWebMap(item);
						}
						catch (Exception error)
						{
							_webMapSelector.ShowError(error);
						}
					}
				}
				Cancel();
			}

			private async Task DisplayWebMap(ArcGISPortalItem portalItem)
			{
				await portalItem.ShareAsync(null, true, null);

				var uri = _webMapSelector.WebMapViewModel.ArcGISPortal.Uri;
				var uriBuilder = new UriBuilder(uri);
				string query = "webmap=" + portalItem.Id;
				string path = Regex.Replace(uriBuilder.Path, "sharing/.*", "home/webmap/viewer.html");
				var crd = IdentityManager.Current.FindCredential(_webMapSelector.WebMapViewModel.ArcGISPortal.Uri.AbsoluteUri);
				if (crd != null)
					query += "&token=" + crd.Token;
				uriBuilder.Query = query;
				uriBuilder.Path = path;

				Process.Start(uriBuilder.ToString());
			}
		}

		internal class ShowWebMapCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public ShowWebMapCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
			}

			public override bool CanExecute(object parameter)
			{
				return _webMapSelector.ArcGISPortal != null && !string.IsNullOrEmpty(_webMapSelector._currentPortalItemId);
			}

			public override void Execute(object parameter)
			{
				IsActive = true;
				DisplayWebMap(_webMapSelector._currentPortalItemId);
			}

			private void DisplayWebMap(string portalItemId)
			{
				var uri = _webMapSelector.ArcGISPortal.Uri;
				var uriBuilder = new UriBuilder(uri);
				string query = "webmap=" + portalItemId;
				string path = Regex.Replace(uriBuilder.Path, "sharing/.*", "home/webmap/viewer.html");
				var crd = IdentityManager.Current.FindCredential(_webMapSelector.ArcGISPortal.Uri.AbsoluteUri);
				if (crd != null)
					query += "&token=" + crd.Token;
				uriBuilder.Query = query;
				uriBuilder.Path = path;

				Process.Start(uriBuilder.ToString());
			}
		}

		internal class NewWebMapCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public NewWebMapCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
			}

			public override void Execute(object parameter)
			{
				IsActive = true;
				_webMapSelector.InitWebMapViewModel();
				IsActive = false;
			}

			public override bool CanExecute(object parameter)
			{
				return _webMapSelector.ArcGISPortal != null;
			}
		}


		internal class BackToResultsCommandImpl : ActiveCommand
		{
			private readonly WebMapSelector _webMapSelector;

			public BackToResultsCommandImpl(WebMapSelector webMapSelector)
			{
				_webMapSelector = webMapSelector;
			}

			public override bool CanExecute(object parameter)
			{
				return _webMapSelector.ArcGISPortal != null && _webMapSelector.PortalItemSelector.ItemsSource is IEnumerable<ArcGISPortalItem>;
			}

			public override void Execute(object parameter)
			{
				_webMapSelector.SelectWebMapCommand.Execute(_webMapSelector.PortalItemSelector.ItemsSource);
			}
		}
	}



	/// <summary>
	/// Converter for being able to display webmap descriptions containing HTML tags
	/// </summary>
	internal class HtmlToTextConverter : MarkupExtension, IValueConverter
	{
		private const string HtmlLineBreakRegex = @"(<br */>)|(\[br */\])"; //@"<br(.)*?>";	// Regular expression to strip HTML line break tag
		private const string HtmlStripperRegex = @"<(.|\n)*?>"; // Regular expression to strip HTML tags

		private static string ToStrippedHtmlText(object input)
		{
			string retVal = string.Empty;

			if (input is string)
			{
				// Replace HTML line break tags with $LINEBREAK$:
				retVal = Regex.Replace(input as string, HtmlLineBreakRegex, "", RegexOptions.IgnoreCase);
				// Remove the rest of HTML tags:
				retVal = Regex.Replace(retVal, HtmlStripperRegex, string.Empty);
				//retVal.Replace("$LINEBREAK$", "\n");
				retVal = retVal.Trim();
			}

			return retVal;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is string)
				return ToStrippedHtmlText(value);
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
