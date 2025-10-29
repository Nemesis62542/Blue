using System;
using System.Collections.Generic;
using Blue.Input;
using Blue.UI.Garage.CraftTable;
using Blue.UI.Garage.Strage;
using Blue.UI.Screen;
using UnityEngine;

namespace Blue.UI.Garage
{
    public class GarageSceneController : MonoBehaviour
    {
        [SerializeField] StrageController strage;
        [SerializeField] CraftTableController craftTable;
        [SerializeField] private List<CanvasGroup> screenCanvasGroups;

        private Dictionary<ScreenState, CanvasGroup> screenDictionary;
        private ScreenState currentScreenState = ScreenState.None;
        private PlayerInputHandler playerInput;

        public ScreenState CurrentScreenState => currentScreenState;
        public event Action<ScreenState> OnScreenStateChanged;

        private void Awake()
        {
            playerInput = new PlayerInputHandler();

            InitializeScreenDictionary();
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

        public void ShowScreen(ScreenState state)
        {
            HideAllScreen();
            currentScreenState = state;
            OnScreenStateChanged?.Invoke(state);

            if (screenDictionary.TryGetValue(state, out CanvasGroup screen))
            {
                SetScreenVisible(screen, true);
            }

            switch(CurrentScreenState)
            {
                case ScreenState.CraftTable:
                    craftTable.Initialize();
                break;

                case ScreenState.Strage:
                    strage.Initialize(playerInput);
                break;
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
    }
}