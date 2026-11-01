using System.Collections;
using LibertyHell.GameLoop;
using LibertyHell.SlotMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class SlotSpinBehaviourTests
{
    [UnityTest]
    public IEnumerator ComponentConnectsSettingsAndRunWithoutAnimation()
    {
        yield return new EnterPlayMode();
        var gameObject = new GameObject("Slot integration test");
        gameObject.SetActive(false);
        var runSettings = ScriptableObject.CreateInstance<RunSettings>();
        var slotSettings = ScriptableObject.CreateInstance<SlotSettings>();
        var bootstrap = gameObject.AddComponent<RunBootstrap>();
        var slot = gameObject.AddComponent<SlotSpinBehaviour>();
        var runSerialized = new SerializedObject(bootstrap);
        runSerialized.FindProperty("settings").objectReferenceValue = runSettings;
        runSerialized.ApplyModifiedPropertiesWithoutUndo();
        var slotSerialized = new SerializedObject(slot);
        slotSerialized.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
        slotSerialized.FindProperty("settings").objectReferenceValue = slotSettings;
        slotSerialized.ApplyModifiedPropertiesWithoutUndo();
        gameObject.SetActive(true);
        yield return null;

        Assert.IsTrue(slot.CanInteract);
        Assert.IsTrue(slot.TryInteract(out long spinId));
        Assert.AreEqual(90, bootstrap.State.Money);
        Assert.IsTrue(slot.PendingResult.HasValue);
        Assert.IsTrue(slot.TryCompleteSpin(spinId));
        Assert.IsFalse(slot.TryCompleteSpin(spinId));
        Object.Destroy(gameObject);
        Object.Destroy(runSettings);
        Object.Destroy(slotSettings);
        yield return new ExitPlayMode();
    }
}
