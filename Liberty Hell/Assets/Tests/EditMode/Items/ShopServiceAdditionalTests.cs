
using System;
using LibertyHell.Items;
using LibertyHell.Shop;
using NUnit.Framework;

public class ShopServiceAdditionalTests
{
    private sealed class Economy : IShopEconomy
    {
        public int Money = 100;
        public int Blood = 100;
        public int MaxBlood = 200;
        public int Bet = 10;
        public bool Available = true;
        public long Revision;

        public ShopEconomySnapshot Read()
        {
            return new ShopEconomySnapshot(
                Revision,
                Money,
                Blood,
                MaxBlood,
                Bet,
                Available
            );
        }

        public bool TryCommit(long revision, int money, int blood, Action grant)
        {
            if (revision != Revision)
                return false;

            Money = money;
            Blood = blood;
            Revision++;
            grant();

            return true;
        }
    }

    private sealed class ReentrantEconomy : IShopEconomy
    {
        private readonly Action onCommit;

        public int Money = 100;
        public int Blood = 100;
        public long Revision;

        public ReentrantEconomy(Action onCommit)
        {
            this.onCommit = onCommit;
        }

        public ShopEconomySnapshot Read()
        {
            return new ShopEconomySnapshot(
                Revision,
                Money,
                Blood,
                200,
                10,
                true
            );
        }

        public bool TryCommit(long revision, int money, int blood, Action grant)
        {
            if (revision != Revision)
                return false;

            onCommit();

            Money = money;
            Blood = blood;
            Revision++;
            grant();

            return true;
        }
    }

    private Economy economy;
    private ItemInventory inventory;
    private ShopService shop;

    [SetUp]
    public void SetUp()
    {
        economy = new Economy();
        inventory = new ItemInventory();
        shop = new ShopService(economy, inventory);
    }

    [Test]
    public void CanPurchaseDoesNotChangeEconomyOrInventory()
    {
        PurchaseResult result = shop.CanPurchase(ItemId.Lure);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(100, economy.Money);
        Assert.AreEqual(100, economy.Blood);
        Assert.AreEqual(0, economy.Revision);
        Assert.IsFalse(inventory.CurrentEffects.HasLure);
    }

    [Test]
    public void InvalidEconomyStateRejectsPurchase()
    {
        economy.Money = -1;
        Assert.AreEqual(
            PurchaseCode.InvalidState,
            shop.TryPurchase(ItemId.Lure).Code
        );

        economy.Money = 100;
        economy.MaxBlood = 0;
        Assert.AreEqual(
            PurchaseCode.InvalidState,
            shop.TryPurchase(ItemId.Lure).Code
        );

        economy.MaxBlood = 200;
        economy.Blood = 0;
        Assert.AreEqual(
            PurchaseCode.InvalidState,
            shop.TryPurchase(ItemId.Lure).Code
        );

        economy.Blood = 201;
        Assert.AreEqual(
            PurchaseCode.InvalidState,
            shop.TryPurchase(ItemId.Lure).Code
        );

        economy.Blood = 100;
        economy.Bet = 0;
        Assert.AreEqual(
            PurchaseCode.InvalidState,
            shop.TryPurchase(ItemId.Lure).Code
        );

        Assert.AreEqual(100, economy.Money);
        Assert.AreEqual(100, economy.Blood);
        Assert.AreEqual(0, economy.Revision);
        Assert.IsFalse(inventory.CurrentEffects.HasLure);
    }

    [Test]
    public void BusyPreventsReentrantPurchase()
    {
        ShopService reentrantShop = null;
        PurchaseCode nestedResult = PurchaseCode.Success;

        var reentrantEconomy = new ReentrantEconomy(() =>
        {
            nestedResult = reentrantShop.TryPurchase(ItemId.Lure).Code;
        });

        reentrantShop = new ShopService(reentrantEconomy, inventory);

        Assert.IsTrue(reentrantShop.TryPurchase(ItemId.Discount).Success);

        Assert.AreEqual(
            PurchaseCode.InvalidState,
            nestedResult
        );

        Assert.AreEqual(1, reentrantEconomy.Revision);
        Assert.IsTrue(inventory.CurrentEffects.HasDiscount);
        Assert.IsFalse(inventory.CurrentEffects.HasLure);
    }
}
