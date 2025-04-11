using UnityEngine;

namespace NFPS.UI.Screen
{
    public class ScreenStateIdentifier : MonoBehaviour
    {
        [SerializeField] private ScreenState screenState;
        public ScreenState ScreenState => screenState;
    }
}
