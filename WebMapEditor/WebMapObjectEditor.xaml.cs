using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace WebMapTester
{
	/// <summary>
	/// Generic editor for a WebMapObject
	/// </summary>
	public partial class WebMapObjectEditor : UserControl
	{
		public WebMapObjectEditor()
		{
			InitializeComponent();
			DataContext = this;
		}

		// DP WebMapObject being edited
		public Object WebMapObject
		{
			get { return GetValue(WebMapObjectProperty); }
			set { SetValue(WebMapObjectProperty, value); }
		}

		public static readonly DependencyProperty WebMapObjectProperty =
			DependencyProperty.Register("WebMapObject", typeof(Object), typeof(WebMapObjectEditor), new PropertyMetadata(OnWebMapObjectChanged));

		private static void OnWebMapObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WebMapObjectEditor)d).OnWebMapObjectChanged(e.NewValue);
		}

		private void OnWebMapObjectChanged(Object newValue)
		{
			PropertiesGrid.Children.Clear();
			PropertiesGrid.RowDefinitions.Clear();
			RemoveSubEditors();
			if (newValue != null)
			{
				var type = newValue.GetType();
				List<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(prop => typeof(DependencyObject).GetProperties().All(doProp => doProp.Name != prop.Name)).ToList();
				int row = -1;

				foreach (PropertyInfo prop in properties)
				{
					PropertiesGrid.RowDefinitions.Add(new RowDefinition());
					row++;

					// Label column 0
					var textBlock = new TextBlock { Text = prop.Name, Margin = new Thickness(2, 2, 5, 2), VerticalAlignment = VerticalAlignment.Center };
					textBlock.SetValue(Grid.RowProperty, row);
					PropertiesGrid.Children.Add(textBlock);

					// Propertyvalue column 1
					Type propType = prop.PropertyType;
					bool isReadOnly = prop.GetSetMethod() == null;
					FrameworkElement uiElement;
					var binding = new Binding(prop.Name) { Mode = isReadOnly ? BindingMode.OneWay : BindingMode.TwoWay };

					if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>) && propType.GetGenericArguments()[0] == typeof(bool)) // bool? type
					{
						// Bool -> checkbox
						uiElement = new CheckBox { IsThreeState = true, IsEnabled = !isReadOnly };
						uiElement.SetBinding(ToggleButton.IsCheckedProperty, binding);
					}
					else if (propType == typeof(bool)) // bool type
					{
						uiElement = new CheckBox { IsEnabled = !isReadOnly };
						uiElement.SetBinding(ToggleButton.IsCheckedProperty, binding);
					}
					else if (propType.IsEnum)
					{
						// Enum -> show value in a combobox
						uiElement = new ComboBox { ItemsSource = Enum.GetValues(propType), IsReadOnly = isReadOnly };
						uiElement.SetBinding(Selector.SelectedValueProperty, binding);
					}
					else if (propType.IsValueType || propType == typeof(string))
					{
						// Value type or string --> display value in a textbox
						uiElement = new TextBox { IsReadOnly = isReadOnly };
						uiElement.SetBinding(TextBox.TextProperty, binding);
						uiElement.SetBinding(ToolTipProperty, binding);
					}
					else if (typeof(IEnumerable).IsAssignableFrom(propType)) // Enumerable
					{
						var genericType = GetEnumerableGenericArgument(propType);
						if (!genericType.IsValueType && genericType != typeof(object))
						{
							// Control showing the collection
							uiElement = CreateCollectionControl(prop, this);
						}
						else
						{
							// Collection of int or object (ex: VisibleLayers) --> use a comma separated string
							uiElement = new TextBox { IsReadOnly = isReadOnly };
							binding.Converter = new EnumerableToStringConverter();
							uiElement.SetBinding(TextBox.TextProperty, binding);
						}
					}
					else
					{
						// Any other object
						uiElement = CreateObjectControl(prop, this);
					}

					if (uiElement != null)
					{
						uiElement.VerticalAlignment = VerticalAlignment.Center;
						uiElement.SetValue(Grid.RowProperty, row);
						uiElement.SetValue(Grid.ColumnProperty, 1);
						PropertiesGrid.Children.Add(uiElement);
					}
				}
			}
		}


		// DP Title of the editor window (with hierarchy infos about the object being edited)
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register("Title", typeof(string), typeof(WebMapObjectEditor), null);


		// Cancel the edition (Button 'Done')
		public void Cancel(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Collapsed;
			WebMapObject = null;
		}


		// Private Methods
		private FrameworkElement CreateObjectControl(PropertyInfo prop, FrameworkElement parent)
		{
			var stackPanel = new StackPanel {Orientation = Orientation.Horizontal};
			var textBlock = new TextBlock {Text = "Null"};
			var bindingIfNull = new Binding(prop.Name) {Converter = new VisibilityConverter(), ConverterParameter = "reverse"};
			textBlock.SetBinding(VisibilityProperty, bindingIfNull);
			stackPanel.Children.Add(textBlock);

			var addButton = new Button
				                {
					                HorizontalAlignment = HorizontalAlignment.Left,
					                VerticalAlignment = VerticalAlignment.Center,
					                Style = parent.TryFindResource("AddButtonStyle") as Style, 
					                ToolTip = "Set New " + prop.PropertyType.Name
				                };
			addButton.SetBinding(VisibilityProperty, bindingIfNull);
			addButton.Click += (sender, args) =>
				                {
									if (!prop.PropertyType.IsInterface)
									{
										try
										{
											object obj = Activator.CreateInstance(prop.PropertyType);
											prop.SetValue(WebMapObject, obj);
										}
										catch
										{
											// todo instantiate symbols/renderers
											Debug.WriteLine("Need to instantiate object " + prop.PropertyType.Name);
										}
									}
									else
									{
										Debug.WriteLine("Unable to instantiate interface " + prop.PropertyType.Name);
									}
				                };
			stackPanel.Children.Add(addButton);

			var showButton = new Button
				                 {
					                 HorizontalAlignment = HorizontalAlignment.Left,
					                 VerticalAlignment = VerticalAlignment.Center,
					                 Style = parent.TryFindResource("PropertyButtonStyle") as Style,
					                 ToolTip = "Show " + prop.Name
				                 };
			showButton.Click += (sender, args) =>
				                    {
					                    RemoveSubEditors();
					                    var title = Title + "\u200B \u2011>" + prop.Name;
					                    var webMapObjectEditor = new WebMapObjectEditor
						                                             {
							                                             Visibility = Visibility.Visible,
							                                             Title = title
						                                             };
					                    LayoutRoot.Children.Add(webMapObjectEditor); // add in visual tree before setting the data context (for resources availability)
					                    webMapObjectEditor.WebMapObject = prop.GetValue(WebMapObject);
				                    };
			var bindingIfNotNull = new Binding(prop.Name) { Converter = new VisibilityConverter() };
			showButton.SetBinding(VisibilityProperty, bindingIfNotNull);
			stackPanel.Children.Add(showButton);
			
			if (prop.GetSetMethod() != null) // has setter --> nullable
			{
				var clearButton = new Button
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					Style = FindResource("DeleteButtonStyle") as Style,
					ToolTip = "Clear " + prop.Name
				};
				clearButton.Click += (o, args) => prop.SetValue(WebMapObject, null);
				clearButton.SetBinding(VisibilityProperty, bindingIfNotNull);
				stackPanel.Children.Add(clearButton);
			}

			return stackPanel;
		}

		private FrameworkElement CreateCollectionControl(PropertyInfo prop, FrameworkElement parent)
		{
			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			var textBlock = new TextBlock { Text = "Null" };
			var bindingIfNull = new Binding(prop.Name) { Converter = new VisibilityConverter(), ConverterParameter = "reverse" };
			textBlock.SetBinding(VisibilityProperty, bindingIfNull);
			stackPanel.Children.Add(textBlock);

			var addButton = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = parent.TryFindResource("AddButtonStyle") as Style,
				ToolTip = "Set New Collection " + prop.Name
			};
			addButton.SetBinding(VisibilityProperty, bindingIfNull);
			addButton.Click += (sender, args) =>
			{
				Type enumerableType;
				if (prop.PropertyType.IsGenericType)
				{
					// Instantiate an ObservableCollection of the right generic type
					// Note : we don't cover all possible cases (might be a list, a dictionary, ...)
					var genericType = GetEnumerableGenericArgument(prop.PropertyType);
					Type[] typeArgs = { genericType };
					enumerableType = typeof(ObservableCollection<>).MakeGenericType(typeArgs);
				}
				else
				{
					enumerableType = prop.PropertyType; // instantiate directly the type (that implements IEnumerable)
				}
				var collection = Activator.CreateInstance(enumerableType);
				prop.SetValue(WebMapObject, collection);
			};
			stackPanel.Children.Add(addButton);

			var showButton = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = parent.TryFindResource("PropertyButtonStyle") as Style,
				ToolTip = "Show " + prop.Name
			};
			showButton.Click += (sender, args) =>
				                    {
					                    RemoveSubEditors();
					                    var webMapCollectionEditor = new WebMapCollectionEditor
						                                                 {
							                                                 WebMapObjects = prop.GetValue(WebMapObject) as IEnumerable,
							                                                 Visibility = Visibility.Visible,
							                                                 Title = Title + "\u200B \u2011>" + prop.Name
						                                                 };
					                    LayoutRoot.Children.Add(webMapCollectionEditor);
				                    };
			var bindingIfNotNull = new Binding(prop.Name) { Converter = new VisibilityConverter() };
			showButton.SetBinding(VisibilityProperty, bindingIfNotNull);
			stackPanel.Children.Add(showButton);

			var clearButton = new Button
				                  {
					                  HorizontalAlignment = HorizontalAlignment.Left,
					                  VerticalAlignment = VerticalAlignment.Center,
					                  Style = parent.TryFindResource("DeleteButtonStyle") as Style,
					                  ToolTip = "Clear " + prop.Name
				                  };
			clearButton.Click += (o, args) => prop.SetValue(WebMapObject, null);
			clearButton.SetBinding(VisibilityProperty, bindingIfNotNull);
			stackPanel.Children.Add(clearButton);

			return stackPanel;
		}

		private void RemoveSubEditors()
		{
			var toDelete = LayoutRoot.Children.OfType<UIElement>()
			                         .Where(elt => elt is WebMapCollectionEditor || elt is WebMapObjectEditor)
			                         .ToArray();
			foreach (var child in toDelete)
				LayoutRoot.Children.Remove(child);
		}


		private Type GetEnumerableGenericArgument(Type type)
		{
			// Note: type.GetGenericArguments()[0]; is less generic
			return type.GetInterfaces()
					   .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					   .Select(t => t.GetGenericArguments()[0])
					   .FirstOrDefault() ?? type.GetGenericArguments()[0]; //  type.GetGenericArguments()[0] useful for IEnumerable<object>
		}
	}


	/// <summary>
	/// Convert an enumerable to a string using comma as separator
	/// </summary>
	public class EnumerableToStringConverter : MarkupExtension, IValueConverter
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
			return !(value is IEnumerable) ? value : string.Join(",", ((IEnumerable)value).Cast<object>());
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
			if (targetType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(targetType.GetGenericTypeDefinition())) // Generic enumerable
			{
				if (value == null || string.IsNullOrEmpty(value.ToString()))
					return null;

				var genericType = targetType.GetGenericArguments()[0];
				IList list;
				if (targetType.IsInterface)
					list = Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(genericType)) as IList;
				else
					list = Activator.CreateInstance(targetType) as IList;
				if (list != null)
				{
					foreach (string v in value.ToString().Split(','))
					{
						object val = v;
						if (genericType == typeof(int) || genericType == typeof(object))
						{
							try
							{
								int i = Convert.ToInt32(v);
								val = i;
							}
							catch
							{
							}
						}
						list.Add(val);
					}
				}
				return list;
			}
			if (value != null)
				Debug.WriteLine("EnumerableToStringConverter.ConvertBack not managed" + value.GetType().Name);
			return value;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

	}


	/// <summary>
	/// Visibility Converter
	/// </summary>
	public class VisibilityConverter : MarkupExtension, IValueConverter
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
			bool vis;
			if (value is string)
				vis = !string.IsNullOrEmpty((string)value);
			else if (value is bool)
				vis = (bool)value;
			else
				vis = (value != null);

			if (parameter is string && ((string)parameter).Equals("reverse", StringComparison.OrdinalIgnoreCase))
				vis = !vis;
			return vis ? Visibility.Visible : Visibility.Collapsed;
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
			return (Visibility)value == Visibility.Visible;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

	}

}
