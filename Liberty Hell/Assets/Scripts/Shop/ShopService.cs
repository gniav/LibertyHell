using System;
using LibertyHell.Items;

namespace LibertyHell.Shop
{
    public enum PurchaseCode { Success, UnknownItem, InsufficientBlood, InsufficientMoney,
        AlreadyPurchased, GuaranteePending, BloodFull, BetReserveRequired, InvalidState }

    public readonly struct PurchaseResult
    {
        public PurchaseCode Code { get; }
        public string Message { get; }
        public bool Success => Code == PurchaseCode.Success;
        internal PurchaseResult(PurchaseCode code, string message) { Code = code; Message = message; }
    }

    public readonly struct ShopEconomySnapshot
    {
        public long Revision { get; }
        public int Money { get; }
        public int Blood { get; }
        public int MaxBlood { get; }
        public int BaseBetCost { get; }
        public bool CanShop { get; }
        public ShopEconomySnapshot(long revision, int money, int blood, int maxBlood, int baseBetCost, bool canShop)
        { Revision = revision; Money = money; Blood = blood; MaxBlood = maxBlood; BaseBetCost = baseBetCost; CanShop = canShop; }
    }

    // Implemented by the authoritative run system, on its serialized game thread.
    // TryCommit must compare revision and atomically publish both balances and grant:
    // invoke grant exactly once on success, before notifications; never invoke it on failure.
    // No reentrant mutation or observer may see a partially committed purchase.
    public interface IShopEconomy
    {
        ShopEconomySnapshot Read();
        bool TryCommit(long expectedRevision, int money, int blood, Action grant);
    }

    public sealed class ShopService
    {
        private readonly IShopEconomy economy;
        private readonly ItemInventory inventory;
        private bool busy;
        public ItemInventory Inventory => inventory;
        public ShopService(IShopEconomy economy, ItemInventory inventory)
        {
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public PurchaseResult CanPurchase(ItemId id) => busy ? Result(PurchaseCode.InvalidState) : Check(id, economy.Read());

        public PurchaseResult TryPurchase(ItemId id)
        {
            if (busy) return Result(PurchaseCode.InvalidState);
            busy = true;
            try
            {
                var state = economy.Read();
                var check = Check(id, state);
                if (!check.Success) return check;
                var item = ItemDefinition.Find(id);
                int money = state.Money, blood = state.Blood;
                if (item.Currency == ItemCurrency.Blood) blood -= item.Price;
                else { money -= item.Price; blood += Math.Min(35, state.MaxBlood - blood); }
                return economy.TryCommit(state.Revision, money, blood, () => inventory.Grant(id))
                    ? Result(PurchaseCode.Success) : Result(PurchaseCode.InvalidState);
            }
            finally { busy = false; }
        }

        private PurchaseResult Check(ItemId id, ShopEconomySnapshot state)
        {
            var item = ItemDefinition.Find(id);
            if (item == null) return Result(PurchaseCode.UnknownItem);
            if (!state.CanShop || state.Money < 0 || state.MaxBlood <= 0 || state.Blood <= 0 ||
                state.Blood > state.MaxBlood || state.BaseBetCost <= 0)
                return Result(PurchaseCode.InvalidState);
            if (inventory.AlreadyOwned(id)) return Result(PurchaseCode.AlreadyPurchased);
            if (id == ItemId.Guarantee && inventory.HasPendingGuarantee) return Result(PurchaseCode.GuaranteePending);
            if (item.Currency == ItemCurrency.Blood)
                return Result(state.Blood > item.Price ? PurchaseCode.Success : PurchaseCode.InsufficientBlood);
            if (state.Blood == state.MaxBlood) return Result(PurchaseCode.BloodFull);
            if (state.Money < item.Price) return Result(PurchaseCode.InsufficientMoney);
            if (state.Money - item.Price < inventory.CurrentEffects.GetEffectiveBetCost(state.BaseBetCost))
                return Result(PurchaseCode.BetReserveRequired);
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
