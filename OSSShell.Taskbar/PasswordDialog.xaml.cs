using System.Windows;
using System.Windows.Input;

namespace OSSShell.Taskbar
{
    public partial class PasswordDialog : Window
    {
        public string Password { get; private set; } = "";
        public bool IsConfirmed { get; private set; } = false;

        public PasswordDialog()
        {
            InitializeComponent();
            PasswordTextBox.Focus();
        }

        public PasswordDialog(string message) : this()
        {
            MessageTextBlock.Text = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordTextBox.Password;
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }
    }
}