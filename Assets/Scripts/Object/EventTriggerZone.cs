using Blue.Game;
using UnityEngine;

namespace Blue.Object
{
    [RequireComponent(typeof(Collider))]
    public class EventTriggerZone : MonoBehaviour
    {
        [SerializeField] private GameEventController eventController;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            eventController.TriggerEvent();
            gameObject.SetActive(false); // 一度だけ発動
        }
    }
}