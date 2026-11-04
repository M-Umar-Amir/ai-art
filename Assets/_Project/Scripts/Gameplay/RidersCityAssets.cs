using UnityEngine;

namespace DallasDash.Gameplay
{
    [CreateAssetMenu(menuName = "Dash Riders/City asset catalogue")]
    public sealed class RidersCityAssets : ScriptableObject
    {
        public GameObject[] riderPrefabs;
        public GameObject[] buildings;
        public GameObject[] trees;
        public GameObject[] props;
        public GameObject[] obstacles;
        public Material[] materials;
        public GameObject[] services;
        public GameObject servicePlatform;
        public GameObject chargingStation;
        public GameObject raceGate;
        public GameObject pedestrianBridge, marketStall;
        public GameObject[] districtRiders, districtObstacles;
        public GameObject districtCharger, districtFood, districtFinish;
    }
}
