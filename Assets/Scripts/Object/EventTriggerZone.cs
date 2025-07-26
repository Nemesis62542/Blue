using Blue.Game;
using UnityEngine;

namespace Blue.Object
{
    [RequireComponent(typeof(Collider))]
    public class EventTriggerZone : MonoBehaviour
    {
        [SerializeField] private GameEventController eventController;
        [SerializeField] private EventID triggerEventID;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            eventController.TriggerEvent(triggerEventID);
            gameObject.SetActive(false);
        }
    }
}