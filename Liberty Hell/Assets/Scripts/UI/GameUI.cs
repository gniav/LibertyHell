using System;
using System.Text;
using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.Items;
using LibertyHell.SlotMachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LibertyHell.UI
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private RunBootstrap runBootstrap;
        [SerializeField] private PlayerInteraction playerInteraction;

        private RunState run;
        private bool shopOpen;
        private string purchaseMessage = "";
        private string spinMessage = "";
        private Vector2 shopScroll;
        private float uiWidth;
        private float uiHeight;

        public event Action<bool> PauseChanged;
        public event Action RunRestarted;
        public bool IsShopOpen => shopOpen;
        public bool IsModalOpen => shopOpen || (run != null && run.Status != RunStatus.Running);

        /// <summary>
        /// Подключает интерфейс к состоянию забега.
        /// </summary>
        private void Start()
        {
            if (runBootstrap == null || runBootstrap.State == null)
            {
                Debug.LogError("Назначьте Run Bootstrap с настройками забега.", this);
                enabled = false;
                return;
            }

            run = runBootstrap.State;
            run.Changed += OnRunChanged;
            OnRunChanged(run);
        }

        /// <summary>
        /// Открывает или закрывает паузу по клавише Escape.
        /// </summary>
        private void Update()
        {
            if (run == null || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (shopOpen)
                CloseShop();
            else if (run.Status == RunStatus.Paused)
                Resume();
            else if (run.Status == RunStatus.Running)
                Pause();
        }

        /// <summary>
        /// Открывает магазин и отключает управление в мире.
        /// </summary>
        /// <returns></returns>
        public bool OpenShop()
        {
            if (run == null || run.Status != RunStatus.Running || run.IsSpinInProgress)
                return false;

            shopOpen = true;
            purchaseMessage = "";
            UpdateInput();
            return true;
        }

        /// <summary>
        /// Закрывает магазин и возвращает управление в мире.
        /// </summary>
        public void CloseShop()
        {
            shopOpen = false;
            UpdateInput();
        }

        /// <summary>
        /// Приостанавливает забег и сообщает подписчикам о паузе.
        /// </summary>
        public void Pause()
        {
            if (run != null && run.Pause())
                PauseChanged?.Invoke(true);
        }

        /// <summary>
        /// Продолжает забег и сообщает подписчикам о снятии паузы.
        /// </summary>
        public void Resume()
        {
            if (run != null && run.Resume())
                PauseChanged?.Invoke(false);
        }

        /// <summary>
        /// Отображает результат завершённого вращения.
        /// </summary>
        /// <param name="result"></param>
        public void ShowSpinResult(AppliedSpinResult appliedResult)
        {
            SlotResult result = appliedResult.Result;
            spinMessage = $"{DescribeSymbol(result.First)} | {DescribeSymbol(result.Second)} | {DescribeSymbol(result.Third)}: +{result.MoneyPayout}, кровь -{appliedResult.BloodLost}";
        }

        private static string DescribeSymbol(SlotSymbol symbol)
        {
            switch (symbol)
            {
                case SlotSymbol.A: return "Вишня";
                case SlotSymbol.B: return "Лимон";
                case SlotSymbol.X: return "Колокольчик";
                case SlotSymbol.Y: return "Фиолетовая вишня";
                case SlotSymbol.H: return "Красный лимон";
                default: return symbol.ToString();
            }
        }

        /// <summary>
        /// Отрисовывает показатели и открытое окно.
        /// </summary>
        private void OnGUI()
        {
            if (run == null)
                return;

            float scale = Mathf.Clamp(Screen.height / 900f, 1f, 3f);
            uiWidth = Screen.width / scale;
            uiHeight = Screen.height / scale;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            try
            {
                if (!IsModalOpen)
                    DrawHud();

                if (shopOpen)
                    DrawShop();
                else if (run.Status == RunStatus.Paused)
                    DrawPause();
                else if (run.Status == RunStatus.Won || run.Status == RunStatus.Lost)
                    DrawResult();
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        /// <summary>
        /// Показывает показатели и подсказку взаимодействия.
        /// </summary>
        private void DrawHud()
        {
            var effects = run.Inventory.CurrentEffects;
            int stake = effects.GetEffectiveBetCost(SlotSpinController.Stake);
            float hudWidth = Mathf.Min(450, uiWidth - 40);
            var messageStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            float messageHeight = messageStyle.CalcHeight(new GUIContent(spinMessage), hudWidth - 20);
            GUILayout.BeginArea(new Rect(20, 20, hudWidth, 105 + Mathf.Max(20, messageHeight)), GUI.skin.box);
            GUILayout.Label($"Деньги: {run.Money} / {run.Parameters.MoneyGoal}   Кровь: {run.Blood} / {run.Parameters.MaxBlood}");
            int seconds = Mathf.CeilToInt(run.RemainingSeconds);
            GUILayout.Label($"Время: {seconds / 60:00}:{seconds % 60:00}   Ставка: {stake}");
            GUILayout.Label("Активные предметы: " + ActiveItems(effects));
            GUILayout.Label(spinMessage, messageStyle);
            GUILayout.EndArea();

            GUI.Label(new Rect(uiWidth / 2f - 5, uiHeight / 2f - 15, 20, 30), "+");
            if (playerInteraction != null && playerInteraction.CurrentTarget != null)
            {
                var target = playerInteraction.CurrentTarget;
                string label = target.IsAvailable ? "E — " + target.ActionName : target.ActionName + " (недоступно)";
                GUI.Label(new Rect(uiWidth / 2f - 100, uiHeight / 2f + 15, 300, 30), label);
            }

        }

        /// <summary>
        /// Показывает товары и причины их недоступности из правил магазина.
        /// </summary>
        private void DrawShop()
        {
            float width = Mathf.Min(520, uiWidth - 40);
            float height = Mathf.Min(520, uiHeight - 40);
            float x = (uiWidth - width) / 2f;
            float y = (uiHeight - height) / 2f;
            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("Магазин");
            shopScroll = GUILayout.BeginScrollView(shopScroll);
            foreach (ItemDefinition item in ItemDefinition.All)
            {
                string currency = item.Currency == ItemCurrency.Blood ? "крови" : "денег";
                GUILayout.Label($"{item.Name}: {item.Price} {currency} — {Description(item.Id)}");
                var check = run.Shop.CanPurchase(item.Id);
                GUI.enabled = check.Success;
                if (GUILayout.Button("Купить"))
                    purchaseMessage = run.Shop.TryPurchase(item.Id).Message;
                GUI.enabled = true;
                if (!check.Success)
                    GUILayout.Label(check.Message);
            }
            GUILayout.EndScrollView();
            GUILayout.Label(purchaseMessage);
            if (GUILayout.Button("Закрыть"))
                CloseShop();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Показывает меню паузы.
        /// </summary>
        private void DrawPause()
        {
            GUILayout.BeginArea(new Rect(uiWidth / 2f - 150, uiHeight / 2f - 100, 300, 200), GUI.skin.box);
            GUILayout.Label("Пауза");
            if (GUILayout.Button("Продолжить")) Resume();
            if (GUILayout.Button("Начать заново")) Restart();
            if (GUILayout.Button("Выход")) Application.Quit();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Показывает итог забега и доступные действия.
        /// </summary>
        private void DrawResult()
        {
            GUILayout.BeginArea(new Rect(uiWidth / 2f - 180, uiHeight / 2f - 100, 360, 200), GUI.skin.box);
            GUILayout.Label(run.Status == RunStatus.Won ? "Победа" : "Поражение");
            if (!string.IsNullOrEmpty(spinMessage))
                GUILayout.Label(spinMessage, new GUIStyle(GUI.skin.label) { wordWrap = true });
            if (run.Status == RunStatus.Lost)
                GUILayout.Label(LossReason());
            if (GUILayout.Button("Начать заново")) Restart();
            if (GUILayout.Button("Выход")) Application.Quit();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Определяет сообщение по доступным данным завершённого забега.
        /// </summary>
        /// <returns></returns>
        private string LossReason()
        {
            if (run.Blood <= 0) return "Закончилась кровь";
            if (run.RemainingSeconds <= 0) return "Закончилось время";
            return "Недостаточно денег для продолжения";
        }

        /// <summary>
        /// Возвращает список активных эффектов забега.
        /// </summary>
        /// <param name="effects"></param>
        /// <returns></returns>
        private string ActiveItems(ItemEffects effects)
        {
            var names = new StringBuilder();
            if (effects.HasLure) names.Append("Приманка ");
            if (effects.HasDiscount) names.Append("Скидка ");
            if (effects.HasProtection) names.Append("Защита ");
            if (effects.GuaranteedXXX) names.Append("Гарантия ");
            return names.Length == 0 ? "нет" : names.ToString();
        }

        /// <summary>
        /// Возвращает краткое описание товара.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string Description(ItemId item)
        {
            switch (item)
            {
                case ItemId.Lure: return "X и Y выпадают чаще";
                case ItemId.Guarantee: return "следующая крутка: XXX";
                case ItemId.BloodPack: return "восстанавливает до 35 крови";
                case ItemId.Discount: return "ставка ниже на 30%";
                case ItemId.Protection: return "потеря крови от крутки ниже на 50%";
                default: return "";
            }
        }

        /// <summary>
        /// Перезапускает состояние забега и закрывает окна.
        /// </summary>
        private void Restart()
        {
            shopOpen = false;
            spinMessage = "";
            purchaseMessage = "";
            runBootstrap.ResetRun();
            RunRestarted?.Invoke();
            PauseChanged?.Invoke(false);
            UpdateInput();
        }

        /// <summary>
        /// Обновляет окна и ввод при изменении забега.
        /// </summary>
        /// <param name="state"></param>
        private void OnRunChanged(RunState state)
        {
            if (state.Status != RunStatus.Running)
                shopOpen = false;
            UpdateInput();
        }

        /// <summary>
        /// Включает управление миром только во время активного забега без окна магазина.
        /// </summary>
        private void UpdateInput()
        {
            if (playerInteraction != null && run != null)
                playerInteraction.SetInputEnabled(run.Status == RunStatus.Running && !shopOpen);
        }

        /// <summary>
        /// Отписывается от изменений состояния.
        /// </summary>
        private void OnDestroy()
        {
            if (run != null)
                run.Changed -= OnRunChanged;
        }
    }
}
