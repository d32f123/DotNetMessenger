using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Events
{
    public class ChatSubscription : ISubscribable
    {
        private static readonly Dictionary<int, EventHandler> NewChatEvents = new Dictionary<int, EventHandler>();

        public void InvokeFor(object sender, int userId)
        {
            lock (NewChatEvents)
            {
                if (NewChatEvents.ContainsKey(userId))
                {
                    NewChatEvents[userId]?.Invoke(sender, null);
                }
            }
            NLogger.Logger.Debug("ChatSubscribtion notified for {0}", userId);
        }

        public void SubscribeTo(int userId, EventHandler handler)
        {
            lock (NewChatEvents)
            {
                if (!NewChatEvents.ContainsKey(userId))
                {
                    NewChatEvents.Add(userId, handler);
                }
                else
                {
                    NewChatEvents[userId] += handler;
                }
            }
            NLogger.Logger.Debug("ChatSubscription added for {0}.", userId);
        }

        public void UnsubscribeFrom(int entityId, EventHandler handler)
        {
            lock (NewChatEvents)
            {
                if (!NewChatEvents.ContainsKey(entityId) || NewChatEvents[entityId] == null)
                    return;
                NewChatEvents[entityId] -= handler;
            }
            NLogger.Logger.Debug("ChatSubscription removed for {0}.", entityId);
        }
    }
}