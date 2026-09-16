using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;
using MingDynasty.OpenToolkit.Simulation;
using NUnit.Framework;

namespace MingDynasty.OpenToolkit.Tests
{
    public sealed class SimulationInfrastructureTests
    {
        [Test]
        public void CoordinateTransformUsesFlooringForNegativeWorldPositions()
        {
            CoordinateTransform transform = new CoordinateTransform(new WorldCoordinate(0d, 0d), 100d, 10d);

            ChunkCoordinate chunk = transform.WorldToChunk(new WorldCoordinate(-0.1d, -10.1d));

            Assert.That(chunk, Is.EqualTo(new ChunkCoordinate(-1, -2)));
        }

        [Test]
        public void ChunkLifecycleMovesThroughIntermediateStates()
        {
            ChunkRegistry registry = new ChunkRegistry();
            ChunkData chunk = new ChunkData(
                StableId.New("chunk"),
                new ChunkCoordinate(0, 0),
                new WorldBounds(0d, 0d, 10d, 10d));
            registry.Register(chunk);

            ChunkLifecycleSettings settings = new ChunkLifecycleSettings
            {
                ActiveRadiusChunks = 0,
                LoadRadiusChunks = 1,
                UnloadRadiusChunks = 2
            };
            ChunkLifecycleService lifecycle = new ChunkLifecycleService(
                registry,
                new CoordinateTransform(new WorldCoordinate(0d, 0d), 100d, 10d),
                settings,
                null,
                null);

            lifecycle.UpdateFocus(new WorldCoordinate(0d, 0d));

            Assert.That(chunk.LoadState, Is.EqualTo(ChunkLoadState.Active));
            Assert.That(lifecycle.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void InventoryTransactionConsumesAndCanRefundResources()
        {
            Inventory inventory = new Inventory(StableId.New("inventory"), 100);
            inventory.TryAdd("wood", 10);
            ResourceTransaction transaction = new ResourceTransaction(
                inventory,
                StableId.New("transaction"),
                new[] { new ResourceAmount("wood", 3) });

            Assert.That(transaction.TryCommit().Succeeded, Is.True);
            Assert.That(inventory.GetAmount("wood"), Is.EqualTo(7));
            Assert.That(transaction.TryRefund(), Is.True);
            Assert.That(inventory.GetAmount("wood"), Is.EqualTo(10));
        }

        [Test]
        public void LODWorkSchedulerRotatesAcrossEntities()
        {
            SimulationLodPolicy policy = new SimulationLodPolicy(10f, 20f, 30f);
            Assert.That(policy.Resolve(5f, true), Is.EqualTo(SimulationLevel.Full));
            Assert.That(policy.Resolve(25f, true), Is.EqualTo(SimulationLevel.Statistical));
            Assert.That(policy.Resolve(5f, false), Is.EqualTo(SimulationLevel.Dormant));

            ChunkWorkScheduler scheduler = new ChunkWorkScheduler();
            IReadOnlyList<StableId> ids = new[]
            {
                new StableId("a"),
                new StableId("b"),
                new StableId("c")
            };

            Assert.That(scheduler.Take(ids, 2), Is.EqualTo(new[] { new StableId("a"), new StableId("b") }));
            Assert.That(scheduler.Take(ids, 2), Is.EqualTo(new[] { new StableId("c"), new StableId("a") }));
        }

        [Test]
        public void TickServiceCapsCatchUpAndCountsTicks()
        {
            SimulationTickSettings settings = new SimulationTickSettings
            {
                FastIntervalHours = 1d,
                NormalIntervalHours = 2d,
                SlowIntervalHours = 4d,
                MaxTicksPerAdvance = 2
            };
            SimulationTickService ticks = new SimulationTickService(settings, null, null);

            ticks.Advance(5d, new GameTime(1, 1, 1, 0, 0, 5d));

            Assert.That(ticks.GetTickCount(SimulationTickKind.Fast), Is.EqualTo(2));
            Assert.That(ticks.GetTickCount(SimulationTickKind.Normal), Is.EqualTo(2));
            Assert.That(ticks.GetTickCount(SimulationTickKind.Slow), Is.EqualTo(1));
        }
    }
}
