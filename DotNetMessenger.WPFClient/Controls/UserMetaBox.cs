using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.WPFClient.Controls
{
    public class UserMetaBox : MetaBox
    {
        private User _displayedUser;

        public User DisplayedUser
        {
            get => _displayedUser;
            set
            {
                _displayedUser = value;
                base.MetaImage = _displayedUser.UserInfo.Avatar;
                base.MetaTitle = _displayedUser.Username;
                base.MetaSecondaryInfo = _displayedUser.UserInfo.LastAndFirstName;
            }
        }

        public UserMetaBox() : base() { }

        public UserMetaBox(User user) : base()
        {
            DisplayedUser = user;
        }
    }
}
