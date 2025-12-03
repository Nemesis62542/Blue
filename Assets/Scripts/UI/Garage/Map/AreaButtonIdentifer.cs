using UnityEngine;

namespace Blue.UI.Garage.Map
{
    public class AreaButtonIdentifer : MonoBehaviour
    {
        [SerializeField] private AreaType type;
        public AreaType AreaType => type;
    }
}