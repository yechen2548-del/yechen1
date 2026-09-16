using System;
using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;

namespace MingDynasty.OpenToolkit.Simulation
{
    [Serializable]
    public struct ResourceAmount : IEquatable<ResourceAmount>
    {
        public ResourceAmount(string resourceId, int amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }

        public string ResourceId;
        public int Amount;

        public bool Equals(ResourceAmount other)
        {
            return string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceAmount && Equals((ResourceAmount)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ResourceId == null ? 0 : ResourceId.GetHashCode()) * 397) ^ Amount;
            }
        }

        public override string ToString()
        {
            return ResourceId + " x" + Amount;
        }
    }

    public enum InventoryFailureReason
    {
        None,
        InvalidAmount,
        InvalidResource,
        CapacityExceeded,
        InsufficientAvailable,
        ReservationExists,
        ReservationNotFound,
        ReservationMismatch,
        TargetRejected
    }

    public readonly struct InventoryOperationResult
    {
        public InventoryOperationResult(bool succeeded, InventoryFailureReason failure, string resourceId, int requested, int available, string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            ResourceId = resourceId ?? string.Empty;
            Requested = requested;
            Available = available;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public InventoryFailureReason Failure { get; }
        public string ResourceId { get; }
        public int Requested { get; }
        public int Available { get; }
        public string Message { get; }
    }

    [Serializable]
    public sealed class InventoryStackSnapshot
    {
        public string ResourceId;
        public int Amount;
        public int ReservedAmount;
    }

    public interface IInventory
    {
        StableId OwnerId { get; }
        int Capacity { get; }
        int UsedCapacity { get; }
        int ReservedCapacity { get; }
        int GetAmount(string resourceId);
        int GetAvailableAmount(string resourceId);
        int GetReservedAmount(string resourceId);
        InventoryOperationResult TryAdd(string resourceId, int amount);
        InventoryOperationResult TryRemove(string resourceId, int amount);
        InventoryOperationResult TryReserve(StableId reservationId, string resourceId, int amount);
        InventoryOperationResult TryReleaseReservation(StableId reservationId);
        InventoryOperationResult TryConsumeReservation(StableId reservationId);
        IReadOnlyList<InventoryStackSnapshot> GetSnapshot();
        void Clear();
        void RestoreSnapshot(IEnumerable<InventoryStackSnapshot> snapshot);
        InventoryOperationResult TryTransferTo(IInventory target, string resourceId, int amount);
    }

    public class Inventory : IInventory
    {
        private sealed class Reservation
        {
            public string ResourceId;
            public int Amount;
        }

        private readonly Dictionary<string, int> amounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<StableId, Reservation> reservations = new Dictionary<StableId, Reservation>();

        public Inventory(StableId ownerId, int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            OwnerId = ownerId;
            Capacity = capacity;
        }

        public StableId OwnerId { get; private set; }
        public int Capacity { get; private set; }

        public int UsedCapacity
        {
            get
            {
                int total = 0;
                foreach (int value in amounts.Values)
                {
                    total += value;
                }

                return total;
            }
        }

        public int ReservedCapacity
        {
            get
            {
                int total = 0;
                foreach (Reservation reservation in reservations.Values)
                {
                    total += reservation.Amount;
                }

                return total;
            }
        }

        public int GetAmount(string resourceId)
        {
            int amount;
            return !string.IsNullOrWhiteSpace(resourceId) && amounts.TryGetValue(resourceId, out amount) ? amount : 0;
        }

        public int GetAvailableAmount(string resourceId)
        {
            return GetAmount(resourceId) - GetReservedAmount(resourceId);
        }

        public int GetReservedAmount(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return 0;
            }

            int total = 0;
            foreach (Reservation reservation in reservations.Values)
            {
                if (string.Equals(reservation.ResourceId, resourceId, StringComparison.Ordinal))
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        public InventoryOperationResult TryAdd(string resourceId, int amount)
        {
            if (amount <= 0)
            {
                return Failure(InventoryFailureReason.InvalidAmount, resourceId, amount, 0, "Amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return Failure(InventoryFailureReason.InvalidResource, resourceId, amount, 0, "Resource ID cannot be empty.");
            }

            if (UsedCapacity + amount > Capacity)
            {
                return Failure(InventoryFailureReason.CapacityExceeded, resourceId, amount, Capacity - UsedCapacity, "Inventory capacity exceeded.");
            }

            int current;
            if (!amounts.TryGetValue(resourceId, out current))
            {
                current = 0;
            }

            amounts[resourceId] = checked(current + amount);
            return Success(resourceId, amount);
        }

        public InventoryOperationResult TryRemove(string resourceId, int amount)
        {
            if (amount <= 0)
            {
                return Failure(InventoryFailureReason.InvalidAmount, resourceId, amount, 0, "Amount must be positive.");
            }

            int available = GetAvailableAmount(resourceId);
            if (available < amount)
            {
                return Failure(InventoryFailureReason.InsufficientAvailable, resourceId, amount, available, "Insufficient unreserved resource.");
            }

            RemoveRaw(resourceId, amount);
            return Success(resourceId, amount);
        }

        public InventoryOperationResult TryReserve(StableId reservationId, string resourceId, int amount)
        {
            if (!reservationId.IsValid)
            {
                return Failure(InventoryFailureReason.ReservationNotFound, resourceId, amount, 0, "Reservation ID cannot be empty.");
            }

            if (amount <= 0)
            {
                return Failure(InventoryFailureReason.InvalidAmount, resourceId, amount, 0, "Amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return Failure(InventoryFailureReason.InvalidResource, resourceId, amount, 0, "Resource ID cannot be empty.");
            }

            if (reservations.ContainsKey(reservationId))
            {
                return Failure(InventoryFailureReason.ReservationExists, resourceId, amount, 0, "Reservation ID already exists.");
            }

            int available = GetAvailableAmount(resourceId);
            if (available < amount)
            {
                return Failure(InventoryFailureReason.InsufficientAvailable, resourceId, amount, available, "Insufficient unreserved resource.");
            }

            reservations.Add(reservationId, new Reservation { ResourceId = resourceId, Amount = amount });
            return Success(resourceId, amount);
        }

        public InventoryOperationResult TryReleaseReservation(StableId reservationId)
        {
            Reservation reservation;
            if (!reservations.TryGetValue(reservationId, out reservation))
            {
                return Failure(InventoryFailureReason.ReservationNotFound, string.Empty, 0, 0, "Reservation was not found.");
            }

            reservations.Remove(reservationId);
            return Success(reservation.ResourceId, reservation.Amount);
        }

        public InventoryOperationResult TryConsumeReservation(StableId reservationId)
        {
            Reservation reservation;
            if (!reservations.TryGetValue(reservationId, out reservation))
            {
                return Failure(InventoryFailureReason.ReservationNotFound, string.Empty, 0, 0, "Reservation was not found.");
            }

            if (GetAmount(reservation.ResourceId) < reservation.Amount)
            {
                return Failure(InventoryFailureReason.ReservationMismatch, reservation.ResourceId, reservation.Amount, GetAmount(reservation.ResourceId), "Reserved amount is no longer present.");
            }

            RemoveRaw(reservation.ResourceId, reservation.Amount);
            reservations.Remove(reservationId);
            return Success(reservation.ResourceId, reservation.Amount);
        }

        public IReadOnlyList<InventoryStackSnapshot> GetSnapshot()
        {
            List<InventoryStackSnapshot> result = new List<InventoryStackSnapshot>();
            foreach (KeyValuePair<string, int> entry in amounts)
            {
                result.Add(new InventoryStackSnapshot
                {
                    ResourceId = entry.Key,
                    Amount = entry.Value,
                    ReservedAmount = GetReservedAmount(entry.Key)
                });
            }

            result.Sort(delegate(InventoryStackSnapshot left, InventoryStackSnapshot right)
            {
                return string.Compare(left.ResourceId, right.ResourceId, StringComparison.Ordinal);
            });
            return result;
        }

        public void Clear()
        {
            amounts.Clear();
            reservations.Clear();
        }

        public void RestoreSnapshot(IEnumerable<InventoryStackSnapshot> snapshot)
        {
            List<InventoryStackSnapshot> entries = snapshot == null
                ? new List<InventoryStackSnapshot>()
                : new List<InventoryStackSnapshot>(snapshot);
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null || string.IsNullOrWhiteSpace(entries[i].ResourceId) || entries[i].Amount < 0 || entries[i].ReservedAmount < 0 || entries[i].ReservedAmount > entries[i].Amount)
                {
                    throw new ArgumentException("Inventory snapshot contains an invalid entry.", nameof(snapshot));
                }
                total = checked(total + entries[i].Amount);
            }
            if (total > Capacity) throw new InvalidOperationException("Inventory snapshot exceeds capacity.");
            Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Amount > 0) amounts[entries[i].ResourceId] = entries[i].Amount;
                if (entries[i].ReservedAmount > 0)
                {
                    StableId reservationId = new StableId(OwnerId + ".restore." + i);
                    reservations[reservationId] = new Reservation { ResourceId = entries[i].ResourceId, Amount = entries[i].ReservedAmount };
                }
            }
        }

        public InventoryOperationResult TryTransferTo(IInventory target, string resourceId, int amount)
        {
            if (target == null)
            {
                return Failure(InventoryFailureReason.TargetRejected, resourceId, amount, 0, "Target inventory is missing.");
            }

            int available = GetAvailableAmount(resourceId);
            if (available < amount)
            {
                return Failure(InventoryFailureReason.InsufficientAvailable, resourceId, amount, available, "Insufficient unreserved resource.");
            }

            InventoryOperationResult added = target.TryAdd(resourceId, amount);
            if (!added.Succeeded)
            {
                return new InventoryOperationResult(false, InventoryFailureReason.TargetRejected, resourceId, amount, added.Available, added.Message);
            }

            InventoryOperationResult removed = TryRemove(resourceId, amount);
            if (!removed.Succeeded)
            {
                target.TryRemove(resourceId, amount);
                return removed;
            }

            return removed;
        }

        private void RemoveRaw(string resourceId, int amount)
        {
            int remaining = amounts[resourceId] - amount;
            if (remaining == 0)
            {
                amounts.Remove(resourceId);
            }
            else
            {
                amounts[resourceId] = remaining;
            }
        }

        private static InventoryOperationResult Success(string resourceId, int amount)
        {
            return new InventoryOperationResult(true, InventoryFailureReason.None, resourceId, amount, amount, string.Empty);
        }

        private static InventoryOperationResult Failure(InventoryFailureReason reason, string resourceId, int amount, int available, string message)
        {
            return new InventoryOperationResult(false, reason, resourceId, amount, available, message);
        }
    }

    public enum ResourceTransactionFailure
    {
        None,
        InvalidRequirement,
        InsufficientResources,
        ReservationFailed,
        ConsumeFailed
    }

    public readonly struct ResourceTransactionResult
    {
        public ResourceTransactionResult(bool succeeded, ResourceTransactionFailure failure, ResourceAmount[] missing, string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            Missing = missing ?? new ResourceAmount[0];
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public ResourceTransactionFailure Failure { get; }
        public ResourceAmount[] Missing { get; }
        public string Message { get; }
    }

    /// <summary>
    /// Atomically reserves and consumes a set of costs. Any reservation failure releases all prior reservations.
    /// </summary>
    public sealed class ResourceTransaction
    {
        private readonly IInventory inventory;
        private readonly StableId transactionId;
        private readonly ResourceAmount[] requirements;
        private readonly List<StableId> reservationIds = new List<StableId>();
        private bool committed;

        public ResourceTransaction(IInventory inventory, StableId transactionId, IEnumerable<ResourceAmount> requirements)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.transactionId = transactionId;
            this.requirements = Normalize(requirements);
        }

        public bool IsCommitted { get { return committed; } }

        public ResourceTransactionResult TryCommit()
        {
            if (!transactionId.IsValid)
            {
                return new ResourceTransactionResult(false, ResourceTransactionFailure.InvalidRequirement, new ResourceAmount[0], "Transaction ID cannot be empty.");
            }

            if (requirements.Length == 0)
            {
                return new ResourceTransactionResult(true, ResourceTransactionFailure.None, new ResourceAmount[0], string.Empty);
            }

            List<ResourceAmount> missing = new List<ResourceAmount>();
            for (int i = 0; i < requirements.Length; i++)
            {
                ResourceAmount requirement = requirements[i];
                if (string.IsNullOrWhiteSpace(requirement.ResourceId) || requirement.Amount <= 0)
                {
                    return new ResourceTransactionResult(false, ResourceTransactionFailure.InvalidRequirement, new ResourceAmount[0], "A transaction requirement is invalid.");
                }

                int available = inventory.GetAvailableAmount(requirement.ResourceId);
                if (available < requirement.Amount)
                {
                    missing.Add(new ResourceAmount(requirement.ResourceId, requirement.Amount - available));
                }
            }

            if (missing.Count > 0)
            {
                return new ResourceTransactionResult(false, ResourceTransactionFailure.InsufficientResources, missing.ToArray(), "Insufficient resources.");
            }

            reservationIds.Clear();
            for (int i = 0; i < requirements.Length; i++)
            {
                ResourceAmount requirement = requirements[i];
                StableId reservationId = new StableId(transactionId + "." + requirement.ResourceId);
                InventoryOperationResult reserved = inventory.TryReserve(reservationId, requirement.ResourceId, requirement.Amount);
                if (!reserved.Succeeded)
                {
                    ReleaseReservations();
                    return new ResourceTransactionResult(false, ResourceTransactionFailure.ReservationFailed, new[] { requirement }, reserved.Message);
                }

                reservationIds.Add(reservationId);
            }

            for (int i = 0; i < reservationIds.Count; i++)
            {
                InventoryOperationResult consumed = inventory.TryConsumeReservation(reservationIds[i]);
                if (!consumed.Succeeded)
                {
                    ReleaseReservations();
                    return new ResourceTransactionResult(false, ResourceTransactionFailure.ConsumeFailed, new[] { requirements[i] }, consumed.Message);
                }
            }

            reservationIds.Clear();
            committed = true;
            return new ResourceTransactionResult(true, ResourceTransactionFailure.None, new ResourceAmount[0], string.Empty);
        }

        public bool TryRefund()
        {
            if (!committed)
            {
                return false;
            }

            for (int i = 0; i < requirements.Length; i++)
            {
                if (!inventory.TryAdd(requirements[i].ResourceId, requirements[i].Amount).Succeeded)
                {
                    return false;
                }
            }

            committed = false;
            return true;
        }

        private void ReleaseReservations()
        {
            for (int i = 0; i < reservationIds.Count; i++)
            {
                inventory.TryReleaseReservation(reservationIds[i]);
            }

            reservationIds.Clear();
        }

        private static ResourceAmount[] Normalize(IEnumerable<ResourceAmount> source)
        {
            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.Ordinal);
            if (source != null)
            {
                foreach (ResourceAmount amount in source)
                {
                    int current;
                    totals.TryGetValue(amount.ResourceId ?? string.Empty, out current);
                    totals[amount.ResourceId ?? string.Empty] = checked(current + amount.Amount);
                }
            }

            List<ResourceAmount> result = new List<ResourceAmount>();
            foreach (KeyValuePair<string, int> entry in totals)
            {
                result.Add(new ResourceAmount(entry.Key, entry.Value));
            }

            result.Sort(delegate(ResourceAmount left, ResourceAmount right)
            {
                return string.Compare(left.ResourceId, right.ResourceId, StringComparison.Ordinal);
            });
            return result.ToArray();
        }
    }
}
