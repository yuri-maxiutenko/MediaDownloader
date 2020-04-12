using System;
using System.Windows.Input;

namespace MediaDownloader
{
    public class RelayCommand : ICommand
    {
        #region Private Members

        // Action - a method that has a single parameter and does not return a value.
        private readonly Action<object> _execute;

        // Predicate - a method that has a single parameter and returns a bool.
        private readonly Predicate<object> _canExecute;

        #endregion

        #region Constructors

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion

        #region Implementation of the ICommand Interface.

        /// <summary>
        /// Occurs when the CanExecute has changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            // If there was no predicate specified for this command, then we allways are allowed to execute it.
            return _canExecute == null || _canExecute(parameter);
        }
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        #endregion
    }
}
