using System.Collections.Generic;
using Blue.Recipe;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableController : MonoBehaviour
    {
        [SerializeField] private CraftTableView view;
        [SerializeField] private List<RecipeData> recipes;

        public void Initialize()
        {
            view.Initialize(recipes);
        }
    }
}