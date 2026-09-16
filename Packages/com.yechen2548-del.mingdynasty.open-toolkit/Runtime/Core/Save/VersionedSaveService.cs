using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    [Serializable]
    public sealed class SaveModuleVersion
    {
        public string PackId;
        public string Version;
    }

    [Serializable]
    public sealed class SaveEnvelope
    {
        public int SaveVersion;
        public string ApplicationVersion;
        public string SaveId;
        public string CreatedUtc;
        public List<SaveModuleVersion> Modules = new List<SaveModuleVersion>();
        public string PayloadJson;
    }

    public interface ISaveSerializer
    {
        string Serialize(SaveEnvelope envelope);
        SaveEnvelope Deserialize(string serializedData);
    }

    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        void Migrate(SaveEnvelope envelope);
    }

    public interface IVersionedSaveService
    {
        string Serialize(SaveEnvelope envelope);
        SaveEnvelope Load(string serializedData);
    }

    /// <summary>
    /// Applies one-version-at-a-time migrations. The serializer is injected so Unity JSON, binary, or tests can differ.
    /// </summary>
    public sealed class VersionedSaveService : IVersionedSaveService
    {
        private readonly int currentVersion;
        private readonly ISaveSerializer serializer;
        private readonly List<ISaveMigration> migrations;
        private readonly IGameLogger logger;

        public VersionedSaveService(int currentVersion, ISaveSerializer serializer, IEnumerable<ISaveMigration> migrations, IGameLogger logger)
        {
            if (currentVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(currentVersion));
            }

            this.currentVersion = currentVersion;
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.migrations = new List<ISaveMigration>(migrations ?? throw new ArgumentNullException(nameof(migrations)));
            this.logger = logger;
        }

        public string Serialize(SaveEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.SaveVersion != currentVersion)
            {
                throw new InvalidOperationException("Only current save versions can be serialized. Expected " + currentVersion + ".");
            }

            return serializer.Serialize(envelope);
        }

        public SaveEnvelope Load(string serializedData)
        {
            if (string.IsNullOrWhiteSpace(serializedData))
            {
                throw new ArgumentException("Save data cannot be empty.", nameof(serializedData));
            }

            SaveEnvelope envelope = serializer.Deserialize(serializedData);
            if (envelope == null)
            {
                throw new InvalidOperationException("The save serializer returned no envelope.");
            }

            if (envelope.SaveVersion > currentVersion)
            {
                throw new InvalidOperationException("The save is newer than this game version.");
            }

            while (envelope.SaveVersion < currentVersion)
            {
                ISaveMigration migration = FindMigration(envelope.SaveVersion);
                if (migration == null)
                {
                    throw new InvalidOperationException("No save migration exists from version " + envelope.SaveVersion + ".");
                }

                migration.Migrate(envelope);
                if (envelope.SaveVersion != migration.ToVersion)
                {
                    throw new InvalidOperationException("Save migration did not set the expected target version.");
                }

                logger?.Info(GameLogCategory.Save, "Migrated save from " + migration.FromVersion + " to " + migration.ToVersion + ".");
            }

            return envelope;
        }

        private ISaveMigration FindMigration(int fromVersion)
        {
            for (int i = 0; i < migrations.Count; i++)
            {
                if (migrations[i].FromVersion == fromVersion && migrations[i].ToVersion > fromVersion)
                {
                    return migrations[i];
                }
            }

            return null;
        }
    }
}
