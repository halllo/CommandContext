using System;
using System.Windows.Input;

namespace CommandContext
{
	public class Command : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public Command(Action execute, Func<bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute();
		public void Execute(object parameter) => _execute();

		public event EventHandler CanExecuteChanged;
	}
}
