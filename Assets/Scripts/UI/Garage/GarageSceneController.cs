using System;
using System.Collections.Generic;
using Blue.Input;
using Blue.UI.Screen;
using UnityEngine;

namespace Blue.UI.Garage
{
    public class GarageSceneController : MonoBehaviour
    {
        [SerializeField] private List<CanvasGroup> screenCanvasGroups;

        private Dictionary<ScreenState, CanvasGroup> screenDictionary;
        private List<IScreenController> screenControllers;
        private ScreenState currentScreenState = ScreenState.None;

        public ScreenState CurrentScreenState => currentScreenState;
        public event Action<ScreenState> OnScreenStateChanged;

        private void Awake()
        {
            if(PlayerInputHandler.Instance == null) new PlayerInputHandler();

            InitializeScreenDictionary();
            InitializeIScreenController();
            ShowScreen(ScreenState.GarageHome);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) TransitionHome();
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

        private void InitializeIScreenController()
        {
            screenControllers = new List<IScreenController>();

            foreach (CanvasGroup canvas in screenCanvasGroups)
            {
                if (canvas.TryGetComponent(out IScreenController screenController))
                {
                    screenControllers.Add(screenController);
                }
            }
        }

        public void ShowScreen(ScreenState state)
        {
            HideAllScreen();
            currentScreenState = state;
            OnScreenStateChanged?.Invoke(state);

            if (screenDictionary.TryGetValue(state, out CanvasGroup screen))
            {
                SetScreenVisible(screen, true);
            }

            foreach(IScreenController screenController in screenControllers)
            {
                screenController.OnScreenChanged(state);
            }
        }

        private void HideAllScreen()
        {
            foreach (CanvasGroup screen in screenDictionary.Values)
            {
                SetScreenVisible(screen, false);
            }
        }

        private void SetScreenVisible(CanvasGroup screen, bool is_visible)
        {
            if (screen == null) return;

            screen.alpha = is_visible ? 1 : 0;
            screen.interactable = is_visible;
            screen.blocksRaycasts = is_visible;
        }

        public void TransitionCraftTable()
        {
            ShowScreen(ScreenState.CraftTable);
        }

        public void TransitionStrage()
        {
            ShowScreen(ScreenState.Strage);
        }

        public void TransitionHome()
        {
            ShowScreen(ScreenState.GarageHome);
        }

        public void TransitionMap()
        {
            ShowScreen(ScreenState.Map);
        }
    }
}