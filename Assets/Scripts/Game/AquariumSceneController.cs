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
                EntityData entity = entities[i];
                AquariumController aquarium = aquaria[i];

                aquarium.SetDisplayEntity(aquarium.FirstEnptyDisplayData(entity.Habitation, entity.School != null), entity);
            }
        }
    }
}