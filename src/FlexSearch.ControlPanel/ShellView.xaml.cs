using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlexSearch.ControlPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ShellView : MetroWindow
    {
        public static ShellView Instance = null;
        public ShellView()
        {
            InitializeComponent();
            Instance = this;
            Console.SetOut(new ConsoleWriter(this.Log, this.Dispatcher));
            Console.WriteLine("Starting FlexSearch Control Panel");
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                this.Hide();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log.CaretIndex = Log.Text.Length;
            Log.ScrollToEnd();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// A simple command that displays the command parameter as
    /// a dialog message.
    /// </summary>
    public class ShowMessageCommand : ICommand
    {
        public void Execute(object parameter)
        {
            if (!ShellView.Instance.IsVisible)
            {
                ShellView.Instance.Show();
            }

            if (ShellView.Instance.WindowState == WindowState.Minimized)
            {
                ShellView.Instance.WindowState = WindowState.Normal;
            }

            ShellView.Instance.Activate();
            ShellView.Instance.Topmost = true;
            ShellView.Instance.Focus();

        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
