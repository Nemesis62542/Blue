using System.Collections.Generic;
using Blue.Entity;
using Blue.Save;
using UnityEngine;

namespace Blue.Game
{
    public class AquariumSceneController : MonoBehaviour
    {
        [SerializeField] private AquariumManager aquariumManager;

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
            if (aquariumManager == null)
            {
                Debug.LogError("AquariumManager が設定されていません");
                return;
            }

            // AquariumManager に生物配置を委譲
            aquariumManager.PlaceEntities(entities);
        }
    }
}