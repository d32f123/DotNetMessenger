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
    /// Логика взаимодействия для ChatMessageBox.xaml
    /// </summary>
    public partial class ChatMessageBox : UserControl
    {
        public byte[] SenderAvatar
        {
            get => (byte[])GetValue(SenderAvatarProperty);
            set => SetValue(SenderAvatarProperty, value);
        }
        public static readonly DependencyProperty SenderAvatarProperty =
            DependencyProperty.Register(
                nameof(SenderAvatar), typeof(byte[]),
                typeof(ChatMessageBox)
            );

        public string SenderName
        {
            get => (string)GetValue(SenderNameProperty);
            set => SetValue(SenderNameProperty, value);
        }
        public static readonly DependencyProperty SenderNameProperty =
            DependencyProperty.Register(
                nameof(SenderName), typeof(string),
                typeof(ChatMessageBox)
            );

        public string MessageText
        {
            get => (string) GetValue(MessageTextProperty);
            set => SetValue(MessageTextProperty, value);
        }
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(
                nameof(MessageText), typeof(string),
                typeof(ChatMessageBox)
            );

        public DateTime MessageDateTime
        {
            get => (DateTime) GetValue(MessageDateTimeProperty);
            set => SetValue(MessageDateTimeProperty, value);
        }
        public static readonly DependencyProperty MessageDateTimeProperty =
            DependencyProperty.Register(
                nameof(MessageDateTime), typeof(DateTime),
                typeof(ChatMessageBox)
            );

        public ObservableCollection<byte[]> MessageAttachments { get; set; } = new ObservableCollection<byte[]>();

        private async void SetInfo()
        {
            var user = await RestClient.GetUserAsync(_message.SenderId);
            SenderName = user.Username;
            SenderAvatar = user.UserInfo?.Avatar;
        }

        private Message _message;
        public Message ChatMessage
        {
            get => _message;
            set
            {
                _message = value;
                SetInfo();
                MessageText = _message.Text;
                MessageDateTime = _message.Date;
                /* TODO: SHOW REGULAR FILES (AND ALLOW DOWNLOAD?) */
                MessageAttachments.Clear();
                if (_message.Attachments == null) return;
                foreach (var images in _message.Attachments.Select(x => x.File))
                {
                    MessageAttachments.Add(images);
                }
            }
        }

        public ChatMessageBox(Message message) : this()
        {
            ChatMessage = message;
        }

        public ChatMessageBox()
        {
            InitializeComponent();
        }
    }

    public class DateTimeToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is DateTime dateTime)) return null;
            return dateTime == DateTime.MinValue
                ? string.Empty
                : dateTime.ToString(dateTime.Date.Equals(DateTime.Now.Date) ? "T" : "g");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    try
                    {
                        return DateTime.Parse(s);
                    }
                    catch
                    {
                        return null;
                    }
            }
            return null;
        }
    }
}
