using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DotNetMessenger.Model;
using DotNetMessenger.WPFClient.Controls;

namespace DotNetMessenger.WPFClient.Windows
{
    /// <summary>
    /// Логика взаимодействия для NewGroupWindow.xaml
    /// </summary>
    public partial class NewGroupWindow : Window
    {
        public class UserWithCheckBox
        {
            public bool IsChecked { get; set; }
            public UserMetaBox UserBox { get; set; }

            public UserWithCheckBox(UserMetaBox userBox)
            {
                UserBox = userBox;
                IsChecked = false;
            }
        }
        public ObservableCollection<UserWithCheckBox> Users { get; set; } = new ObservableCollection<UserWithCheckBox>();

        public IEnumerable<User> SelectedUsers => Users.Where(x => x.IsChecked).Select(x => x.UserBox.DisplayedUser);

        public NewGroupWindow()
        {
            InitializeComponent();
        }

        public NewGroupWindow(IEnumerable<User> users)
        {
            InitializeComponent();
            foreach (var user in users)
            {
                Users.Add(new UserWithCheckBox(new UserMetaBox(user)));
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
