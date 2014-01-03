using System.Threading.Tasks;
using Esri.ArcGISRuntime.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WebMapTester
{
	/// <summary>
	/// Image control that subclasses the standard <see cref="Image"/> control and leaverages the IdentityManager to get the Image stream.
	/// This control allows accessing portal Thumbnails secured with Http or PKI.
	/// </summary>
	/// <remarks>Both properties ImageUri and HttpMessageHandler must be set</remarks>
	public class SecuredImage : Image
	{
		/// <summary>
		/// Gets or sets the image URI.
		/// </summary>
		/// <value>
		/// The image URI.
		/// </value>
		public Uri ImageUri{
			get { return (Uri)GetValue(ImageUriProperty); }
			set { SetValue(ImageUriProperty, value); }
		}

		/// <summary>
		/// The image URI property
		/// </summary>
		public static readonly DependencyProperty ImageUriProperty =
			DependencyProperty.Register("ImageUri", typeof(Uri), typeof(SecuredImage), new PropertyMetadata(OnImageUriChanged));

		private static void OnImageUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var task = ((SecuredImage)d).UpdateImageSource();
		}


		/// <summary>
		/// Gets or sets the HTTP message handler.
		/// </summary>
		/// <value>
		/// The HTTP message handler.
		/// </value>
		public HttpMessageHandler HttpMessageHandler
		{
			get { return (HttpMessageHandler)GetValue(HttpMessageHandlerProperty); }
			set { SetValue(HttpMessageHandlerProperty, value); }
		}

		/// <summary>
		/// The HTTP message handler property
		/// </summary>
		public static readonly DependencyProperty HttpMessageHandlerProperty =
			DependencyProperty.Register("HttpMessageHandler", typeof(HttpMessageHandler), typeof(SecuredImage), new PropertyMetadata(OnHttpMessageHandlerChanged));


		private static void OnHttpMessageHandlerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var task = ((SecuredImage)d).UpdateImageSource();
		}

		private async Task UpdateImageSource()
		{
			ImageSource source;
			if (ImageUri == null)
				source = null;
			else
			{
				try
				{
					//var httpClient = ArcGISHttpClient.CreateHttpClient(ImageUri);
					var httpClient = HttpMessageHandler == null ? new ArcGISHttpClient() :new ArcGISHttpClient(HttpMessageHandler);

					Stream streamSource = await httpClient.GetStreamAsync(ImageUri);
					var bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					source = bitmapImage;
				}
				catch
				{
					source = null;
				}
			}
			Source = source;
		}
	}
}
