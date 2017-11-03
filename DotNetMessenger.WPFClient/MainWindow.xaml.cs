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
using DotNetMessenger.WPFClient.Controls;

namespace DotNetMessenger.WPFClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly ObservableCollection<UserMetaBox> UsersMetaBoxs = new ObservableCollection<UserMetaBox>();
        public readonly ObservableCollection<ChatMetaBox> ChatsMetaBoxs = new ObservableCollection<ChatMetaBox>();
        public readonly ObservableCollection<MetaBox> HistoryMetaBoxs = new ObservableCollection<MetaBox>();
        public readonly ObservableCollection<ChatMessageBox> ChatMessagesBoxs = new ObservableCollection<ChatMessageBox>();

        public MainWindow()
        {
            InitializeComponent();
            CurrentUserBox.DisplayedUser = new User
            {
                Username = "d32f123",
                UserInfo = new UserInfo {FirstName = "Andrey", LastName = "Nesterov"}
            };
            UsersMetaBoxs.Add(new UserMetaBox(new User
            {
                Username = "Temыч",
                UserInfo = new UserInfo { FirstName = "Темыч", LastName = "Темтемыч" }
            }));
            UsersMetaBoxs.Add(new UserMetaBox(new User
            {
                Username = "Шурьло",
                UserInfo = new UserInfo { FirstName = "Шуричелло", LastName = "Шурикво" }
            }));

            ChatsMetaBoxs.Add(new ChatMetaBox(new Chat
            {
                Info = new ChatInfo { Title = "hey"}
            }));

            ChatMessagesBoxs.Add(new ChatMessageBox (new Message
            {
                Text = "Hey there!"
            }));

            UsersListView.ItemsSource = UsersMetaBoxs;
            ChatsListView.ItemsSource = ChatsMetaBoxs;
            HistoryListView.ItemsSource = HistoryMetaBoxs;
            MessagesListView.ItemsSource = ChatMessagesBoxs;
        }

        private void SetCurrentChatBox(MetaBox newBox)
        {
            CurrentChatBox.MetaTitle = newBox.MetaTitle;
            CurrentChatBox.MetaImage = newBox.MetaImage;
            CurrentChatBox.MetaSecondaryInfo = newBox.MetaSecondaryInfo;
        }

        private void UserPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersListView.SelectedIndex == -1)
                return;
            ChatsListView.SelectedIndex = -1;
            HistoryListView.SelectedIndex = -1;

            SetCurrentChatBox((MetaBox)e.AddedItems[0]);
            /* TODO: OPEN DIALOG */
        }

        private void ChatPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatsListView.SelectedIndex == -1)
                return;
            UsersListView.SelectedIndex = -1;
            HistoryListView.SelectedIndex = -1;

            SetCurrentChatBox((MetaBox)e.AddedItems[0]);
            /* TODO: OPEN DIALOG */
        }

        private void HistoryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListView.SelectedIndex == -1)
                return;
            UsersListView.SelectedIndex = -1;
            ChatsListView.SelectedIndex = -1;

            SetCurrentChatBox((MetaBox)e.AddedItems[0]);
            /* TODO: OPEN DIALOG */
        }
    }
}
