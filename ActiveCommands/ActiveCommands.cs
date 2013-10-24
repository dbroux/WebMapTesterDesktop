using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WebMapTester
{ 
	/// <summary>
	/// Manage a list of commands with only one active at a time
	/// </summary>
	class ActiveCommands : INotifyPropertyChanged
	{
		private readonly IEnumerable<IActiveCommand> _commands;
		private bool _isBusy;
		private bool _isActive;

		public ActiveCommands(IEnumerable<IActiveCommand> commands)
		{
			_commands = commands;
			foreach (var activeCommand in commands)
			{
				activeCommand.PropertyChanged += ActiveCommandOnPropertyChanged;
			}
			CancelCommand = new CancelCommandImpl(this);
		}

		/// <summary>
		/// Gets the command that cancels the active command.
		/// </summary>
		/// <value>
		/// The cancel command.
		/// </value>
		public ICommand CancelCommand { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether one command of the pool is busy.
		/// </summary>
		/// <value>
		///   <c>true</c> if one command of the pool is busy; otherwise, <c>false</c>.
		/// </value>
		public bool IsBusy
		{
			get { return _isBusy; }
			internal set
			{
				if (_isBusy != value)
				{
					_isBusy = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether one command of the pool is active.
		/// </summary>
		/// <value>
		///   <c>true</c> if one command of the pool is active; otherwise, <c>false</c>.
		/// </value>
		public bool IsActive
		{
			get { return _isActive; }
			internal set
			{
				if (_isActive != value)
				{
					_isActive = value;
					OnPropertyChanged();
				}
			}
		}

		internal void OnCanExecuteChanged()
		{
			_commands.OfType<ActiveCommand>().ToList().ForEach(c => c.OnCanExecuteChanged());
		}

		private void ActiveCommandOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			IsBusy = _commands.Any(c => c.IsBusy);
			if (e.PropertyName == "IsActive")
			{
				var command = sender as IActiveCommand;
				if (command != null && command.IsActive)
					// Keep one command active only;
					_commands.Where(c => c.IsActive && c != command).ToList().ForEach(c => c.Cancel());
				IsActive = _commands.Any(c => c.IsActive);
			}

			if (e.PropertyName == "IsBusy")
			{
				IsBusy = _commands.Any(c => c.IsBusy);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var propertyChanged = PropertyChanged;
			if (propertyChanged != null)
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}


		private class CancelCommandImpl : ICommand
		{
			private readonly ActiveCommands _activeCommands;

			internal CancelCommandImpl(ActiveCommands activeCommands)
			{
				_activeCommands = activeCommands;
				_activeCommands.PropertyChanged += (s,e) => OnCanExecuteChanged();
			}
			public bool CanExecute(object parameter)
			{
				return _activeCommands._isActive;
			}

			public event EventHandler CanExecuteChanged;

			public void Execute(object parameter)
			{
				_activeCommands._commands.Where(c => c.IsActive).ToList().ForEach(c => c.Cancel());
			}

			private void OnCanExecuteChanged()
			{
				if (CanExecuteChanged != null)
					CanExecuteChanged(this, EventArgs.Empty);
			}
		}
	}
}
