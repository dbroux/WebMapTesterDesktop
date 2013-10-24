using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WebMapTester
{
	/// <summary>
	/// Base class for commands
	/// </summary>
	internal abstract class ActiveCommand : IActiveCommand
	{
		private bool _isActive;
		private bool _isBusy;

		public virtual bool CanExecute(object parameter)
		{
			return true;
		}

		public abstract void Execute(object parameter);
		public event EventHandler CanExecuteChanged;

		/// <summary>
		/// Gets or sets a value indicating whether the command is active.
		/// </summary>
		/// <value>
		///   <c>true</c> if the command is active; otherwise, <c>false</c>.
		/// </value>
		public virtual bool IsActive
		{
			get { return _isActive; }
			protected set
			{
				if (_isActive != value)
				{
					_isActive = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the command is busy.
		/// </summary>
		/// <value>
		///   <c>true</c> if the command is busy; otherwise, <c>false</c>.
		/// </value>
		public bool IsBusy
		{
			get { return _isBusy; }
			protected set
			{
				if (_isBusy != value)
				{
					_isBusy = value;
					CancellationTokenSource = _isBusy ? new CancellationTokenSource() : null;
					OnPropertyChanged();
				}
			}
		}

		protected CancellationTokenSource CancellationTokenSource { get; private set; }

		/// <summary>
		/// Cancels the command.
		/// </summary>
		public virtual void Cancel()
		{
			if (CancellationTokenSource != null)
				CancellationTokenSource.Cancel();
			IsActive = false;
			IsBusy = false;
		}

		protected internal void OnCanExecuteChanged()
		{
			var canExecuteChanged = CanExecuteChanged;
			if (canExecuteChanged != null)
				canExecuteChanged(this, EventArgs.Empty);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var propertyChanged = PropertyChanged;
			if (propertyChanged != null)
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
