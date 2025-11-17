using System.Collections.Generic;
using Blue.Entity;
using Blue.Object;
using Blue.Save;
using UnityEngine;

namespace Blue.Game
{
    public class AquariumSceneController : MonoBehaviour
    {
        [SerializeField] private List<AquariumController> aquaria = new List<AquariumController>();

        void Awake()
        {
            // セーブデータから捕獲した生物を読み込む
            List<EntityData> entities = SaveDataConverter.LoadCapturedEntitiesList();
            if (entities != null && entities.Count > 0)
            {
                InitializeAquaria(entities);
            }
        }

        private void InitializeAquaria(List<EntityData> entities)
        {
            // for (int i = 0; i < aquaria.Count; i++)
            // {
            //     EntityData entity = entities[i];
            //     AquariumController aquarium = aquaria[i];

            //     aquarium.SetDisplayEntity(aquarium.FirstEnptyDisplayData(entity.Habitation, entity.School != null), entity);
            // }

            foreach (EntityData entity in entities)
            {
                Debug.Log(entity.Name);
                AquariumController aquarium = aquaria[entity.ID];
                aquarium.SetDisplayEntity(aquarium.FirstEnptyDisplayData(entity.Habitation, entity.School != null), entity);
            }
        }
    }
}