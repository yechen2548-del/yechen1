using System;
using MingDynasty.OpenToolkit.Core;

namespace MingDynasty.OpenToolkit.Simulation
{
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    [Serializable]
    public sealed class CalendarDefinition
    {
        public string CalendarId = "generic-12x30";
        public string EraDisplayName = "Year";
        public int MonthsPerYear = 12;
        public int DaysPerMonth = 30;
        public Season[] SeasonsByMonth =
        {
            Season.Winter, Season.Winter, Season.Spring,
            Season.Spring, Season.Spring, Season.Summer,
            Season.Summer, Season.Summer, Season.Autumn,
            Season.Autumn, Season.Autumn, Season.Winter
        };

        public void Validate()
        {
            if (MonthsPerYear <= 0 || DaysPerMonth <= 0)
            {
                throw new InvalidOperationException("Calendar month and day counts must be positive.");
            }

            if (SeasonsByMonth == null || SeasonsByMonth.Length != MonthsPerYear)
            {
                throw new InvalidOperationException("Calendar season mapping must have one entry per month.");
            }
        }
    }

    public readonly struct GameTime
    {
        public GameTime(int year, int month, int day, int hour, int minute, double totalHours)
        {
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            TotalHours = totalHours;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public int Hour { get; }
        public int Minute { get; }
        public double TotalHours { get; }

        public override string ToString()
        {
            return Year + "-" + Month.ToString("00") + "-" + Day.ToString("00") + " " + Hour.ToString("00") + ":" + Minute.ToString("00");
        }
    }

    public interface IGameCalendar
    {
        string CalendarId { get; }
        GameTime FromHours(double totalHours);
        double ToHours(GameTime time);
        Season GetSeason(GameTime time);
        string Format(GameTime time);
    }

    public sealed class GenericCalendar : IGameCalendar
    {
        private readonly CalendarDefinition definition;
        private readonly int daysPerYear;

        public GenericCalendar(CalendarDefinition definition)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.definition.Validate();
            daysPerYear = this.definition.MonthsPerYear * this.definition.DaysPerMonth;
        }

        public string CalendarId { get { return definition.CalendarId; } }

        public GameTime FromHours(double totalHours)
        {
            if (totalHours < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(totalHours));
            }

            int totalMinutes = (int)Math.Floor(totalHours * 60d + 0.000001d);
            int totalDays = totalMinutes / (24 * 60);
            int year = totalDays / daysPerYear + 1;
            int dayOfYear = totalDays % daysPerYear;
            int month = dayOfYear / definition.DaysPerMonth + 1;
            int day = dayOfYear % definition.DaysPerMonth + 1;
            int minuteOfDay = totalMinutes % (24 * 60);
            int hour = minuteOfDay / 60;
            int minute = minuteOfDay % 60;
            return new GameTime(year, month, day, hour, minute, totalHours);
        }

        public double ToHours(GameTime time)
        {
            if (time.Year < 1 || time.Month < 1 || time.Month > definition.MonthsPerYear ||
                time.Day < 1 || time.Day > definition.DaysPerMonth || time.Hour < 0 || time.Hour > 23 ||
                time.Minute < 0 || time.Minute > 59)
            {
                throw new ArgumentException("Game time is outside the calendar definition.", nameof(time));
            }

            int days = (time.Year - 1) * daysPerYear + (time.Month - 1) * definition.DaysPerMonth + (time.Day - 1);
            return days * 24d + time.Hour + time.Minute / 60d;
        }

        public Season GetSeason(GameTime time)
        {
            if (time.Month < 1 || time.Month > definition.SeasonsByMonth.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(time));
            }

            return definition.SeasonsByMonth[time.Month - 1];
        }

        public string Format(GameTime time)
        {
            return definition.EraDisplayName + " " + time.Year + ", " + time.Month.ToString("00") + "/" + time.Day.ToString("00") + " " + time.Hour.ToString("00") + ":" + time.Minute.ToString("00");
        }
    }

    public sealed class GameTimeAdvancedEvent : IGameEvent
    {
        public GameTimeAdvancedEvent(GameTime previous, GameTime current, double deltaGameHours)
        {
            Previous = previous;
            Current = current;
            DeltaGameHours = deltaGameHours;
        }

        public GameTime Previous { get; private set; }
        public GameTime Current { get; private set; }
        public double DeltaGameHours { get; private set; }
    }

    public sealed class SeasonChangedEvent : IGameEvent
    {
        public SeasonChangedEvent(Season previous, Season current, GameTime time)
        {
            Previous = previous;
            Current = current;
            Time = time;
        }

        public Season Previous { get; private set; }
        public Season Current { get; private set; }
        public GameTime Time { get; private set; }
    }

    public interface IGameTimeService
    {
        GameTime Current { get; }
        Season CurrentSeason { get; }
        string CalendarId { get; }
        double TotalGameHours { get; }
        float TimeScale { get; }
        bool IsPaused { get; }
        string FormatCurrentTime();
        void Pause();
        void Resume();
        void SetTimeScale(float timeScale);
        double AdvanceRealSeconds(float realDeltaSeconds);
        double AdvanceGameHours(double gameHours);
        void RestoreTotalGameHours(double restoredHours);
    }

    public sealed class GameTimeService : IGameTimeService
    {
        private readonly IGameClock realClock;
        private readonly IGameCalendar calendar;
        private readonly IGameEventBus eventBus;
        private readonly IGameLogger logger;
        private readonly double secondsPerGameHour;
        private double totalGameHours;
        private Season currentSeason;

        public GameTimeService(
            IGameClock realClock,
            IGameCalendar calendar,
            IGameEventBus eventBus,
            IGameLogger logger,
            double secondsPerGameHour)
        {
            if (secondsPerGameHour <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerGameHour));
            }

            this.realClock = realClock ?? throw new ArgumentNullException(nameof(realClock));
            this.calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            this.eventBus = eventBus;
            this.logger = logger;
            this.secondsPerGameHour = secondsPerGameHour;
            Current = calendar.FromHours(0d);
            currentSeason = calendar.GetSeason(Current);
        }

        public GameTime Current { get; private set; }
        public Season CurrentSeason { get { return currentSeason; } }
        public string CalendarId { get { return calendar.CalendarId; } }
        public double TotalGameHours { get { return totalGameHours; } }
        public float TimeScale { get { return realClock.TimeScale; } }
        public bool IsPaused { get { return realClock.IsPaused; } }

        public string FormatCurrentTime()
        {
            return calendar.Format(Current);
        }

        public void Pause()
        {
            realClock.Pause();
            logger?.Info(GameLogCategory.Simulation, "Game time paused.");
        }

        public void Resume()
        {
            realClock.Resume();
            logger?.Info(GameLogCategory.Simulation, "Game time resumed.");
        }

        public void SetTimeScale(float timeScale)
        {
            if (timeScale < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeScale));
            }

            realClock.TimeScale = timeScale;
            logger?.Info(GameLogCategory.Simulation, "Game time scale set to " + timeScale + ".");
        }

        public double AdvanceRealSeconds(float realDeltaSeconds)
        {
            double previousElapsed = realClock.ElapsedSeconds;
            realClock.Advance(realDeltaSeconds);
            double gameHours = (realClock.ElapsedSeconds - previousElapsed) / secondsPerGameHour;
            return AdvanceInternal(gameHours);
        }

        public double AdvanceGameHours(double gameHours)
        {
            if (gameHours < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(gameHours));
            }

            return AdvanceInternal(gameHours);
        }

        public void RestoreTotalGameHours(double restoredHours)
        {
            if (restoredHours < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(restoredHours));
            }

            totalGameHours = restoredHours;
            Current = calendar.FromHours(totalGameHours);
            currentSeason = calendar.GetSeason(Current);
        }

        private double AdvanceInternal(double gameHours)
        {
            if (gameHours <= 0d)
            {
                return 0d;
            }

            GameTime previous = Current;
            Season previousSeason = currentSeason;
            totalGameHours += gameHours;
            Current = calendar.FromHours(totalGameHours);
            currentSeason = calendar.GetSeason(Current);
            eventBus?.Publish(new GameTimeAdvancedEvent(previous, Current, gameHours));
            if (previousSeason != currentSeason)
            {
                eventBus?.Publish(new SeasonChangedEvent(previousSeason, currentSeason, Current));
                logger?.Info(GameLogCategory.Simulation, "Season changed to " + currentSeason + ".");
            }

            return gameHours;
        }
    }
}
