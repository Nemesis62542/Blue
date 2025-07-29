using Blue.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.Object
{
    public class EMPBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private string objectName;
        [SerializeField] private MecaSharkController shark;
        [SerializeField] private Slider slider;
        [SerializeField] private float maxBattery = 30.0f;

        private float battery;

        public string ObjectName => objectName;

        void Awake()
        {
            battery = maxBattery;
        }

        public void Interact(MonoBehaviour interactor)
        {
            if (battery < maxBattery) return;
            battery = 0;
            slider.value = 0;
            shark.Damage(new Attack.AttackData(null, shark, 100, Attack.AttackType.Magic, shark.transform.position));
        }

        void Update()
        {
            if (battery < maxBattery)
            {
                battery += Time.deltaTime;
                slider.value = battery / maxBattery;
            }
        }
    }
}