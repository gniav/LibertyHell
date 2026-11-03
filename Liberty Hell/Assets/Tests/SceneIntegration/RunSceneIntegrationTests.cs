using System.Collections;
using System.Reflection;
using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.Items;
using LibertyHell.SceneIntegration;
using LibertyHell.SlotMachine;
using LibertyHell.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class RunSceneIntegrationTests
{
    [Test]
    public void FirstEnabledBuildSceneIsRunFoundation()
    {
        var first = System.Array.Find(EditorBuildSettings.scenes, scene => scene.enabled);
        Assert.IsNotNull(first);
        Assert.AreEqual("Assets/Scenes/RunFoundation.unity", first.path);
        Assert.AreEqual(AssetDatabase.AssetPathToGUID(first.path), first.guid.ToString());
    }

    [UnityTest]
    public IEnumerator SceneConnectsSpinPauseRestartAndShop()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        yield return null;
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        var player = Object.FindFirstObjectByType<PlayerInteraction>();
        var slot = Object.FindFirstObjectByType<SlotSceneInteraction>();
        var spin = Object.FindFirstObjectByType<SlotSpinBehaviour>();
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        var shop = Object.FindFirstObjectByType<ShopSceneInteraction>();
        var ui = Object.FindFirstObjectByType<GameUI>();
        Assert.IsNotNull(slot); Assert.IsNotNull(spin); Assert.IsNotNull(view);
        Assert.IsNotNull(shop); Assert.IsNotNull(ui); Assert.IsNotNull(player);
        // Batch mode has no focused game window; simulate focus for input checks.
        typeof(PlayerInteraction).GetMethod("OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(player, new object[] { true });
        Physics.SyncTransforms();
        player.RefreshTarget();
        Assert.AreSame(slot, player.CurrentTarget);
        Assert.IsTrue(player.TryInteract());
        Assert.AreEqual(90, run.Money);
        Assert.IsTrue(run.IsSpinInProgress);
        Assert.IsFalse(shop.IsAvailable);
        Assert.IsTrue(view.IsSpinning);
        var result = spin.PendingResult.Value;
        ui.Pause();
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(player.InputEnabled);
        float time = run.RemainingSeconds;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(time, run.RemainingSeconds);
        Assert.IsTrue(view.IsSpinning);
        Assert.IsFalse(player.TryInteract());
        ui.Resume();
        Assert.AreEqual(1f, Time.timeScale);
        float deadline = Time.realtimeSinceStartup + 10;
        while (view.IsSpinning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(view.IsSpinning);
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.AreEqual(result.First.ToString(), view.transform.Find("Reel 1").GetComponent<TextMesh>().text);
        Assert.AreEqual(result.Second.ToString(), view.transform.Find("Reel 2").GetComponent<TextMesh>().text);
        Assert.AreEqual(result.Third.ToString(), view.transform.Find("Reel 3").GetComponent<TextMesh>().text);
        Assert.IsTrue(player.InputEnabled);
        int balance = run.Money;
        view.enabled = false;
        Assert.IsFalse(slot.IsAvailable);
        Assert.IsFalse(slot.Interact());
        Assert.AreEqual(balance, run.Money);
        view.enabled = true;
        Assert.IsTrue(slot.Interact());
        ui.Pause();
        typeof(GameUI).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(view.IsSpinning);
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.AreEqual(100, run.Money);
        Assert.AreEqual(100, run.Blood);
        Assert.IsTrue(player.InputEnabled);
        typeof(GameUI).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
        Assert.AreEqual(100, run.Money);
        player.transform.rotation = Quaternion.LookRotation(shop.transform.position - player.transform.position);
        Physics.SyncTransforms();
        player.RefreshTarget();
        Assert.AreSame(shop, player.CurrentTarget);
        Assert.IsTrue(player.TryInteract());
        Assert.IsTrue(ui.IsShopOpen);
        Assert.IsFalse(player.InputEnabled);
        Assert.IsFalse(player.TryInteract());
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Discount).Success);
        Assert.AreEqual(75, run.Blood);
        ui.CloseShop();
        Assert.IsTrue(player.InputEnabled);
        Assert.IsTrue(run.Inventory.CurrentEffects.HasDiscount);
        player.transform.rotation = Quaternion.Euler(-80, 0, 0);
        player.RefreshTarget();
        Assert.IsNull(player.CurrentTarget);
        Assert.IsFalse(player.TryInteract());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator FailedPresentationStillCompletesThePaidSpin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        yield return null;
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        var slot = Object.FindFirstObjectByType<SlotSceneInteraction>();
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        typeof(SlotPresentation).GetField("firstReel", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(view, null);
        LogAssert.Expect(LogType.Warning, "Slot animation could not start; applying the paid result immediately.");
        Assert.IsTrue(slot.Interact());
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.IsFalse(view.IsSpinning);
        yield return new ExitPlayMode();
    }
}
