using System;
using LibertyHell.Items;

namespace LibertyHell.Shop
{
    /// <summary>Возможные результаты покупки.</summary>
    public enum PurchaseCode
    {
        Success,
        UnknownItem,
        InsufficientBlood,
        InsufficientMoney,
        AlreadyPurchased,
        GuaranteePending,
        BloodFull,
        BetReserveRequired,
        InvalidState
    }

    /// <summary>Содержит код результата покупки, сообщение и признак успеха.</summary>
    public readonly struct PurchaseResult
    {
        public PurchaseCode Code { get; }
        public string Message { get; }
        public bool Success => Code == PurchaseCode.Success;

        /// <summary>Регулирует результат покупки.</summary>
        internal PurchaseResult(PurchaseCode code, string message)
        {
            Code = code;
            Message = message;
        }
    }

/// <summary>Структура для хранения важных параметрров экономики игры.</summary>
    public readonly struct ShopEconomySnapshot
    {
        public long Revision { get; }
        public int Money { get; }
        public int Blood { get; }
        public int MaxBlood { get; }
        public int BaseBetCost { get; }
        public bool CanShop { get; }

        /// <summary>Задает в переменные текущие значения параметров экономики.</summary>
        public ShopEconomySnapshot(long revision, int money, int blood, int maxBlood, int baseBetCost, bool canShop)
        {
            Revision = revision;
            Money = money;
            Blood = blood;
            MaxBlood = maxBlood;
            BaseBetCost = baseBetCost;
            CanShop = canShop;
        }    
    }

    /// <summary>
    /// Проверка правильности данных со снапшота, подтверждение покупки, обновление баланса.
    /// </summary>
    public interface IShopEconomy
    {
        /// <summary>Считывает актуальный файл для экономики.</summary>
        ShopEconomySnapshot Read();


        /// <summary>
        /// Если версия экономики совпадает с expectedRevision, то выдает предмет.
        /// </summary>
        bool TryCommit(long expectedRevision, int money, int blood, Action grant);
    }

    /// <summary>Проверяет условия покупки и передаёт подтверждение покупки в случае успеха.</summary>
    public sealed class ShopService
    {
        private readonly IShopEconomy economy;
        private readonly ItemInventory inventory;
        private bool busy;

        public ItemInventory Inventory => inventory;

        
        /// <summary>Проверка на наличие инвентаря и экономики.</summary>
        public ShopService(IShopEconomy economy, ItemInventory inventory)
        {
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>Проверяет, не проводится покупка уже и есть ли возможность купить именно этот предмет.</summary>
        public PurchaseResult CanPurchase(ItemId id)
        {
            if (busy)
            {
                return Result(PurchaseCode.InvalidState);
            }
            return Check(id, economy.Read());
        }

        /// <summary>Совершает покупку в нужной валюте(кровь или деньги).
        /// Контролирует восстановление крови на максимум 35 единиц.
        /// Применяет новые значения статов к игроку.</summary>
        public PurchaseResult TryPurchase(ItemId id)
        {
            if (busy) return Result(PurchaseCode.InvalidState);
            busy = true;
            try
            {
                ShopEconomySnapshot state = economy.Read();
                PurchaseResult checkResult = Check(id, state);
                if (!checkResult.Success) return checkResult;
                ItemDefinition item = ItemDefinition.Find(id);
                int newMoney = state.Money;
                int newBlood = state.Blood;

                if (item.Currency == ItemCurrency.Blood)
                {
                    newBlood -= item.Price;
                }
                else
                {
                    newMoney -= item.Price;
                    newBlood += Math.Min(35, state.MaxBlood - newBlood);
                }

                bool committed = economy.TryCommit(
                    state.Revision,
                    newMoney,
                    newBlood,
                    () => inventory.Grant(id));

                return committed
                    ? Result(PurchaseCode.Success)
                    : Result(PurchaseCode.InvalidState);
            }
            finally
            {
                // Даже при раннем выходе или исключении магазин снова становится доступен.
                busy = false;
            }
        }
    

        /// <summary>
        /// Проверка всех сценариев покупки предмета.
        /// </summary>
        private PurchaseResult Check(ItemId id, ShopEconomySnapshot state)
        {
            var item = ItemDefinition.Find(id);
            if (item == null) return Result(PurchaseCode.UnknownItem);

            bool invalidEconomyState = !state.CanShop
                || state.Money < 0
                || state.MaxBlood <= 0
                || state.Blood <= 0
                || state.Blood > state.MaxBlood
                || state.BaseBetCost <= 0;

            if (invalidEconomyState)
            {
                return Result(PurchaseCode.InvalidState);
            }
            if (inventory.AlreadyOwned(id)) return Result(PurchaseCode.AlreadyPurchased);
            if (id == ItemId.Guarantee && inventory.HasPendingGuarantee) return Result(PurchaseCode.GuaranteePending);
            if (item.Currency == ItemCurrency.Blood)
                return Result(state.Blood > item.Price ? PurchaseCode.Success : PurchaseCode.InsufficientBlood);
            if (state.Blood == state.MaxBlood) return Result(PurchaseCode.BloodFull);
            if (state.Money < item.Price) return Result(PurchaseCode.InsufficientMoney);
            
            int betCost = inventory.CurrentEffects.GetEffectiveBetCost(state.BaseBetCost);
            if (state.Money - item.Price < betCost)
            {
                return Result(PurchaseCode.BetReserveRequired);
            }

            return Result(PurchaseCode.Success);
        }

        private static PurchaseResult Result(PurchaseCode code)
        {
            switch (code)
            {
                case PurchaseCode.Success: return new PurchaseResult(code, "Покупка доступна.");
                case PurchaseCode.UnknownItem: return new PurchaseResult(code, "Неизвестный предмет.");
                case PurchaseCode.InsufficientBlood: return new PurchaseResult(code, "Недостаточно крови: после покупки должна остаться хотя бы единица.");
                case PurchaseCode.InsufficientMoney: return new PurchaseResult(code, "Недостаточно денег.");
                case PurchaseCode.AlreadyPurchased: return new PurchaseResult(code, "Предмет уже куплен в этом забеге.");
                case PurchaseCode.GuaranteePending: return new PurchaseResult(code, "Гарантия уже ожидает крутку.");
                case PurchaseCode.BloodFull: return new PurchaseResult(code, "Кровь уже восстановлена полностью.");
                case PurchaseCode.BetReserveRequired: return new PurchaseResult(code, "После покупки не хватит денег на ставку.");
                default: return new PurchaseResult(code, "Покупка недоступна в текущем состоянии.");
            }
        }
    }
}
