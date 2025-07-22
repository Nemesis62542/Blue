using Blue.Entity;
using UnityEngine;

namespace Blue.Object
{
    public class AquariumController : MonoBehaviour
    {
        [SerializeField] private EntityData entity;
        [SerializeField] private Transform spawnPoint;

        void Start()
        {
            Instantiate(entity.Object, spawnPoint.position, Quaternion.identity);
        }
    }
}