using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public enum GameLogLevel
    {
        Info,
        Warning,
        Error
    }

    public enum GameLogCategory
    {
        Core,
        Simulation,
        AI,
        Save,
        Content,
        Debug,
        Presentation
    }

    public struct GameLogMessage
    {
        public DateTime TimestampUtc;
        public GameLogLevel Level;
        public GameLogCategory Category;
        public string Message;
    }

    public interface ILogSink
    {
        void Write(GameLogMessage message);
    }

    public interface IGameLogger
    {
        bool Enabled { get; set; }
        void Log(GameLogLevel level, GameLogCategory category, string message);
        void Info(GameLogCategory category, string message);
        void Warning(GameLogCategory category, string message);
        void Error(GameLogCategory category, string message);
    }

    public sealed class GameLogger : IGameLogger
    {
        private readonly ILogSink sink;

        public GameLogger(ILogSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            Enabled = true;
        }

        public bool Enabled { get; set; }

        public void Log(GameLogLevel level, GameLogCategory category, string message)
        {
            if (!Enabled)
            {
                return;
            }

            sink.Write(new GameLogMessage
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message ?? string.Empty
            });
        }

        public void Info(GameLogCategory category, string message)
        {
            Log(GameLogLevel.Info, category, message);
        }

        public void Warning(GameLogCategory category, string message)
        {
            Log(GameLogLevel.Warning, category, message);
        }

        public void Error(GameLogCategory category, string message)
        {
            Log(GameLogLevel.Error, category, message);
        }
    }

    public sealed class MemoryLogSink : ILogSink
    {
        private readonly List<GameLogMessage> entries = new List<GameLogMessage>();

        public IReadOnlyList<GameLogMessage> Entries
        {
            get { return entries; }
        }

        public void Write(GameLogMessage message)
        {
            entries.Add(message);
        }
    }
}
