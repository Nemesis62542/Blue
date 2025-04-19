using UnityEngine;

namespace Blue.UI.Screen
{
    public class ScreenStateIdentifier : MonoBehaviour
    {
        [SerializeField] private ScreenState screenState;
        public ScreenState ScreenState => screenState;
    }
}
