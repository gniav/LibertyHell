using System.Collections;
using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.Items;
using LibertyHell.SlotMachine;
using LibertyHell.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class GameUITests
{
    [UnityTest]
    public IEnumerator SpinMessageUsesAppliedBloodLoss()
    {
        EditorSceneManager.OpenScene("Assets/Tests/UI/GameUIDemo.unity");
        yield return new EnterPlayMode();
        yield return null;
        var ui = Object.FindFirstObjectByType<GameUI>();
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Protection).Success);
        using (var spin = new SlotSpinController(run, () =>
            new SlotResult(SlotSymbol.H, SlotSymbol.H, SlotSymbol.H, 0, 20)))
        {
            Assert.IsTrue(spin.TryInteract(out long id));
            int before = run.Blood;
            Assert.IsTrue(spin.TryCompleteSpin(id, out var applied));
            ui.ShowSpinResult(applied);
            var message = typeof(GameUI).GetField("spinMessage", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(ui) as string;
            Assert.AreEqual(10, before - run.Blood);
            StringAssert.Contains("кровь -10", message);
            StringAssert.Contains("Красный лимон | Красный лимон | Красный лимон", message);
            ui.ShowSpinResult(new AppliedSpinResult(new SlotResult(SlotSymbol.A, SlotSymbol.A, SlotSymbol.Y, 15, 0), 0));
            message = typeof(GameUI).GetField("spinMessage", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(ui) as string;
            StringAssert.Contains("Вишня | Вишня | Фиолетовая вишня", message);
        }
        yield return new ExitPlayMode();
    }

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
