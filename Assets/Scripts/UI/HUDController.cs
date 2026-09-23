using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ushimitsu.UI
{
    public class HUDController : MonoBehaviour
    {
        public enum LineStyle { Normal, Flavor, Clue }

        public Text promptText;
        public GameObject messagePanel;
        public Text messageText;
        public float messageHoldSeconds = 4.5f;
        public Text clockText;
        public Text inventoryText;
        public Color normalColor = new Color(0.93f, 0.89f, 0.83f);
        public Color flavorColor = new Color(0.72f, 0.67f, 0.60f);
        public Color clueColor = new Color(0.86f, 0.71f, 0.38f);
        public Color criticalColor = new Color(0.76f, 0.27f, 0.23f);

        public DialPanelController dialPanel;

        public GameObject endingPanel;
        public Text endingTitleText;
        public Text endingBodyText;
        public Button restartButton;

        readonly List<string> inventoryItems = new List<string>();
        Coroutine hideRoutine;

        void Awake()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            if (messagePanel != null) messagePanel.SetActive(false);
            if (inventoryText != null) inventoryText.text = "持ち物はまだない";

            // Listeners added from an editor script are not serialized, so wire here.
            if (restartButton != null) restartButton.onClick.AddListener(RestartPressed);
        }

        void Update()
        {
            if (endingPanel == null || !endingPanel.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))
            {
                RestartPressed();
            }
        }

        public void SetPrompt(string text)
        {
            if (promptText != null) promptText.text = text;
        }

        public void SetGameplayHudVisible(bool visible)
        {
            if (promptText != null) promptText.gameObject.SetActive(visible);
            if (clockText != null) clockText.gameObject.SetActive(visible);
            if (inventoryText != null) inventoryText.gameObject.SetActive(visible);
        }

        public void Log(string line, LineStyle style)
        {
            if (messagePanel != null) messagePanel.SetActive(true);

            if (messageText != null)
            {
                messageText.text = line;
                messageText.color = style == LineStyle.Flavor ? flavorColor :
                    style == LineStyle.Clue ? clueColor : normalColor;
            }

            // The panel starts inactive, and Unity's layout system does not process
            // ContentSizeFitter/LayoutGroup children while a hierarchy is inactive,
            // so force an immediate rebuild now that it's on and has fresh text.
            if (messagePanel != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(messagePanel.GetComponent<RectTransform>());
            }

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideMessageAfterDelay());
        }

        IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageHoldSeconds);
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        public bool IsMessageShowing()
        {
            return messagePanel != null && messagePanel.activeSelf;
        }

        public void DismissMessage()
        {
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        public void AddInventoryItem(string label)
        {
            if (inventoryItems.Contains(label)) return;
            inventoryItems.Add(label);
            if (inventoryText != null)
            {
                inventoryText.text = "持ち物: " + string.Join("　", inventoryItems);
            }
        }

        public void SetClockText(string text, bool critical)
        {
            if (clockText == null) return;
            clockText.text = text;
            clockText.color = critical ? criticalColor : normalColor;
        }

        public void OpenDialPanel(Game.DoorLock doorLock)
        {
            DismissMessage();
            dialPanel?.Open(doorLock);
        }

        public void CloseDialPanel()
        {
            dialPanel?.Close();
        }

        public void ShakeDial()
        {
            dialPanel?.Shake();
        }

        public void ShowEnding(string title, string body)
        {
            if (endingPanel != null) endingPanel.SetActive(true);
            if (endingTitleText != null) endingTitleText.text = title;
            if (endingBodyText != null) endingBodyText.text = body;
        }

        public void RestartPressed()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
