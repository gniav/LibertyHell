using System.Collections;
using LibertyHell.GameLoop;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class RunFoundationSceneTests
{
    [UnityTest]
    public IEnumerator SceneInitializesAndCanResetInPlayMode()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        var bootstrap = Object.FindFirstObjectByType<RunBootstrap>();
        Assert.IsNotNull(bootstrap);
        Assert.IsNotNull(bootstrap.State);
        Assert.AreEqual(100, bootstrap.State.Money);
        bootstrap.ResetRun();
        Assert.AreEqual(5, bootstrap.State.Blood);
        yield return new ExitPlayMode();
    }
}
