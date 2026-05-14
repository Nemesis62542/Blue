using System;
using System.Collections.Generic;
using Blue.UI.Screen;
using Unity.Cinemachine;
using UnityEngine;

namespace Blue.UI.Garage
{
    [Serializable]
    public class ScreenCameraMapping
    {
        public ScreenState screenState;
        public CinemachineCamera virtualCamera;
    }

    public class GarageCameraController : MonoBehaviour
    {
        [SerializeField] private GarageSceneController garageSceneController;
        [SerializeField] private List<ScreenCameraMapping> cameraMappings;
        [SerializeField] private int activePriority = 10;
        [SerializeField] private int inactivePriority = 0;

        private Dictionary<ScreenState, CinemachineCamera> cameraDictionary;

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
            cameraDictionary = new Dictionary<ScreenState, CinemachineCamera>();

            foreach (ScreenCameraMapping mapping in cameraMappings)
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

            if (cameraDictionary.TryGetValue(newState, out CinemachineCamera camera))
            {
                camera.Priority = activePriority;
            }
        }

        private void SetAllCamerasInactive()
        {
            foreach (CinemachineCamera camera in cameraDictionary.Values)
            {
                if (camera != null)
                {
                    camera.Priority = inactivePriority;
                }
            }
        }
    }
}
