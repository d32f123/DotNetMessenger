using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using DotNetMessenger.Model.Enums;
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
        private int _currChat = -1;

        private void LoginUser()
        {
            while (true)
            {
                var welcomeWindow = new WelcomeWindow();
                welcomeWindow.ShowDialog();
                if (welcomeWindow.DialogResult == null || welcomeWindow.DialogResult == false)
                {
                    CurrentUser = null;
                    break;
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
                var userId = RestClient.GetLoggedUserIdAsync().Result;
                CurrentUser = RestClient.GetUserAsync(userId).Result;
                break;
            }
        }

        private async Task AddUserAndSubscribe(User x)
        {
            UsersMetaBoxs.Add(new UserMetaBox(x));

            var dialog = (await RestClient.GetDialogChatAsync(x.Id)).Id;

            var message = await RestClient.GetLatestChatMessageAsync(dialog);
            RestClient.SubscribeForNewMesages(dialog, message?.Id ?? -1, NewMessagesHandler);
            if (message == null) return;
            var box = new HistoryMetaBox(x);
            box.InfoFetched += OnHistoryInfoFetched;
            HistoryMetaBoxs.Add(box);
        }

        private async Task AddChatAndSubscribe(Chat x)
        {
            ChatsMetaBoxs.Add(new ChatMetaBox(x));
            var message = await RestClient.GetLatestChatMessageAsync(x.Id);
            RestClient.SubscribeForNewMesages(x.Id, message?.Id ?? -1, NewMessagesHandler);
            if (message == null) return;
            var box = new HistoryMetaBox(x);
            box.InfoFetched += OnHistoryInfoFetched;
            HistoryMetaBoxs.Add(box);
        }
        #endregion

        #region View
        public ObservableCollection<UserMetaBox> UsersMetaBoxs { get; set; } = new ObservableCollection<UserMetaBox>();
        public ObservableCollection<ChatMetaBox> ChatsMetaBoxs { get; set; } = new ObservableCollection<ChatMetaBox>();
        public ObservableCollection<MetaBox> HistoryMetaBoxs { get; set; } = new ObservableCollection<MetaBox>();
        public ObservableCollection<ChatMessageBox> ChatMessagesBoxs { get; set; } = new ObservableCollection<ChatMessageBox>();

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
            var users = RestClient.GetAllUsersAsync().Result;
            if (users != null && users.Any())
                users.ForEach(async x => await AddUserAndSubscribe(x));

            /* Get user's groups and display them */
            var chats = RestClient.GetUserChatsAsync().Result?.Where(x => x.ChatType != ChatTypes.Dialog).ToList();
            if (chats != null && chats.Any())
                chats.ForEach(async x => await AddChatAndSubscribe(x));

            HistoryListView.ItemsSource = HistoryMetaBoxs;
            var view = (CollectionView)CollectionViewSource.GetDefaultView(HistoryListView.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("MetaDateTime", ListSortDirection.Descending));

            UsersListView.ItemsSource = UsersMetaBoxs;
            ChatsListView.ItemsSource = ChatsMetaBoxs;
            MessagesListView.ItemsSource = ChatMessagesBoxs;

            RestClient.SubscribeForNewChats(chats?.LastOrDefault()?.Id ?? -1, NewChatsHandler);
            RestClient.SubscribeForNewUsers(users?.LastOrDefault()?.Id ?? -1, NewUsersHandler);

            DisplayChatRegion = Visibility.Hidden;

            new Thread(ExpiredMessagesDeleter).Start();
        }

        private void ExpiredMessagesDeleter()
        {
            while (true)
            {
                if (ChatMessagesBoxs != null && ChatMessagesBoxs.Any())
                {
                    ChatMessagesBoxs
                        .Where(x => x.ChatMessage.ExpirationDate != null &&
                                    DateTime.Compare((DateTime) x.ChatMessage.ExpirationDate, DateTime.Now) < 0)
                        .ToList().ForEach(x => Application.Current.Dispatcher.Invoke(() => ChatMessagesBoxs.Remove(x)));
                }
                Thread.Sleep(1000);
            }
        }

        #region LongPollingHandlers
        private async void NewMessagesHandler(object sender, (int, List<Message>) valueTuple)
        {
            (var chatId, var messagesListView) = valueTuple;
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                if (chatId == _currChat)
                {
                    messagesListView.ForEach(x => ChatMessagesBoxs.Add(new ChatMessageBox(x)));

                }

                var box = HistoryMetaBoxs.Cast<HistoryMetaBox>().FirstOrDefault(x => x.ChatId == chatId);
                // if no history yet
                if (box == null)
                {
                    box = new HistoryMetaBox(await RestClient.GetChatAsync(chatId));
                    box.InfoFetched += OnHistoryInfoFetched;
                    HistoryMetaBoxs.Add(box);
                }
                else
                {
                    await box.UpdateInfo();
                }
            });
        }

        private void NewChatsHandler(object sender, List<Chat> chats)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                chats.ForEach(async x => await AddChatAndSubscribe(x));
            });
        }

        private void NewUsersHandler(object sender, List<User> users)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                users.ForEach(async x => await AddUserAndSubscribe(x));
            });
        }
        #endregion

        private void SetCurrentChatBox(MetaBox newBox)
        {
            CurrentChatBox.MetaTitle = newBox.MetaTitle;
            CurrentChatBox.MetaImage = newBox.MetaImage;
            CurrentChatBox.MetaSecondaryInfo = newBox.MetaSecondaryInfo;
        }

        private async Task OnChatSelectionChanged(MetaBox metaBox,int chatId, bool isDialog)
        {
            SetCurrentChatBox(metaBox);
            DisplayChatRegion = Visibility.Visible;

            _currChat = chatId;

            ChatMessagesBoxs.Clear();

            var messages = await RestClient.GetChatMessagesAsync(chatId);
            messages.ForEach(x => ChatMessagesBoxs.Add(new ChatMessageBox(x)));

            await SendMessageBox.SetMessageBox(chatId, isDialog);
        }

        private async void UserPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersListView.SelectedIndex == -1)
                return;
            ChatsListView.SelectedIndex = -1;
            HistoryListView.SelectedIndex = -1;

            var userBox = (UserMetaBox)e.AddedItems[0];
            var chat = await RestClient.GetDialogChatAsync(userBox.DisplayedUser.Id);
            await OnChatSelectionChanged(userBox, chat.Id, true);
        }

        private async void ChatPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatsListView.SelectedIndex == -1)
                return;
            UsersListView.SelectedIndex = -1;
            HistoryListView.SelectedIndex = -1;

            var chatBox = (ChatMetaBox) e.AddedItems[0];
            await OnChatSelectionChanged(chatBox, chatBox.DisplayedChat.Id, false);
        }

        private async void HistoryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListView.SelectedIndex == -1)
                return;
            UsersListView.SelectedIndex = -1;
            ChatsListView.SelectedIndex = -1;

            var historyBox = (HistoryMetaBox) e.AddedItems[0];
            var box = historyBox.DisplayedChat != null
                ? new ChatMetaBox(historyBox.DisplayedChat)
                : (MetaBox)new UserMetaBox(historyBox.DisplayedUser);

            var chat = historyBox.DisplayedChat ?? await RestClient.GetDialogChatAsync(historyBox.DisplayedUser.Id);
            await OnChatSelectionChanged(box, chat.Id, historyBox.DisplayedChat == null);
        }
        #endregion

        private void OnHistoryInfoFetched(object sender, EventArgs eventArgs)
        {
            CollectionViewSource.GetDefaultView(HistoryListView.ItemsSource).Refresh();
        }

        private async void CreateGroupMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var newGroupWindow = new NewGroupWindow(UsersMetaBoxs.Select(x => x.DisplayedUser).Where(x => x.Id != RestClient.UserId));
            newGroupWindow.ShowDialog();
            if (newGroupWindow.DialogResult != null && (bool)newGroupWindow.DialogResult)
            {
                await RestClient.CreateNewGroupChat(newGroupWindow.ChatName,
                    newGroupWindow.SelectedUsers.Select(x => x.Id));
            }
        }
    }
}
