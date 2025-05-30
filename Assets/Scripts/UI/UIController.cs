using System;
using System.Collections.Generic;
using Blue.UI.Screen;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blue.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private List<CanvasGroup> screenCanvasGroups;
        private Dictionary<ScreenState, CanvasGroup> screenDictionary;
        private ScreenState currentScreenState = ScreenState.None;

        public ScreenState CurrentScreenState => currentScreenState;
        public event Action<ScreenState> OnScreenStateChanged;

        private void Awake()
        {
            InitializeScreenDictionary();
            ShowScreen(ScreenState.Ingame);
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
            SetScreenVisible(GetCanvasGroup(currentScreenState), false);
            currentScreenState = state;
            OnScreenStateChanged?.Invoke(state);

            if (screenDictionary.TryGetValue(state, out CanvasGroup screen))
            {
                SetScreenVisible(screen, true);
            }

            ShowCursor(state != ScreenState.Ingame);
        }

        public void HideCurrentScreen()
        {
            ShowScreen(ScreenState.Ingame);
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
