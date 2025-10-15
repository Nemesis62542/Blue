using Blue.Input;
using Blue.UI.Garage.Strage;
using UnityEngine;

namespace Blue.UI.Garage
{
    public class GarageSceneController : MonoBehaviour
    {
        [SerializeField] StrageController strage;

        PlayerInputHandler playerInput;

        private void Awake()
        {
            playerInput = new PlayerInputHandler();

            strage.Initialize(playerInput);
        }
    }
}