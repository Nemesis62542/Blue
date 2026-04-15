namespace Blue.Game
{
    /// <summary>
    /// GameEventControllerが必要とするランタイム操作を抽象化
    /// </summary>
    public interface IGameEventRuntime
    {
        void ForwardIngame();
        void ForwardMovie();
        void PlayShallowSeaBGM();
        void PlayMecaSharkBGM();
        void StopBGM(float fadeTime);
        void StopEnvironmentSound();
        void ShowSubtitle(string message);
        void ShowMessage(string message, float duration);
        void StartMecaSharkBattle();
        void LoadScene(string sceneName);
    }
}
