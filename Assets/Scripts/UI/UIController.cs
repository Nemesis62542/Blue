using System.Collections.Generic;
using NFPS.UI.Screen;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NFPS.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private List<CanvasGroup> screenCanvasGroups;
        private Dictionary<ScreenState, CanvasGroup> screenDictionary;

        private ScreenState currentScreenState = ScreenState.None;
        public ScreenState CurrentScreenState => currentScreenState;

        private void Awake()
        {
            InitializeScreenDictionary();
        }

        private void InitializeScreenDictionary()
        {
            screenDictionary = new Dictionary<ScreenState, CanvasGroup>();

            foreach (CanvasGroup canvas in screenCanvasGroups)
            {
                if (canvas.TryGetComponent(out ScreenStateIdentifier identifier))
                {
                    screenDictionary[identifier.ScreenState] = canvas;
                }
            }
        }

        public void ShowScreen(ScreenState state)
        {
            if (currentScreenState != ScreenState.None)
            {
                SetScreenVisible(GetCanvasGroup(currentScreenState), false);
            }

            currentScreenState = state;

            if (screenDictionary.TryGetValue(state, out CanvasGroup screen))
            {
                SetScreenVisible(screen, true);
                ShowCursor(true);
            }
            else
            {
                currentScreenState = ScreenState.None;
                ShowCursor(false);
            }
        }

        public void HideCurrentScreen()
        {
            SetScreenVisible(GetCanvasGroup(currentScreenState), false);
            currentScreenState = ScreenState.None;
            ShowCursor(false); 
        }

        private CanvasGroup GetCanvasGroup(ScreenState state)
        {
            if (screenDictionary.TryGetValue(state, out CanvasGroup screen))
            {
                return screen;
            }
            else
            {
                return null;
            }
        }

        private void HideAllScreen()
        {
            foreach (CanvasGroup screen in screenDictionary.Values)
            {
                SetScreenVisible(screen, false);
            }
        }

        public void OpenSettingsFromPause()
        {
            ShowScreen(ScreenState.Settings);
        }

        public void BackToPauseFromSettings()
        {
            ShowScreen(ScreenState.Pause);
        }

        private void SetScreenVisible(CanvasGroup screen, bool is_visible)
        {
            if (screen == null) return;

            screen.alpha = is_visible ? 1 : 0;
            screen.interactable = is_visible;
            screen.blocksRaycasts = is_visible;

            if(!is_visible) 
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void ShowCursor(bool is_visible)
        {
            Cursor.lockState = is_visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = is_visible;
        }
    }
}
