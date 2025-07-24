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

        public static void LoadScene(string scene_name, Action on_complete = null)
        {
            if (runnerObj != null) return;

            runnerObj = new GameObject("SceneLoaderRunner");
            SceneLoaderRunner runner = runnerObj.AddComponent<SceneLoaderRunner>();
            runner.StartLoading(scene_name, () =>
            {
                on_complete?.Invoke();
                runnerObj = null;
            });
        }

        public static IEnumerator RunAsync(string scene_name, Action on_complete)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene_name);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            on_complete?.Invoke();
        }

        private class SceneLoaderRunner : MonoBehaviour
        {
            public void StartLoading(string scene_name, Action on_complete)
            {
                DontDestroyOnLoad(gameObject);

                Action wrapped = () =>
                {
                    on_complete?.Invoke();
                    Destroy(gameObject);
                };

                StartCoroutine(RunAsync(scene_name, wrapped));
            }
        }
    }
}