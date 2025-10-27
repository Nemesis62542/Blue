using Blue.Input;
using Blue.UI.Garage.CraftTable;
using Blue.UI.Garage.Strage;
using UnityEngine;

namespace Blue.UI.Garage
{
    public class GarageSceneController : MonoBehaviour
    {
        [SerializeField] StrageController strage;
        [SerializeField] CraftTableController craftTable;

        PlayerInputHandler playerInput;

        private void Awake()
        {
            playerInput = new PlayerInputHandler();

            //strage.Initialize(playerInput);
            craftTable.Initialize();
        }
    }
}