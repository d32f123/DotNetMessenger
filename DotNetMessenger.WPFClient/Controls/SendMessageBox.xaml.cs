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
        private int _chatId;

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

        public async Task SetMessageBox(int chatId, bool isDialog)
        {
            _chatId = chatId;
            var info = await RestClient.GetChatUserInfoAsync(chatId);
            var perms = isDialog ? (RolePermissions.WritePerm | RolePermissions.AttachPerm) :
                info?.Role?.RolePermissions ?? RolePermissions.WritePerm;
            if ((perms & RolePermissions.WritePerm) != 0)
            {
                SendButton.Visibility = Visibility.Visible;
                TimeExpander.Visibility = Visibility.Visible;
                MainTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                SendButton.Visibility = Visibility.Hidden;
                TimeExpander.Visibility = Visibility.Hidden;
                MainTextBox.Visibility = Visibility.Hidden;
            }
            AttachButton.Visibility = (perms & RolePermissions.AttachPerm) != 0 ? Visibility.Visible : Visibility.Hidden;
            MessageText = string.Empty;
        }

        public SendMessageBox()
        {
            InitializeComponent();
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            var message = new Message
            {
                ChatId = _chatId,
                SenderId = RestClient.UserId,
                Text = MessageText,
                Date = DateTime.Now,
                ExpirationDate = MessageExpiration == 0 ? null : (DateTime?) DateTime.Now.AddSeconds(MessageExpiration),
                Attachments = MessageAttachments
            };
            await RestClient.SendMessageAsync(_chatId, message);
            MessagePosted?.Invoke(this, message);
        }

        public event EventHandler<Message> MessagePosted;
    }
}
