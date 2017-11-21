using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.WPFClient.Controls
{
    public class HistoryMetaBox : MetaBox
    {
        public int ChatId { get; private set; }

        private Chat _displayedChat;
        public Chat DisplayedChat
        {
            get => _displayedChat;
            set
            {
                if (value.ChatType == ChatTypes.Dialog)
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var usersList = value.Users.ToList();
                        DisplayedUser = await RestClient.GetUserAsync(usersList.Count == 1
                            ? usersList[0]
                            : usersList[0] == RestClient.UserId
                                ? usersList[1]
                                : usersList[0]);
                    });
                    return;
                }
                _displayedUser = null;
                _displayedChat = value;
                base.MetaImage = _displayedChat.Info?.Avatar;
                base.MetaTitle = string.IsNullOrEmpty(_displayedChat.Info?.Title)
                    ? $"#{_displayedChat.Id}"
                    : _displayedChat.Info.Title;
                ChatId = value.Id;
                GetLastChatMessage();
            }
        }

        private async Task GetLastChatMessage()
        {
            var message = await RestClient.GetLatestChatMessageAsync(ChatId);
            CurrentMessage = message;

            lock (_lock)
            {
                _messageCheckerShouldExit = true;
                _expirableMessageCheckerThread?.Join();
                _messageCheckerShouldExit = false;
            }

            if (message == null)
            {
                base.MetaSecondaryInfo = "";
                base.MetaDateTime = DateTime.MinValue;
                InfoFetched?.Invoke(this, null);
                return;
            }

            base.MetaDateTime = message.Date;
            if (string.IsNullOrEmpty(message.Text))
            {
                // if there is no text, there should be a file attached
                base.MetaSecondaryInfo = "<attachment>";
            }
            else if (message.Text.Length > 15)
            {
                base.MetaSecondaryInfo = message.Text.Substring(0, 15) + "...";
            }
            else
            {
                base.MetaSecondaryInfo = message.Text;
            }

            if (message.ExpirationDate != null)
            {
                _expirableMessageCheckerThread = new Thread(ExpirableMessageChecker);
                _expirableMessageCheckerThread.Start();
            }

            InfoFetched?.Invoke(this, null);
        }

        private async Task SetUserAsync(int userId)
        {
            var chat = await RestClient.GetDialogChatAsync(_displayedUser.Id);
            ChatId = chat.Id;
            await GetLastChatMessage();
        }

        private User _displayedUser;

        public User DisplayedUser
        {
            get => _displayedUser;
            set
            {
                _displayedChat = null;
                _displayedUser = value;
                base.MetaImage = _displayedUser.UserInfo?.Avatar;
                base.MetaTitle = _displayedUser.Username;
                SetUserAsync(_displayedUser.Id);
            }
        }

        public Message CurrentMessage { get; private set; }
        private bool _messageCheckerShouldExit;
        private readonly object _lock = new object();
        private Thread _expirableMessageCheckerThread;
        private void ExpirableMessageChecker()
        {
            while (true)
            {
                if (CurrentMessage.ExpirationDate.HasValue &&
                    DateTime.Compare((DateTime) CurrentMessage.ExpirationDate, DateTime.Now) < 0)
                {
                    Task.Run(() => Application.Current.Dispatcher.Invoke(async () => await UpdateInfo()));
                    return;
                }
                lock (_lock)
                {
                    if (_messageCheckerShouldExit)
                    {
                        return;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public async Task UpdateInfo()
        {
            await GetLastChatMessage();
        }

        public HistoryMetaBox() : base() { }

        public HistoryMetaBox(Chat chat) : base()
        {
            DisplayedChat = chat;
        }

        public HistoryMetaBox(User user) : base()
        {
            DisplayedUser = user;
        }

        public event EventHandler InfoFetched;
    }
}
