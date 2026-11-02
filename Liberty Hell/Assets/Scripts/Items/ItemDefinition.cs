using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LibertyHell.Items
{
    public enum ItemId { Lure, Guarantee, BloodPack, Discount, Protection }
    public enum ItemCurrency { Blood, Money }

    public sealed class ItemDefinition
    {
        public ItemId Id { get; }
        public string Name { get; }
        public ItemCurrency Currency { get; }
        public int Price { get; }

        private ItemDefinition(ItemId id, string name, ItemCurrency currency, int price)
        { Id = id; Name = name; Currency = currency; Price = price; }

        public static IReadOnlyList<ItemDefinition> All { get; } = new ReadOnlyCollection<ItemDefinition>(
            new[] { new ItemDefinition(ItemId.Lure, "Приманка", ItemCurrency.Blood, 20),
                new ItemDefinition(ItemId.Guarantee, "Гарантия", ItemCurrency.Blood, 30),
                new ItemDefinition(ItemId.BloodPack, "Пакет крови", ItemCurrency.Money, 25),
                new ItemDefinition(ItemId.Discount, "Скидка", ItemCurrency.Blood, 25),
                new ItemDefinition(ItemId.Protection, "Защита", ItemCurrency.Blood, 25) });

        public static ItemDefinition Find(ItemId id)
        {
            foreach (var item in All) if (item.Id == id) return item;
            return null;
        }
    }
}
