using System;
using Blue.Game;
using UnityEngine;

namespace Blue.UI.Garage.Map
{
    public class MapController : MonoBehaviour
    {
        public void OnClickAreaButton(AreaButtonIdentifer area_button)
        {
            switch(area_button.AreaType)
            {
                case AreaType.Shallow:
                    MoveToTargetArea("Tutorial");
                break;

                case AreaType.Coast:
                    MoveToTargetArea("Terrain");
                break;
            }
        }

        private void MoveToTargetArea(string scene_name)
        {
            SceneLoader.LoadScene(scene_name);
        }
    }
}