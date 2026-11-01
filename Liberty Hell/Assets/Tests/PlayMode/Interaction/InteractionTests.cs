using LibertyHell.Interaction;
using NUnit.Framework;
using UnityEngine;

public class InteractionTests
{
    private GameObject cameraObject;
    private GameObject targetObject;
    private PlayerInteraction player;
    private TestInteractionTarget target;

    /// <summary>
    /// Создаёт камеру и тестовый объект перед ней.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Test Camera");
        cameraObject.AddComponent<Camera>();
        player = cameraObject.AddComponent<PlayerInteraction>();
        targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetObject.transform.position = new Vector3(0f, 0f, 2f);
        target = targetObject.AddComponent<TestInteractionTarget>();
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Удаляет объекты, созданные для теста.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(targetObject);
    }

    /// <summary>
    /// Проверяет выбор объекта перед камерой и выполнение его действия.
    /// </summary>
    [Test]
    public void InteractsWithTargetInView()
    {
        player.RefreshTarget();
        Assert.AreSame(target, player.CurrentTarget);
        Assert.IsTrue(player.TryInteract());
        Assert.AreEqual(1, target.InteractionCount);
    }

    /// <summary>
    /// Проверяет, что объект за пределами дистанции не выбирается.
    /// </summary>
    [Test]
    public void IgnoresTargetOutsideRange()
    {
        targetObject.transform.position = new Vector3(0f, 0f, 10f);
        Physics.SyncTransforms();
        player.RefreshTarget();
        Assert.IsNull(player.CurrentTarget);
        Assert.IsFalse(player.TryInteract());
    }

    /// <summary>
    /// Проверяет, что непрозрачное препятствие закрывает объект.
    /// </summary>
    [Test]
    public void ObstacleBlocksInteraction()
    {
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            obstacle.transform.position = new Vector3(0f, 0f, 1f);
            obstacle.transform.localScale = Vector3.one * 0.2f;
            Physics.SyncTransforms();
            player.RefreshTarget();
            Assert.IsNull(player.CurrentTarget);
        }
        finally
        {
            Object.DestroyImmediate(obstacle);
        }
    }

    /// <summary>
    /// Проверяет блокировку обзора и взаимодействия при отключении управления.
    /// </summary>
    [Test]
    public void DisabledInputBlocksLookAndInteraction()
    {
        player.RefreshTarget();
        Quaternion initialRotation = cameraObject.transform.rotation;
        player.SetInputEnabled(false);
        player.GetComponent<PlayerLook>().ApplyLook(new Vector2(100f, 100f));
        player.RefreshTarget();
        Assert.AreEqual(initialRotation, cameraObject.transform.rotation);
        Assert.IsNull(player.CurrentTarget);
        Assert.IsFalse(player.TryInteract());
        player.SetInputEnabled(true);
        player.RefreshTarget();
        Assert.IsTrue(player.TryInteract());
    }

    /// <summary>
    /// Проверяет ограничение вертикального поворота камеры.
    /// </summary>
    [Test]
    public void LookClampsVerticalAngle()
    {
        player.GetComponent<PlayerLook>().ApplyLook(new Vector2(100f, 10000f));
        Assert.AreEqual(-80f, Mathf.DeltaAngle(0f, cameraObject.transform.eulerAngles.x), 0.01f);
        Assert.AreEqual(10f, cameraObject.transform.eulerAngles.y, 0.01f);
    }

    /// <summary>
    /// Проверяет, что отключённый компонент цели нельзя активировать.
    /// </summary>
    [Test]
    public void DisabledTargetCannotBeUsed()
    {
        target.enabled = false;
        player.RefreshTarget();
        Assert.IsNull(player.CurrentTarget);
        Assert.IsFalse(player.TryInteract());
    }
    /// <summary>
    /// Проверяет, что недоступное действие видно под прицелом, но не вызывается.
    /// </summary>
    [Test]
    public void UnavailableTargetDoesNotInteract()
    {
        target.IsAvailable = false;
        player.RefreshTarget();
        Assert.AreSame(target, player.CurrentTarget);
        Assert.AreEqual("Тестовое действие", player.CurrentTarget.ActionName);
        Assert.IsFalse(player.TryInteract());
        Assert.AreEqual(0, target.InteractionCount);
    }

    /// <summary>
    /// Проверяет, что объект, отключённый после наведения, нельзя активировать.
    /// </summary>
    [Test]
    public void TargetDisabledAfterFocusDoesNotInteract()
    {
        player.RefreshTarget();
        target.enabled = false;
        Assert.IsFalse(player.TryInteract());
    }

}
