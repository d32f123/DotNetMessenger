using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.WPFClient.Controls
{
    public class ChatMetaBox : MetaBox
    {
        private Chat _displayedChat;
        public Chat DisplayedChat
        {
            get => _displayedChat;
            set
            {
                _displayedChat = value;
                base.MetaImage = _displayedChat.Info.Avatar;
                base.MetaTitle = string.IsNullOrEmpty(_displayedChat.Info.Title)
                    ? $"#{_displayedChat.Id}"
                    : _displayedChat.Info.Title;
                base.MetaSecondaryInfo = null;
            }
        }

        public ChatMetaBox() : base() { }

        public ChatMetaBox(Chat chat) : base()
        {
            DisplayedChat = chat;
        }
    }
}
