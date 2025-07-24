using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blue.Game
{
    public static class SceneLoader
    {
        private static GameObject runnerObj;

        public static string CurrentSceneName => SceneManager.GetActiveScene().name;

        public static void LoadScene(string sceneName, Action onComplete = null)
        {
            if (runnerObj != null) return;

            runnerObj = new GameObject("SceneLoaderRunner");
            SceneLoaderRunner runner = runnerObj.AddComponent<SceneLoaderRunner>();
            runner.StartLoading(sceneName, () =>
            {
                onComplete?.Invoke();
                runnerObj = null;
            });
        }

        public static IEnumerator RunAsync(string sceneName, Action onComplete)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        private class SceneLoaderRunner : MonoBehaviour
        {
            public void StartLoading(string sceneName, Action onComplete)
            {
                DontDestroyOnLoad(gameObject);

                Action wrapped = () =>
                {
                    onComplete?.Invoke();
                    Destroy(gameObject);
                };

                StartCoroutine(RunAsync(sceneName, wrapped));
            }
        }
    }
}