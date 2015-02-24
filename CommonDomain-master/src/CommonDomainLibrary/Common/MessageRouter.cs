using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonDomainLibrary.Common
{
    public class MessageRouter
    {
        private readonly IState _state;

        private MessageRouter(IState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            _state = state;
        }

        public bool HasCausation(Guid id)
        {
            return Messages.ContainsKey(id);
        }

        public IEnumerable<dynamic> GetMessages()
        {
            return Messages.Values.SelectMany(v => v, (v, c) => c);
        }

        public IEnumerable<dynamic> GetMessages(Guid causationId)
        {
            if (!HasCausation(causationId))
            {
                return Enumerable.Empty<dynamic>();
            }

            return Messages[causationId];
        }

        public IEnumerable<IMessage> GetCommands(Guid causationId)
        {
            if(!HasCausation(causationId))
            {
                return Enumerable.Empty<IMessage>();
            }

            return Messages[causationId].Where(m => !(m is IEvent) && !(m is DeferrableMessage)).Cast<IMessage>();
        }

        public IEnumerable<DeferrableMessage> GetDeferredCommands(Guid causationId)
        {
            if (!Messages.ContainsKey(causationId))
            {
                return Enumerable.Empty<DeferrableMessage>();
            }

            return new List<DeferrableMessage>(Messages[causationId].Where(m => m is DeferrableMessage).Cast<DeferrableMessage>());
        }

        public IEnumerable<IEvent> GetEvents(Guid causationId)
        {
            if (!Messages.ContainsKey(causationId))
            {
                return Enumerable.Empty<IEvent>();
            }

            return new List<IEvent>(Messages[causationId].Where(m => m is IEvent).Cast<IEvent>());
        }

        public void RaiseMessage(dynamic m)
        {
            IMessage message = m.GetType() ==  typeof(DeferrableMessage) ? m.Message : m;
            
            CreateKeyIfNotExists(message.CausationId);

            Messages[message.CausationId].Add(m);

            try
            {
                if (message is IEvent) ((dynamic) _state).Apply((dynamic)message);
            }
            catch (Exception)
            {
            }
        }

        private void CreateKeyIfNotExists(Guid causationId)
        {
            if (!Messages.ContainsKey(causationId))
            {
                Messages.Add(causationId, new List<dynamic>());
            }
        }        

        private OrderedDictionary<Guid, List<dynamic>> Messages
        {
            get { return _state.Messages; }
        }

        public static MessageRouter For(dynamic state)
        {
            return new MessageRouter(state);
        }        
    }
}
