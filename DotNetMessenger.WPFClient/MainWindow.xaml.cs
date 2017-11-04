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
using DotNetMessenger.WPFClient.Windows;

namespace DotNetMessenger.WPFClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Business logic
        private User CurrentUser { get; set; }
        private Guid Token { get; set; }

        private void LoginUser()
        {
            while (true)
            {
                var welcomeWindow = new WelcomeWindow();
                welcomeWindow.ShowDialog();
                if (welcomeWindow.DialogResult == null || welcomeWindow.DialogResult == false)
                {
                    CurrentUser = null;
                    Token = Guid.Empty;
                }
                if (!welcomeWindow.UserRegistered)
                {
                    var registerWindow = new RegisterWindow();
                    registerWindow.ShowDialog();
                    if (registerWindow.DialogResult == null || registerWindow.DialogResult == false)
                        continue;
                }
                var loginWindow = new LoginWindow();
                loginWindow.ShowDialog();
                if (loginWindow.DialogResult == null || loginWindow.DialogResult == false)
                    continue;
                Token = loginWindow.Token;
                var userId = RestClient.GetUserIdByTokenAsync(Token).Result;
                CurrentUser = RestClient.GetUserAsync(userId, Token).Result;
                break;
            }
        }
        #endregion

        #region View
        public readonly ObservableCollection<UserMetaBox> UsersMetaBoxs = new ObservableCollection<UserMetaBox>();
        public readonly ObservableCollection<ChatMetaBox> ChatsMetaBoxs = new ObservableCollection<ChatMetaBox>();
        public readonly ObservableCollection<MetaBox> HistoryMetaBoxs = new ObservableCollection<MetaBox>();
        public readonly ObservableCollection<ChatMessageBox> ChatMessagesBoxs = new ObservableCollection<ChatMessageBox>();

        public Visibility DisplayChatRegion
        {
            get => (Visibility)GetValue(DisplayChatRegionProperty);
            set => SetValue(DisplayChatRegionProperty, value);
        }
        public static readonly DependencyProperty DisplayChatRegionProperty =
            DependencyProperty.Register(
                nameof(DisplayChatRegion), typeof(Visibility),
                typeof(MainWindow)
            );

        public MainWindow()
        {
            InitializeComponent();

            /* Login */
            LoginUser();

            /* Set current user */
            CurrentUserBox.DisplayedUser = CurrentUser;

            /* Get all users and display them */
            var users = RestClient.GetAllUsersAsync(Token).Result;
            if (users != null && users.Any())
                foreach (var user in users)
                {
                    UsersMetaBoxs.Add(new UserMetaBox(user));
                }

            /* Get user's groups and display them */
            var chats = RestClient.GetUserChats(Token, CurrentUser.Id).Result;
            if (chats != null && chats.Any())
                foreach (var chat in chats)
                    ChatsMetaBoxs.Add(new ChatMetaBox(chat));

            UsersListView.ItemsSource = UsersMetaBoxs;
            ChatsListView.ItemsSource = ChatsMetaBoxs;
            /* TODO: HISTORY.
             * REST REPO SHOULD IMPLEMENT CALL TO GET LATEST CHAT MESSAGE
             * HISTORYMETABOX SHOULD DISPLAY LATEST MESSAGE AND DATE */
            HistoryListView.ItemsSource = HistoryMetaBoxs;

            DisplayChatRegion = Visibility.Hidden;
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

            var userBox = (UserMetaBox)e.AddedItems[0];

            SetCurrentChatBox(userBox);
            DisplayChatRegion = Visibility.Visible;

            ChatMessagesBoxs.Clear();

            RestClient.GetChatMessages(Token, userBox)
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
        #endregion 
    }
}
