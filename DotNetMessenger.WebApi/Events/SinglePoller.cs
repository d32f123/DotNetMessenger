using System;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Events
{
    public class SinglePoller
    {
        private readonly ISubscribable _subscrption;
        private readonly int _entityId;
        public bool SubscriptionInvoked { get; private set; }

        public SinglePoller(ISubscribable subscription, int entityId)
        {
            NLogger.Logger.Debug("Subscibing to {0}. Entity id: {1}", subscription, entityId);
            subscription.SubscribeTo(entityId, Handler);
            _subscrption = subscription;
            _entityId = entityId;
        }

        private void Handler(object sender, EventArgs eventArgs)
        {
            NLogger.Logger.Debug("Subscription acknowledged. Notifying subscriber");
            SubscriptionInvoked = true;
            NLogger.Logger.Debug("Unsubscribing from {0} with entity id: {1}", _subscrption, _entityId);
            _subscrption.UnsubscribeFrom(_entityId, Handler);
        }
    }
}