using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public readonly struct ScheduledTaskId : IEquatable<ScheduledTaskId>
    {
        public ScheduledTaskId(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public bool Equals(ScheduledTaskId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ScheduledTaskId && Equals((ScheduledTaskId)obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public interface ITaskScheduler
    {
        ScheduledTaskId Schedule(double delaySeconds, Action action);
        bool Cancel(ScheduledTaskId taskId);
        int Advance(double deltaSeconds);
    }

    public sealed class GameTaskScheduler : ITaskScheduler
    {
        private readonly List<ScheduledTask> tasks = new List<ScheduledTask>();
        private long nextId = 1;
        private double elapsedSeconds;

        public ScheduledTaskId Schedule(double delaySeconds, Action action)
        {
            if (delaySeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ScheduledTaskId id = new ScheduledTaskId(nextId++);
            tasks.Add(new ScheduledTask(id, elapsedSeconds + delaySeconds, action));
            return id;
        }

        public bool Cancel(ScheduledTaskId taskId)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Id.Equals(taskId))
                {
                    tasks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public int Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            elapsedSeconds += deltaSeconds;
            List<ScheduledTask> dueTasks = new List<ScheduledTask>();
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                if (tasks[i].DueAtSeconds <= elapsedSeconds)
                {
                    dueTasks.Add(tasks[i]);
                    tasks.RemoveAt(i);
                }
            }

            dueTasks.Sort(delegate(ScheduledTask left, ScheduledTask right)
            {
                int dueTime = left.DueAtSeconds.CompareTo(right.DueAtSeconds);
                return dueTime != 0 ? dueTime : left.Id.Value.CompareTo(right.Id.Value);
            });

            for (int i = 0; i < dueTasks.Count; i++)
            {
                dueTasks[i].Action();
            }

            return dueTasks.Count;
        }

        private sealed class ScheduledTask
        {
            public ScheduledTask(ScheduledTaskId id, double dueAtSeconds, Action action)
            {
                Id = id;
                DueAtSeconds = dueAtSeconds;
                Action = action;
            }

            public ScheduledTaskId Id { get; private set; }
            public double DueAtSeconds { get; private set; }
            public Action Action { get; private set; }
        }
    }
}
