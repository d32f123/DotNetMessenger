using System;

namespace DotNetMessenger.WebApi.Events
{
    public interface ISubscribable
    {
        void InvokeFor(object sender, int entityId);
        void SubscribeTo(int entityId, EventHandler handler);
        void UnsubscribeFrom(int entityId, EventHandler handler);
    }
}