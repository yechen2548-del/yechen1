using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IDebugCommand
    {
        string Id { get; }
        string Description { get; }
        string Execute(IReadOnlyList<string> arguments);
    }

    public interface IDebugCommandRegistry
    {
        void Register(IDebugCommand command);
        bool TryExecute(string id, IReadOnlyList<string> arguments, out string result);
        IReadOnlyList<IDebugCommand> GetAll();
    }

    /// <summary>
    /// Command-only debug surface. A UI can be added later without inventing fake buttons or bypassing domain APIs.
    /// </summary>
    public sealed class DebugCommandRegistry : IDebugCommandRegistry
    {
        private readonly Dictionary<string, IDebugCommand> commands = new Dictionary<string, IDebugCommand>(StringComparer.Ordinal);

        public void Register(IDebugCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.Id))
            {
                throw new ArgumentException("A debug command must have an ID.", nameof(command));
            }

            if (commands.ContainsKey(command.Id))
            {
                throw new InvalidOperationException("Duplicate debug command ID: " + command.Id);
            }

            commands.Add(command.Id, command);
        }

        public bool TryExecute(string id, IReadOnlyList<string> arguments, out string result)
        {
            IDebugCommand command;
            if (!commands.TryGetValue(id, out command))
            {
                result = "Unknown debug command: " + id;
                return false;
            }

            result = command.Execute(arguments ?? new string[0]);
            return true;
        }

        public IReadOnlyList<IDebugCommand> GetAll()
        {
            List<IDebugCommand> result = new List<IDebugCommand>(commands.Values);
            result.Sort(delegate(IDebugCommand left, IDebugCommand right)
            {
                return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
            return result;
        }
    }
}
