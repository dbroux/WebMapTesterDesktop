using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace WebMapTester
{
	/// <summary>
	/// Generic editor for a collection of WebMapObject
	/// </summary>
	public partial class WebMapCollectionEditor : UserControl
	{
		public WebMapCollectionEditor()
		{
			InitializeComponent();
			DataContext = this;
		}

		// DP Collection of WebMapObjects being edited
		public IEnumerable WebMapObjects
		{
			get { return (IEnumerable)GetValue(WebMapObjectsProperty); }
			set { SetValue(WebMapObjectsProperty, value); }
		}

		public static readonly DependencyProperty WebMapObjectsProperty =
			DependencyProperty.Register("WebMapObjects", typeof(IEnumerable), typeof(WebMapCollectionEditor), new PropertyMetadata(OnWebMapObjectsChanged));

		private static void OnWebMapObjectsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WebMapCollectionEditor)d).OnWebMapObjectsChanged(e.NewValue as IEnumerable);
		}

		private void OnWebMapObjectsChanged(IEnumerable newValue)
		{
			webMapObjectEditor.Visibility = Visibility.Collapsed;
			if (newValue != null)
			{
				Type enumType = GetEnumerableGenericArgument(newValue.GetType());
				InstanceLabel = enumType.Name;
			}
			else
			{
				InstanceLabel = null;
			}
		}

		// DP Title of the editor window (with hierarchy infos about the object being edited)
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register("Title", typeof(string), typeof(WebMapCollectionEditor), null);


		// DP Label for the WebmapObjects in the collection
		public string InstanceLabel
		{
			get { return (string)GetValue(InstanceLabelProperty); }
			set { SetValue(InstanceLabelProperty, value); }
		}

		public static readonly DependencyProperty InstanceLabelProperty =
			DependencyProperty.Register("InstanceLabel", typeof(string), typeof(WebMapCollectionEditor), null);


		// Cancel the edition (Button 'Done')
		public void Cancel(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Collapsed;
			WebMapObjects = null;
		}


		// Remove an object from the collection
		public void RemoveWebMapObject(object sender, RoutedEventArgs e)
		{
			var child = ((FrameworkElement)sender).DataContext;
			if (WebMapObjects == null || child == null)
				return;
			var type = WebMapObjects.GetType();
			var method = type.GetMethod("Remove");
			method.Invoke(WebMapObjects, new[] {child});
		}

		// Clear the collection
		public void ClearCollection(object sender, RoutedEventArgs e)
		{
			if (WebMapObjects == null)
				return;
			var type = WebMapObjects.GetType();
			var method = type.GetMethod("Clear");
			method.Invoke(WebMapObjects, null);
		}

		// Add a new object in the collection
		public void NewChild(object sender, RoutedEventArgs e)
		{
			if (WebMapObjects == null)
				return;
			var type = WebMapObjects.GetType();
			var objectType = GetEnumerableGenericArgument(type);
			var method = type.GetMethod("Add");
			try
			{
				object child = Activator.CreateInstance(objectType);
				method.Invoke(WebMapObjects, new[] { child });
			}
			catch { }

			//ShowDetails(child);
		}

		// Edit a webmapobject
		public void ShowDetails(object sender, RoutedEventArgs e)
		{
			var child = ((FrameworkElement)sender).DataContext;
			ShowDetails(child);
		}

		private void ShowDetails(object child)
		{
			if (child == null)
				return;
			webMapObjectEditor.Visibility = Visibility.Visible;
			webMapObjectEditor.WebMapObject = null;
			webMapObjectEditor.WebMapObject = child;
			webMapObjectEditor.Title = Title + "[]";
		}

		private Type GetEnumerableGenericArgument(Type type)
		{
			// Note: type.GetGenericArguments()[0]; is less generic
			return type.GetInterfaces()
					   .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					   .Select(t => t.GetGenericArguments()[0])
					   .FirstOrDefault();
		}
	}

	public class ObjectWithIdConverter : MarkupExtension, IValueConverter
	{
		/// <summary>
		/// Modifies the source data before passing it to the target for display in the UI.
		/// </summary>
		/// <param name="value">The source data being passed to the target.</param>
		/// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
		/// <param name="parameter">An optional parameter to be used in the converter logic.</param>
		/// <param name="culture">The culture of the conversion.</param>
		/// <returns>
		/// The value to be passed to the target dependency property.
		/// </returns>
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ObjectWithId(value);
		}

		/// <summary>
		/// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
		/// </summary>
		/// <param name="value">The target data being passed to the source.</param>
		/// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
		/// <param name="parameter">An optional parameter to be used in the converter logic.</param>
		/// <param name="culture">The culture of the conversion.</param>
		/// <returns>
		/// The value to be passed to the source object.
		/// </returns>
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

	}


	// Allows twoway binding with dynamic Id of an object
	internal class ObjectWithId : FrameworkElement
	{
		public ObjectWithId(object obj)
		{
			DataContext = obj;

			// Looks for the best ident property
			var type = obj.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			PropertyInfo prop = properties.FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
								?? properties.FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
								?? properties.FirstOrDefault(p => p.Name.Equals("title", StringComparison.OrdinalIgnoreCase))
								?? properties.FirstOrDefault(p => p.Name.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0)
								?? properties.FirstOrDefault(p => p.PropertyType == typeof(string));

			if (prop == null)
			{
				Debug.WriteLine("No ident found " + type.Name);
				return;
			}
			IdName = prop.Name;
			var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };
			SetBinding(IdProperty, binding);
		}

		public object Id
		{
			get { return GetValue(IdProperty); }
			set { SetValue(IdProperty, value); }
		}

		public static readonly DependencyProperty IdProperty =
			DependencyProperty.Register("Id", typeof(object), typeof(ObjectWithId), new PropertyMetadata(null));

		public string IdName { get; set; }
	}
}
