using Blue.Save;
using UnityEngine;

namespace Blue.Game
{
    public class TitleSceneController : MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.anyKeyDown) 
            {
                SaveManager.DeleteSaveData();
                SceneLoader.LoadScene("Tutorial");
            }
        }
    }
}