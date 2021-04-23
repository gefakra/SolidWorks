using System;
using System.Windows.Input;


namespace Math
{
   public class DelegateCommand :ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;


        public DelegateCommand(Action<object> executeAction)
        {
            this.execute = executeAction;            
        }
        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecuteFunc)
        {
            this.execute = executeAction;
            this.canExecute = canExecuteFunc;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
