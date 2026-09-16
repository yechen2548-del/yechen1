using System;
using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;
using NUnit.Framework;

namespace MingDynasty.OpenToolkit.Tests
{
    public sealed class CoreInfrastructureTests
    {
        [Test]
        public void EventBusSubscriptionCanBeDisposed()
        {
            GameEventBus events = new GameEventBus();
            int received = 0;
            IDisposable subscription = events.Subscribe<TestEvent>(value => received++);

            events.Publish(new TestEvent());
            subscription.Dispose();
            events.Publish(new TestEvent());

            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void CommandBusRejectsDuplicateHandlersAndDispatches()
        {
            GameCommandBus commands = new GameCommandBus();
            TestCommandHandler handler = new TestCommandHandler();
            commands.Register<TestCommand>(handler);

            commands.Send(new TestCommand { Amount = 7 });

            Assert.That(handler.LastAmount, Is.EqualTo(7));
            Assert.Throws<InvalidOperationException>(() => commands.Register<TestCommand>(new TestCommandHandler()));
        }

        [Test]
        public void SchedulerRunsDueTasksInTimeOrder()
        {
            GameTaskScheduler scheduler = new GameTaskScheduler();
            List<int> order = new List<int>();
            scheduler.Schedule(3d, () => order.Add(3));
            scheduler.Schedule(1d, () => order.Add(1));

            Assert.That(scheduler.Advance(3d), Is.EqualTo(2));
            Assert.That(order, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void VersionedSaveServiceAppliesMigrationChain()
        {
            MemoryLogSink sink = new MemoryLogSink();
            VersionedSaveService service = new VersionedSaveService(
                2,
                new TestSaveSerializer(),
                new[] { new TestSaveMigration(1, 2) },
                new GameLogger(sink));

            SaveEnvelope loaded = service.Load("serialized");

            Assert.That(loaded.SaveVersion, Is.EqualTo(2));
            Assert.That(sink.Entries.Count, Is.EqualTo(1));
        }

        private sealed class TestEvent : IGameEvent
        {
        }

        private sealed class TestCommand : IGameCommand
        {
            public int Amount;
        }

        private sealed class TestCommandHandler : ICommandHandler<TestCommand>
        {
            public int LastAmount { get; private set; }

            public void Handle(TestCommand command)
            {
                LastAmount = command.Amount;
            }
        }

        private sealed class TestSaveSerializer : ISaveSerializer
        {
            public string Serialize(SaveEnvelope envelope)
            {
                return envelope.SaveVersion.ToString();
            }

            public SaveEnvelope Deserialize(string serializedData)
            {
                return new SaveEnvelope { SaveVersion = 1 };
            }
        }

        private sealed class TestSaveMigration : ISaveMigration
        {
            public TestSaveMigration(int fromVersion, int toVersion)
            {
                FromVersion = fromVersion;
                ToVersion = toVersion;
            }

            public int FromVersion { get; private set; }
            public int ToVersion { get; private set; }

            public void Migrate(SaveEnvelope envelope)
            {
                envelope.SaveVersion = ToVersion;
            }
        }
    }
}
