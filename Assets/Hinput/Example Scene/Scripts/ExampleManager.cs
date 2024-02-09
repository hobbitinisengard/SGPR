using System.Linq;
using UnityEngine;

namespace HinputClasses.Internal {
    // Hinput class handling the Example Scene.
    public class ExampleManager : MonoBehaviour {
        [Header("STATE")]
        public GameObject currentPanel;

        [Header("REFERENCES")]
        public GameObject oneGamepad;
        public GameObject twoGamepads;
        public GameObject fourGamepads;
        public GameObject eightGamepads;

        public void Start() {
            currentPanel = oneGamepad;
        }

        public void Update() {
            int highestConnectedGamepad;
            try {
                highestConnectedGamepad = Hinput.gamepad
                    .Where(gamepad => gamepad.isConnected)
                    .Select(gamepad => gamepad.index)
                    .Max();
            } catch { return; }
            
            if (highestConnectedGamepad < 1) ActivatePanel(oneGamepad);
            else if (highestConnectedGamepad < 2) ActivatePanel(twoGamepads);
            else if (highestConnectedGamepad < 4) ActivatePanel(fourGamepads);
            else ActivatePanel(eightGamepads);
        }

        private void ActivatePanel(GameObject panel) {
            if (panel == currentPanel) return;
            currentPanel.SetActive(false);
            panel.SetActive(true);
            currentPanel = panel;
        }
    }
}
