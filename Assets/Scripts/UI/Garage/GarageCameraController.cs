using System;
using System.Collections.Generic;
using Blue.UI.Screen;
using Cinemachine;
using UnityEngine;

namespace Blue.UI.Garage
{
    [Serializable]
    public class ScreenCameraMapping
    {
        public ScreenState screenState;
        public CinemachineVirtualCamera virtualCamera;
    }

    public class GarageCameraController : MonoBehaviour
    {
        [SerializeField] private GarageSceneController garageSceneController;
        [SerializeField] private List<ScreenCameraMapping> cameraMappings;
        [SerializeField] private int activePriority = 10;
        [SerializeField] private int inactivePriority = 0;

        private Dictionary<ScreenState, CinemachineVirtualCamera> cameraDictionary;

        private void Awake()
        {
            InitializeCameraDictionary();
        }

        private void OnEnable()
        {
            if (garageSceneController != null)
            {
                garageSceneController.OnScreenStateChanged += OnScreenStateChanged;
            }
        }

        private void OnDisable()
        {
            if (garageSceneController != null)
            {
                garageSceneController.OnScreenStateChanged -= OnScreenStateChanged;
            }
        }

        private void InitializeCameraDictionary()
        {
            cameraDictionary = new Dictionary<ScreenState, CinemachineVirtualCamera>();

            foreach (var mapping in cameraMappings)
            {
                if (mapping.virtualCamera != null)
                {
                    cameraDictionary[mapping.screenState] = mapping.virtualCamera;
                }
            }
        }

        private void OnScreenStateChanged(ScreenState newState)
        {
            if (newState == ScreenState.None) return;

            SetAllCamerasInactive();

            if (cameraDictionary.TryGetValue(newState, out var camera))
            {
                camera.Priority = activePriority;
            }
        }

        private void SetAllCamerasInactive()
        {
            foreach (var camera in cameraDictionary.Values)
            {
                if (camera != null)
                {
                    camera.Priority = inactivePriority;
                }
            }
        }
    }
}
