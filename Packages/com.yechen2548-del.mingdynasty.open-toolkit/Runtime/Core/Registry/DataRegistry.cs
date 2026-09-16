using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IDataDefinition
    {
        string Id { get; }
    }

    public interface IDataRegistry
    {
        void Register<TDefinition>(TDefinition definition) where TDefinition : IDataDefinition;
        bool TryGet<TDefinition>(string id, out TDefinition definition) where TDefinition : IDataDefinition;
        IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : IDataDefinition;
        int Count<TDefinition>() where TDefinition : IDataDefinition;
        void Clear();
    }

    /// <summary>
    /// Unified data registry. Definitions are keyed by type and stable ID to prevent cross-domain collisions.
    /// </summary>
    public sealed class DataRegistry : IDataRegistry
    {
        private readonly Dictionary<Type, Dictionary<string, IDataDefinition>> definitions =
            new Dictionary<Type, Dictionary<string, IDataDefinition>>();

        public void Register<TDefinition>(TDefinition definition) where TDefinition : IDataDefinition
        {
            if (ReferenceEquals(definition, null))
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException("A data definition must have a stable ID.", nameof(definition));
            }

            Dictionary<string, IDataDefinition> typedDefinitions;
            if (!definitions.TryGetValue(typeof(TDefinition), out typedDefinitions))
            {
                typedDefinitions = new Dictionary<string, IDataDefinition>(StringComparer.Ordinal);
                definitions.Add(typeof(TDefinition), typedDefinitions);
            }

            if (typedDefinitions.ContainsKey(definition.Id))
            {
                throw new InvalidOperationException("Duplicate data definition ID: " + definition.Id);
            }

            typedDefinitions.Add(definition.Id, definition);
        }

        public bool TryGet<TDefinition>(string id, out TDefinition definition) where TDefinition : IDataDefinition
        {
            definition = default(TDefinition);
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            Dictionary<string, IDataDefinition> typedDefinitions;
            IDataDefinition rawDefinition;
            if (!definitions.TryGetValue(typeof(TDefinition), out typedDefinitions) ||
                !typedDefinitions.TryGetValue(id, out rawDefinition))
            {
                return false;
            }

            definition = (TDefinition)rawDefinition;
            return true;
        }

        public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : IDataDefinition
        {
            List<TDefinition> result = new List<TDefinition>();
            Dictionary<string, IDataDefinition> typedDefinitions;
            if (!definitions.TryGetValue(typeof(TDefinition), out typedDefinitions))
            {
                return result;
            }

            foreach (IDataDefinition definition in typedDefinitions.Values)
            {
                result.Add((TDefinition)definition);
            }

            result.Sort(delegate(TDefinition left, TDefinition right)
            {
                return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
            return result;
        }

        public int Count<TDefinition>() where TDefinition : IDataDefinition
        {
            Dictionary<string, IDataDefinition> typedDefinitions;
            return definitions.TryGetValue(typeof(TDefinition), out typedDefinitions) ? typedDefinitions.Count : 0;
        }

        public void Clear()
        {
            definitions.Clear();
        }
    }
}
