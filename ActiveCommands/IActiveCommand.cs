using System.ComponentModel;
using System.Windows.Input;

namespace WebMapTester
{
	/// <summary>
	/// Active command interface
	/// </summary>
	interface IActiveCommand : ICommand, INotifyPropertyChanged
	{
		bool IsActive { get; }

		bool IsBusy { get; }

		void Cancel();
	}
}
