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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DotNetMessenger.Model;

namespace DotNetMessenger.WPFClient.Controls
{
    /// <summary>
    /// Логика взаимодействия для SendMessageBox.xaml
    /// </summary>
    public partial class SendMessageBox : UserControl
    {
        private User _user;

        public string MessageText
        {
            get => (string)GetValue(MessageTextProperty);
            set => SetValue(MessageTextProperty, value);
        }
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(
                nameof(MessageText), typeof(string),
                typeof(MainWindow)
            );

        public int MessageExpiration
        {
            get => (int)GetValue(MessageExpirationProperty);
            set => SetValue(MessageExpirationProperty, value);
        }
        public static readonly DependencyProperty MessageExpirationProperty =
            DependencyProperty.Register(
                nameof(MessageExpiration), typeof(int),
                typeof(MainWindow));

        public ObservableCollection<Attachment> MessageAttachments { get; set; } = new ObservableCollection<Attachment>();

        public void SetMessageBox(User user, int chatId)
        {
            _user = user;
            var perms = user?.ChatUserInfos?[chatId]?.Role?.RolePermissions;
            if (perms == null)
            {
                SendButton.Visibility = Visibility.Collapsed;
                AttachButton.Visibility = Visibility.Collapsed;
                TimeExpander.Visibility = Visibility.Collapsed;
                return;
            }
            if ((perms & RolePermissions.WritePerm) != 0)
            {
                SendButton.Visibility = Visibility.Visible;
                TimeExpander.Visibility = Visibility.Visible;
            }
            if ((perms & RolePermissions.AttachPerm) != 0)
            {
                AttachButton.Visibility = Visibility.Visible;
            }
        }

        public SendMessageBox()
        {
            InitializeComponent();
        }
    }
}
