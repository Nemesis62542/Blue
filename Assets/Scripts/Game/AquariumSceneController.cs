using System.Collections.Generic;
using Blue.Entity;
using Blue.Object;
using UnityEngine;

namespace Blue.Game
{
    public class AquariumSceneController : MonoBehaviour
    {
        [SerializeField] private List<AquariumController> aquaria = new List<AquariumController>();

        void Awake()
        {
            //InitializeAquaria();
        }

        private void InitializeAquaria(List<EntityData> entities)
        {
            for (int i = 0; i < aquaria.Count; i++)
            {
                aquaria[i].SetDisplayEntity(aquaria[i].FirstEnptyDisplayData(entities[i].School != null), entities[i]);
            }
        }
    }
}