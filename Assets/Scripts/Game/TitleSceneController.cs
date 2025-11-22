using Blue.Save;
using UnityEngine;

namespace Blue.Game
{
    public class TitleSceneController : MonoBehaviour
    {
        private void Update()
        {
            SaveManager.DeleteSaveData();
            if (UnityEngine.Input.anyKeyDown) SceneLoader.LoadScene("Tutorial");
        }
    }
}