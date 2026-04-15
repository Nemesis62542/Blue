using Blue.Audio;
using Blue.Entity;
using Blue.Player;
using Blue.UI;
using UnityEngine;

namespace Blue.Game
{
    /// <summary>
    /// GameEvent用の各システム橋渡し実装
    /// </summary>
    public class GameEventRuntimeBridge : MonoBehaviour, IGameEventRuntime
    {
        [SerializeField] private MecaSharkController shark;

        public void ForwardIngame()
        {
            PlayerController.Instance.ForwardIngame();
        }

        public void ForwardMovie()
        {
            PlayerController.Instance.ForwardMovie();
        }

        public void PlayShallowSeaBGM()
        {
            SoundController.Instance.PlayBGM(BGMType.ShallowSea);
        }

        public void PlayMecaSharkBGM()
        {
            SoundController.Instance.PlayBGM(BGMType.MecaShark);
        }

        public void StopBGM(float fadeTime)
        {
            SoundController.Instance.StopBGM(fadeTime);
        }

        public void StopEnvironmentSound()
        {
            SoundController.Instance.StopEnvironmentSound();
        }

        public void ShowSubtitle(string message)
        {
            SubtitleUIController.Instance.ShowMessage(message);
        }

        public void ShowMessage(string message, float duration)
        {
            MessageView.Instance.ShowMessage(new MessageData(message), duration);
        }

        public void StartMecaSharkBattle()
        {
            if (shark == null)
            {
                Debug.LogWarning("MecaSharkController is not assigned.");
                return;
            }

            shark.StartBattle();
        }

        public void LoadScene(string sceneName)
        {
            SceneLoader.LoadScene(sceneName);
        }
    }
}
