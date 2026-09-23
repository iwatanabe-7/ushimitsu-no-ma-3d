using UnityEngine;
using UnityEngine.UI;
using Ushimitsu.Game;

namespace Ushimitsu.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        public GameObject titlePanel;
        public HUDController hud;
        public Button startButton;

        bool dismissed;

        void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(StartGamePressed);
        }

        void Start()
        {
            if (titlePanel != null) titlePanel.SetActive(true);
            hud?.SetGameplayHudVisible(false);
            GameManager.Instance?.SetPlayerInputLocked(true);
        }

        void Update()
        {
            if (dismissed) return;
            // Deliberately not E: that key is the interact/dismiss key, and the same
            // press would otherwise close the opening message in the very same frame.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))
            {
                StartGamePressed();
            }
        }

        public void StartGamePressed()
        {
            if (dismissed) return;
            dismissed = true;

            if (titlePanel != null) titlePanel.SetActive(false);
            hud?.SetGameplayHudVisible(true);
            GameManager.Instance?.SetPlayerInputLocked(false);
            GameManager.Instance?.BeginGame();
        }
    }
}
