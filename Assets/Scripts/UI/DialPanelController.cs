using UnityEngine;
using UnityEngine.UI;
using Ushimitsu.Game;

namespace Ushimitsu.UI
{
    public class DialPanelController : MonoBehaviour
    {
        public GameObject panelRoot;
        public Text[] digitTexts = new Text[3];
        public RectTransform shakeTarget;
        public Text hintText;
        public Button[] upButtons = new Button[3];
        public Button[] downButtons = new Button[3];
        public Button tryButton;
        public Button closeButton;

        public Color selectedDigitColor = new Color(0.86f, 0.71f, 0.38f);
        public Color idleDigitColor = new Color(0.93f, 0.89f, 0.83f);

        // Raised from Open()/Close() themselves, so every way of closing the
        // panel (戻る button, Esc, a correct code) is seen by listeners - not just
        // the ones that happen to go through HUDController.
        public event System.Action<bool> OpenChanged;

        readonly int[] digits = new int[3];
        DoorLock activeLock;
        float shakeTimer;
        Vector2 shakeBasePos;
        int selectedIndex;

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            // Wired here at runtime: listeners added from an editor script are not
            // serialized into the scene, so the buttons would otherwise do nothing.
            for (int i = 0; i < digits.Length; i++)
            {
                int idx = i;
                if (upButtons != null && idx < upButtons.Length && upButtons[idx] != null)
                {
                    upButtons[idx].onClick.AddListener(() => Increment(idx));
                }
                if (downButtons != null && idx < downButtons.Length && downButtons[idx] != null)
                {
                    downButtons[idx].onClick.AddListener(() => Decrement(idx));
                }
            }
            if (tryButton != null) tryButton.onClick.AddListener(SubmitPressed);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePressed);
        }

        public void Open(DoorLock doorLock)
        {
            activeLock = doorLock;
            if (panelRoot != null) panelRoot.SetActive(true);
            if (hintText != null) hintText.text = "←→で桁を選び、↑↓で数字を変更　Enterで試す　Escで戻る";
            for (int i = 0; i < digits.Length; i++)
            {
                digits[i] = 0;
            }
            selectedIndex = 0;
            RefreshTexts();
            GameManager.Instance?.SetPlayerInputLocked(true);
            OpenChanged?.Invoke(true);
        }

        public void ShowHint(string text)
        {
            if (hintText != null) hintText.text = text;
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            GameManager.Instance?.SetPlayerInputLocked(false);
            OpenChanged?.Invoke(false);
        }

        public void Increment(int index)
        {
            digits[index] = (digits[index] + 1) % 10;
            RefreshTexts();
        }

        public void Decrement(int index)
        {
            digits[index] = (digits[index] + 9) % 10;
            RefreshTexts();
        }

        public void SubmitPressed()
        {
            activeLock?.OnDialSubmit(digits[0], digits[1], digits[2]);
        }

        public void ClosePressed()
        {
            Close();
        }

        public void Shake()
        {
            if (shakeTarget == null) return;
            shakeBasePos = shakeTarget.anchoredPosition;
            shakeTimer = 0.35f;
        }

        void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                HandleKeyboard();
            }

            if (shakeTimer <= 0f || shakeTarget == null) return;
            shakeTimer -= Time.unscaledDeltaTime;
            float offset = Mathf.Sin(shakeTimer * 60f) * 6f * Mathf.Clamp01(shakeTimer / 0.35f);
            shakeTarget.anchoredPosition = shakeBasePos + new Vector2(offset, 0f);
            if (shakeTimer <= 0f)
            {
                shakeTarget.anchoredPosition = shakeBasePos;
            }
        }

        // Keyboard control so the lock stays usable without relying on mouse clicks.
        void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                selectedIndex = (selectedIndex + 1) % digits.Length;
                RefreshTexts();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                selectedIndex = (selectedIndex + digits.Length - 1) % digits.Length;
                RefreshTexts();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Increment(selectedIndex);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Decrement(selectedIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitPressed();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        void RefreshTexts()
        {
            for (int i = 0; i < digitTexts.Length; i++)
            {
                if (digitTexts[i] == null) continue;
                digitTexts[i].text = digits[i].ToString();
                digitTexts[i].color = i == selectedIndex ? selectedDigitColor : idleDigitColor;
            }
        }
    }
}
