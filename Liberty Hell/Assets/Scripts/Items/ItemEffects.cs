using System;
using System.Collections.Generic;
using LibertyHell.SlotMachine;

namespace LibertyHell.Items
{
    // Immutable spin-specific values. Blood-loss fractions round down.
    public readonly struct ItemEffects
    {
        public bool HasLure { get; }
        public bool HasDiscount { get; }
        public bool HasProtection { get; }
        public bool GuaranteedXXX { get; }
        internal ItemEffects(bool lure, bool discount, bool protection, bool guarantee)
        { HasLure = lure; HasDiscount = discount; HasProtection = protection; GuaranteedXXX = guarantee; }

        public float GetSymbolWeightMultiplier(SlotSymbol symbol) =>
            HasLure && (symbol == SlotSymbol.X || symbol == SlotSymbol.Y) ? 1.5f : 1f;
        public float GetEffectiveWeight(SlotSymbol symbol, float baseWeight)
        {
            if (float.IsNaN(baseWeight) || float.IsInfinity(baseWeight) || baseWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(baseWeight));
            return baseWeight * GetSymbolWeightMultiplier(symbol);
        }
        public int GetEffectiveBetCost(int baseCost)
        {
            if (baseCost < 0) throw new ArgumentOutOfRangeException(nameof(baseCost));
            long rounded = HasDiscount ? ((long)baseCost * 7 + 5) / 10 : baseCost;
            return (int)Math.Max(1L, rounded);
        }
        public int GetEffectiveLosingBloodLoss(int baseLoss)
        {
            if (baseLoss < 0) throw new ArgumentOutOfRangeException(nameof(baseLoss));
            return HasProtection ? baseLoss / 2 : baseLoss;
        }
    }

    public sealed class ItemInventory
    {
        private bool lure, discount, protection, guarantee;
        private readonly Dictionary<Guid, ItemEffects> spins = new Dictionary<Guid, ItemEffects>();
        public bool HasPendingGuarantee => guarantee;
        public ItemEffects CurrentEffects => new ItemEffects(lure, discount, protection, guarantee);

        internal bool AlreadyOwned(ItemId id) => id == ItemId.Lure ? lure :
            id == ItemId.Discount ? discount : id == ItemId.Protection && protection;
        internal void Grant(ItemId id)
        {
            switch (id)
            {
                case ItemId.Lure: lure = true; break;
                case ItemId.Guarantee: guarantee = true; break;
                case ItemId.Discount: discount = true; break;
                case ItemId.Protection: protection = true; break;
            }
        }

        // Call only after the authoritative system has accepted this exact spin.
        // Repeated confirmation returns the original snapshot without consuming another guarantee.
        public ItemEffects ConfirmSpinStarted(Guid spinId)
        {
            if (spinId == Guid.Empty) throw new ArgumentException("A unique spin ID is required.", nameof(spinId));
            if (spins.TryGetValue(spinId, out var existing)) return existing;
            var snapshot = CurrentEffects;
            spins.Add(spinId, snapshot);
            guarantee = false;
            return snapshot;
        }
        public void ResetForNewRun()
        {
            lure = discount = protection = guarantee = false;
            spins.Clear();
        }
    }
}
