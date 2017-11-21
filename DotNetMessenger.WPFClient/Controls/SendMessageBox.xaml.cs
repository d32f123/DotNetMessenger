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
using System.Drawing;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using Image = System.Drawing.Image;

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
                typeof(SendMessageBox)
            );

        public int MessageExpiration
        {
            get => (int)GetValue(MessageExpirationProperty);
            set => SetValue(MessageExpirationProperty, value);
        }
        public static readonly DependencyProperty MessageExpirationProperty =
            DependencyProperty.Register(
                nameof(MessageExpiration), typeof(int),
                typeof(SendMessageBox));


        public Attachment MessageAttachment { get; set; }

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
                Attachments = MessageAttachment != null ? new List<Attachment> { MessageAttachment } : null
            };
            await RestClient.SendMessageAsync(_chatId, message);
            MessagePosted?.Invoke(this, message);
        }

        public event EventHandler<Message> MessagePosted;

        private void AttachButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (MessageAttachment != null)
            {
                button.Content = "Attach";
                MessageAttachment = null;
            }
            else
            {
                var filename = "";
                if (!GetImageFromDialog(ref filename)) return;
                var length = new System.IO.FileInfo(filename).Length;
                var shortFileName = new System.IO.FileInfo(filename).Name;
                // if length > 30 MB = 30 MB * 1024 kb/mb * 1024 b/kb
                if (length > 30 * 1024 * 1024)
                {
                    MessageBox.Show("This file is way too large! Please something up to 30 mb only", "File too big",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Now load the file
                var isImage = true;
                System.Drawing.Image image;
                try
                {
                    image = Image.FromFile(filename);
                }
                catch
                {
                    // not an image for sure!
                    isImage = false;
                    image = null;
                }
                if (isImage)
                {
                    MessageAttachment = new Attachment {Type = AttachmentTypes.Image, File = image.ToBytes(), FileName = shortFileName};
                }
                else
                {
                    MessageAttachment = new Attachment
                    {
                        Type = AttachmentTypes.RegularFile,
                        FileName = shortFileName,
                        File = System.IO.File.ReadAllBytes(filename)
                    };
                }
                button.Content = "Unattach";
            }
        }

        private static bool GetImageFromDialog(ref string filename)
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".*",
                Filter = "Any files (*.*)|*.*"
            };

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            if (result != true) return false;
            filename = dlg.FileName;
            return true;
        }
    }
}
