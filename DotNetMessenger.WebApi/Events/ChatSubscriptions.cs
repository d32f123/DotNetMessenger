using System;
using System.Collections.Generic;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Events
{
    public class ChatSubscriptions : ISubscribable
    {
        private static readonly Dictionary<int, EventHandler> NewMessageEvents = new Dictionary<int, EventHandler>();

        public void InvokeFor(object sender, int chatId)
        {
            lock (NewMessageEvents)
            {
                if (NewMessageEvents.ContainsKey(chatId))
                {
                    NewMessageEvents[chatId]?.Invoke(sender, null);
                }
            }
            NLogger.Logger.Debug("ChatSubscribtion notified for {0}", chatId);
        }

        public void SubscribeTo(int chatId, EventHandler handler)
        {
            lock (NewMessageEvents)
            {
                if (!NewMessageEvents.ContainsKey(chatId))
                {
                    NewMessageEvents.Add(chatId, handler);
                }
                else
                {
                    NewMessageEvents[chatId] += handler;
                }
            }
            NLogger.Logger.Debug("ChatSubscription added for {0}.", chatId);
        }

        public void UnsubscribeFrom(int entityId, EventHandler handler)
        {
            lock (NewMessageEvents)
            {
                if (!NewMessageEvents.ContainsKey(entityId) || NewMessageEvents[entityId] == null)
                    return;
                NewMessageEvents[entityId] -= handler;
            }
            NLogger.Logger.Debug("ChatSubscription removed for {0}.", entityId);
        }
    }
}