using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

namespace NFPS.UI.Common
{
    public class UISelectableNavigator : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;

        public void InitializeSelection()
        {
            StartCoroutine(InitializeSelectionRoutine());
        }

        private IEnumerator InitializeSelectionRoutine()
        {
            yield return null;

            List<Selectable> selectable_list = new List<Selectable>();
            foreach (Transform child in contentRoot)
            {
                if (child.TryGetComponent(out Selectable selectable))
                {
                    selectable_list.Add(selectable);
                }
            }

            if (selectable_list.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(selectable_list[0].gameObject);
            }
        }

        public void ClearSelection()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
