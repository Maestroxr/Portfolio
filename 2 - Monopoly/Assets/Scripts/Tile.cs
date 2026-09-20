using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// One space of the 3D board: the pieces on it that change during a match. The owner tag is a plate in the owner's
    /// colour along the outer edge, the mortgage stamp darkens the space, the highlight glows around it, and houses and
    /// hotels stand on the colour bar of a street (taken from the board's pools).
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private Renderer ownerTag;
        [SerializeField] private GameObject mortgaged;
        [SerializeField] private Renderer highlight;
        [Tooltip("Middle of the colour bar, rotated like the space: local x runs along the side, local z to the board centre.")]
        [SerializeField] private Transform buildingAnchor;
        [SerializeField] private float buildingSpacing = 0.23f;

        private readonly List<Building> buildings = new List<Building>();
        private BuildingPool usedHousePool;
        private BuildingPool usedHotelPool;
        private MaterialPropertyBlock block;
        private Color highlightColor = Color.white;
        private int shownHouses;
        private Coroutine popping;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public int Index => index;

        public int Houses => shownHouses;

        public void SetOwner(Material material)
        {
            if (ownerTag == null)
            {
                return;
            }
            ownerTag.gameObject.SetActive(material != null);
            if (material != null)
            {
                ownerTag.sharedMaterial = material;
            }
        }

        public void SetMortgaged(bool value)
        {
            if (mortgaged != null)
            {
                mortgaged.SetActive(value);
            }
        }

        public void SetHighlight(bool on, Color color)
        {
            if (highlight == null)
            {
                return;
            }
            highlight.gameObject.SetActive(on);
            highlightColor = color;
            SetHighlightStrength(1f);
        }

        public void SetHighlightStrength(float strength)
        {
            if (highlight == null || !highlight.gameObject.activeSelf)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            Color c = highlightColor * strength;
            c.a = highlightColor.a * strength;
            block.SetColor(BaseColor, c);
            highlight.SetPropertyBlock(block);
        }

        /// <summary>Shows the buildings of a street: 0 to 4 houses, or a hotel for 5.</summary>
        public void SetBuildings(int houses, BuildingPool housePool, BuildingPool hotelPool, bool animate)
        {
            if (buildingAnchor == null || housePool == null || hotelPool == null)
            {
                return;
            }
            houses = Mathf.Clamp(houses, 0, MonopolyMatch.Hotel);
            if (houses == shownHouses && buildings.Count == ExpectedCount(houses))
            {
                return;
            }
            bool grew = houses > shownHouses;
            int previous = shownHouses;
            ReturnBuildings();
            usedHousePool = housePool;
            usedHotelPool = hotelPool;
            if (houses == MonopolyMatch.Hotel)
            {
                Building hotel = hotelPool.Deploy();
                Place(hotel, 0f);
                buildings.Add(hotel);
            }
            else
            {
                for (int i = 0; i < houses; i++)
                {
                    Building house = housePool.Deploy();
                    Place(house, (i - (houses - 1) * 0.5f) * buildingSpacing);
                    buildings.Add(house);
                }
            }
            shownHouses = houses;
            if (animate && isActiveAndEnabled)
            {
                if (popping != null)
                {
                    StopCoroutine(popping);
                }
                popping = StartCoroutine(Pop(grew, previous));
            }
        }

        private static int ExpectedCount(int houses)
        {
            return houses == MonopolyMatch.Hotel ? 1 : houses;
        }

        private void Place(Building building, float along)
        {
            Transform t = building.transform;
            t.SetParent(buildingAnchor, false);
            t.localPosition = new Vector3(along, 0f, 0f);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            building.Show();
        }

        private IEnumerator Pop(bool grew, int previous)
        {
            if (!grew)
            {
                yield break;
            }
            // The new building (or the hotel) drops in; the others settle with a small squash.
            int count = buildings.Count;
            for (int i = 0; i < count; i++)
            {
                bool fresh = shownHouses == MonopolyMatch.Hotel || i >= previous;
                if (fresh)
                {
                    buildings[i].Drop();
                }
            }
            yield return null;
        }

        private void ReturnBuildings()
        {
            foreach (Building building in buildings)
            {
                if (building == null)
                {
                    continue;
                }
                BuildingPool pool = building.IsHotel ? usedHotelPool : usedHousePool;
                if (pool != null)
                {
                    pool.Recycle(building);
                }
                else
                {
                    building.gameObject.SetActive(false);
                }
            }
            buildings.Clear();
        }
    }
}
