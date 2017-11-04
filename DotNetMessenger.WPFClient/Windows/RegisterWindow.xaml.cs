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
    /// Логика взаимодействия для RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
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

        public string PasswordString
        {
            get => (string)GetValue(PasswordStringProperty);
            set => SetValue(PasswordStringProperty, value);
        }
        public static readonly DependencyProperty PasswordStringProperty =
            DependencyProperty.Register(
                nameof(PasswordString), typeof(string),
                typeof(LoginWindow)
            );

        public bool PasswordOk => PasswordBox.Password.Equals(PasswordSecondBox.Password);

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_OnClick(object sender, RoutedEventArgs e)
        {
            var user = await RestClient.CreateUserAsync(LoginString, PasswordString);
            if (user == null)
            {
                MainTextBlock.Text = "Либо такой пользователь уже есть, либо ты ввел хреновый логин или пароль";
                return;
            }
            DialogResult = true;
            this.Close();
        }

        private void BackButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
