using System;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IGameClock
    {
        double ElapsedSeconds { get; }
        float TimeScale { get; set; }
        bool IsPaused { get; }
        void Pause();
        void Resume();
        void Advance(float realDeltaSeconds);
    }

    public sealed class GameClock : IGameClock
    {
        private double elapsedSeconds;

        public double ElapsedSeconds { get { return elapsedSeconds; } }
        public float TimeScale { get; set; }
        public bool IsPaused { get; private set; }

        public GameClock()
        {
            TimeScale = 1f;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Advance(float realDeltaSeconds)
        {
            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds));
            }

            if (TimeScale < 0f)
            {
                throw new InvalidOperationException("TimeScale cannot be negative.");
            }

            if (!IsPaused)
            {
                elapsedSeconds += realDeltaSeconds * TimeScale;
            }
        }
    }
}
