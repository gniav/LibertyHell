using System.Collections;
using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.Items;
using LibertyHell.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class GameUITests
{
    /// <summary>
    /// Проверяет открытие магазина и переходы в паузу на тестовой сцене.
    /// </summary>
    /// <returns></returns>
    [UnityTest]
    public IEnumerator ShopAndPauseFollowRunState()
    {
        EditorSceneManager.OpenScene("Assets/Tests/UI/GameUIDemo.unity");
        yield return new EnterPlayMode();
        yield return null;

        var ui = Object.FindFirstObjectByType<GameUI>();
        var bootstrap = Object.FindFirstObjectByType<RunBootstrap>();
        var player = Object.FindFirstObjectByType<PlayerInteraction>();
        Assert.IsNotNull(player);
        Assert.IsNotNull(ui);
        Assert.IsNotNull(bootstrap);
        Assert.IsTrue(ui.OpenShop());
        Assert.IsTrue(ui.IsShopOpen);
        Assert.IsFalse(player.InputEnabled);
        ui.CloseShop();
        Assert.IsFalse(ui.IsShopOpen);
        Assert.IsTrue(player.InputEnabled);
        ui.Pause();
        Assert.AreEqual(RunStatus.Paused, bootstrap.State.Status);
        Assert.IsFalse(player.InputEnabled);
        Assert.IsFalse(ui.OpenShop());
        ui.Resume();
        Assert.AreEqual(RunStatus.Running, bootstrap.State.Status);
        Assert.IsTrue(ui.OpenShop());
        var purchase = bootstrap.State.Shop.TryPurchase(ItemId.Discount);
        Assert.IsTrue(purchase.Success);
        Assert.IsTrue(bootstrap.State.Inventory.CurrentEffects.HasDiscount);
        ui.CloseShop();
        Assert.IsTrue(bootstrap.State.TryAddMoney(1000));
        Assert.AreEqual(RunStatus.Won, bootstrap.State.Status);
        Assert.IsFalse(player.InputEnabled);
        Assert.IsFalse(ui.OpenShop());
        yield return new ExitPlayMode();
    }
}
