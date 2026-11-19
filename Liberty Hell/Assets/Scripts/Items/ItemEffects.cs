using System;
using System.Collections.Generic;
using LibertyHell.SlotMachine;

namespace LibertyHell.Items
{
    /// <summary>
    /// Структура хранения эффектов предметов для конкретной крутки.
    /// </summary>
    public readonly struct ItemEffects
    {
        public bool HasLure { get; }
        public bool HasDiscount { get; }
        public bool HasProtection { get; }
        public bool GuaranteedXXX { get; }
        
        /// <summary>Задает наличие текущих эффектов предметов.</summary>
        internal ItemEffects(bool lure, bool discount, bool protection, bool guarantee)
        {
            HasLure = lure;
            HasDiscount = discount;
            HasProtection = protection;
            GuaranteedXXX = guarantee;
        }
        
        /// <summary>
        /// Возвращает множитель веса символа из слот-машины с учётом приманки.
        /// При наличии приманки увеличивает вес символов X и Y в 1.5 раза.
        /// </summary>
        public float GetSymbolWeightMultiplier(SlotSymbol symbol)
        {
            bool isAttractedSymbol = symbol == SlotSymbol.X || symbol == SlotSymbol.Y;
            return HasLure && isAttractedSymbol ? 1.5f : 1f;
        }
        
        /// <summary>Рассчитывает вес символа после применения эффекта приманки.</summary>
        public float GetEffectiveWeight(SlotSymbol symbol, float baseWeight)
        {
            if (float.IsNaN(baseWeight) || float.IsInfinity(baseWeight) || baseWeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseWeight));
            }
            return baseWeight * GetSymbolWeightMultiplier(symbol);
        }

        
        /// <summary>
        /// Рассчитывает стоимость ставки после применения скидки, но не ниже единицы.
        /// (округление в большую сторону).
        /// </summary>
        public int GetEffectiveBetCost(int baseCost)
        {
            if (baseCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCost));
            }

            // Умножение выполняется в long, чтобы избежать переполнения int.
            long discountedCost = HasDiscount
                ? ((long)baseCost * 7 + 5) / 10
                : baseCost;

            return (int)Math.Max(1L, discountedCost);
        }

        /// <summary>Сокращает потерю крови при наличии защиты.</summary>
        public int GetEffectiveLosingBloodLoss(int baseLoss)
        {
            if (baseLoss < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseLoss));
            }

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
