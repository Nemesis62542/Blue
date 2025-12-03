using Blue.Game;
using UnityEngine;

namespace Blue.UI.Garage.Home
{
    public class HomeController : MonoBehaviour
    {
        public void MoveToAquarium()
        {
            TransitionScene("Aquarium");
        }

        private void TransitionScene(string target_scene)
        {
            SceneLoader.LoadScene(target_scene);
        }
    }
}