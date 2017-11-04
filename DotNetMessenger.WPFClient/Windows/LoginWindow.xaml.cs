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
using System.Windows.Shapes;

namespace DotNetMessenger.WPFClient.Windows
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public string LoginString
        {
            get => (string)GetValue(LoginStringProperty);
            set => SetValue(LoginStringProperty, value);
        }
        public static readonly DependencyProperty LoginStringProperty =
            DependencyProperty.Register(
                nameof(LoginString), typeof(string),
                typeof(LoginWindow)
            );

        public string PasswordString => PasswordBox.Password;

        public bool RememberLogin
        {
            get => (bool) GetValue(RememberLoginProperty);
            set => SetValue(RememberLoginProperty, value);
        }
        public static readonly DependencyProperty RememberLoginProperty =
            DependencyProperty.Register(
                nameof(RememberLogin), typeof(bool),
                typeof(LoginWindow)
            );

        public bool RememberPassword
        {
            get => (bool)GetValue(RememberPasswordProperty);
            set => SetValue(RememberPasswordProperty, value);
        }
        public static readonly DependencyProperty RememberPasswordProperty =
            DependencyProperty.Register(
                nameof(RememberPassword), typeof(bool),
                typeof(LoginWindow)
            );

        public LoginWindow()
        {
            InitializeComponent();
        }

        public Guid Token { get; private set; }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var token = await RestClient.GetUserTokenAsync(LoginString, PasswordString);
            if (token.Equals(Guid.Empty))
            {
                BadPasswordLabel.Visibility = Visibility.Visible;
                return;
            }
            Token = token;
            DialogResult = true;
            this.Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
