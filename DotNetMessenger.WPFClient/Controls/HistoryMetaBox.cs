using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetMessenger.Model;

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
            base.MetaDateTime = message.Date;
            if (message.Text.Length > 15)
            {
                base.MetaSecondaryInfo = message.Text.Substring(0, 15) + "...";
            }
            else
            {
                base.MetaSecondaryInfo = message.Text;
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
