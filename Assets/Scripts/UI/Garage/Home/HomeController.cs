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

        public void MoveToSea()
        {
            //TODO:海のどこに行くか、の機能も今後実装する
            TransitionScene("Tutorial");
        }

        private void TransitionScene(string target_scene)
        {
            SceneLoader.LoadScene(target_scene);
        }
    }
}