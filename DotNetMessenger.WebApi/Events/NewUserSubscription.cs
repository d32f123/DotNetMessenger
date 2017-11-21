using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Events
{
    public class NewUserSubscription : ISubscribable
    {
        private static EventHandler _newUserSubscribers;
        private object _locker = new object();

        public void InvokeFor(object sender, int _)
        {
            lock (_locker)
            {
                _newUserSubscribers?.Invoke(sender, null);
                NLogger.Logger.Debug("NewUserSubscription notified");
            }
        }

        public void SubscribeTo(int _, EventHandler handler)
        {
            lock (_locker)
            {
                if (_newUserSubscribers == null)
                    _newUserSubscribers = handler;
                _newUserSubscribers += handler;
                NLogger.Logger.Debug("NewUserSubscription added: {0}", handler);
            }
        }

        public void UnsubscribeFrom(int _, EventHandler handler)
        {
            lock (_locker)
            {
                if (_newUserSubscribers == null)
                    return;
                _newUserSubscribers -= handler;
                NLogger.Logger.Debug("ChatSubscription removed: {0}.", handler);
            }
        }
    }
}