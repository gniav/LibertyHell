using LibertyHell.Interaction;
using LibertyHell.GameLoop;
using LibertyHell.UI;
using UnityEngine;

namespace LibertyHell.SceneIntegration
{
    public sealed class ShopSceneInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameUI gameUI;
        [SerializeField] private RunBootstrap runBootstrap;
        public string ActionName => "Открыть магазин";
        public bool IsAvailable => isActiveAndEnabled && gameUI != null && !gameUI.IsModalOpen &&
            runBootstrap != null && runBootstrap.State != null &&
            runBootstrap.State.Status == RunStatus.Running && !runBootstrap.State.IsSpinInProgress;
        public bool Interact() => IsAvailable && gameUI.OpenShop();
    }
}
