using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;

namespace WebMapTester
{
	public class SignInControl : Control, INotifyPropertyChanged
	{
		static SignInControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SignInControl), new FrameworkPropertyMetadata(typeof(SignInControl)));
		}

		public SignInControl()
		{
			SignInCommand = new SignInCommand(this);
		}

		public string PortalUrl
		{
			get { return (string)GetValue(PortalUrlProperty); }
			set { SetValue(PortalUrlProperty, value); }
		}

		public static readonly DependencyProperty PortalUrlProperty =
			DependencyProperty.Register("PortalUrl", typeof(string), typeof(SignInControl), new PropertyMetadata(OnPortalUrlChanged));

		static void OnPortalUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SignInControl)d).OnPortalUrlChanged();
		}

		private void OnPortalUrlChanged()
		{
#pragma warning disable 4014 // not awaited task
			UpdatePortal(false); // for the case, it's not initialized
#pragma warning restore 4014
		}


		public ArcGISPortal ArcGISPortal
		{
			get { return (ArcGISPortal)GetValue(ArcGISPortalProperty); }
			internal set { SetValue(ArcGISPortalProperty, value); }
		}

		public static readonly DependencyProperty ArcGISPortalProperty =
			DependencyProperty.Register("ArcGISPortal", typeof(ArcGISPortal), typeof(SignInControl), new PropertyMetadata(null, OnArcGISPortalChanged));

		static void OnArcGISPortalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SignInControl)d).OnPropertyChanged("IsSignedIn");
		}

		public bool IsSignedIn
		{
			get
			{
				return ArcGISPortal != null && ArcGISPortal.CurrentUser != null;
			}
		}


		public ICommand SignInCommand { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		internal async Task UpdatePortal(bool withChallenge)
		{
			var im = IdentityManager.Current;
			var challengeMethod = im.ChallengeMethod;
			if (!withChallenge)
			{
				// Deactivate the challenge method temporarly before creating the portal (else challengemethod would be called for portal secured by native)
				//im.ChallengeMethod = null;
				im.ChallengeMethod = info => { throw new Exception(); }; // note: avoid painful asserts for now
			}
			ArcGISPortal arcgisPortal;
			try
			{
				arcgisPortal = await ArcGISPortal.CreateAsync(string.IsNullOrEmpty(PortalUrl) ? null : new Uri(PortalUrl));
			}
			catch
			{
				arcgisPortal = null;
			}
			if (!withChallenge)
			{
				// Restore IM
				im.ChallengeMethod = challengeMethod;
			}
			ArcGISPortal = arcgisPortal;
		}
	}

	internal class SignInCommand : ICommand
	{
		private readonly SignInControl _signInControl;
		private bool _isBusy;

		public SignInCommand(SignInControl signInControl)
		{
			_signInControl = signInControl;
		}

		public bool CanExecute(object parameter)
		{
			return !IsBusy;
		}

		public event EventHandler CanExecuteChanged;
		internal void OnCanExecuteChanged()
		{
			if (CanExecuteChanged != null)
				CanExecuteChanged(this, EventArgs.Empty);
		}

		public async void Execute(object parameter)
		{
			IsBusy = true;
			var im = IdentityManager.Current;
			if (IsSignedIn())
			{
				// Sign Out

				// Remove all credentials (even those for external services, hosted services, federated services)
				var toDelete = im.Credentials.ToArray();
				foreach (var crd in toDelete)
					im.RemoveCredential(crd);

				await _signInControl.UpdatePortal(false);
			}
			else
			{
				// Sign In
				Credential crd = null;

				if (ArcGISPortal == null || ArcGISPortal.ArcGISPortalInfo == null)
				{
					// Try first to initialize the portal without setting a token
					// After this step we might have a current user if the portal is secured with native/PKI (portal initialization will challenge or use the default credentials)
					await _signInControl.UpdatePortal(true);
				}

				if (!IsSignedIn() && ArcGISPortal != null) // Note: if the user canceled the previous native/PKI authentication, ArcGISPortal is null. In this case don't challenge again the user for a token
				{
					// OK we need a token to act as 'SignedIn'
					try
					{
						crd = await Challenge(new CredentialRequestInfo{ServiceUri = ArcGISPortal.Uri.AbsoluteUri}); 
					}
					catch { }
					if (crd != null)
					{
						im.AddCredential(crd);
						await _signInControl.UpdatePortal(true); // false should be OK as well. We are not supposed to challenge here.
						Debug.Assert(IsSignedIn());
					}
				}
			}
			IsBusy = false;
		}

		private Task<Credential> Challenge(CredentialRequestInfo info)
		{
			var im = IdentityManager.Current;

			var serverInfo = im.FindServerInfo(info.ServiceUri);

			if (serverInfo != null && serverInfo.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken)
			{
				// OAuth2 case --> call generateToken which will show up the authorization page
				return im.GenerateCredentialAsync(info.ServiceUri, info.GenerateTokenOptions).ContinueWith(t => (Credential)t.Result);
			}
			else
			{
				// Use the Toolkit SignInDialog
				return SignInDialog.DoSignIn(info);
			}
		}

		private bool IsSignedIn()
		{
			return _signInControl.IsSignedIn;
		}

		private ArcGISPortal ArcGISPortal
		{
			get { return _signInControl.ArcGISPortal; }
		}

		private bool IsBusy
		{
			get { return _isBusy; }
			set
			{
				if (_isBusy != value)
				{
					_isBusy = value;
					OnCanExecuteChanged();
				}
			}
		}
	}

}
