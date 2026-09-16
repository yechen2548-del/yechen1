using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IGameCommand
    {
    }

    public interface ICommandHandler<TCommand> where TCommand : IGameCommand
    {
        void Handle(TCommand command);
    }

    public interface ICommandBus
    {
        void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : IGameCommand;
        void Send<TCommand>(TCommand command) where TCommand : IGameCommand;
    }

    public sealed class GameCommandBus : ICommandBus
    {
        private readonly Dictionary<Type, object> handlers = new Dictionary<Type, object>();

        public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : IGameCommand
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (handlers.ContainsKey(typeof(TCommand)))
            {
                throw new InvalidOperationException("A command handler is already registered for " + typeof(TCommand).FullName + ".");
            }

            handlers.Add(typeof(TCommand), handler);
        }

        public void Send<TCommand>(TCommand command) where TCommand : IGameCommand
        {
            if (ReferenceEquals(command, null))
            {
                throw new ArgumentNullException(nameof(command));
            }

            object rawHandler;
            if (!handlers.TryGetValue(typeof(TCommand), out rawHandler))
            {
                throw new InvalidOperationException("No command handler is registered for " + typeof(TCommand).FullName + ".");
            }

            ((ICommandHandler<TCommand>)rawHandler).Handle(command);
        }
    }
}
