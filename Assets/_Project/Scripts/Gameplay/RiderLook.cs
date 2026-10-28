using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Which crew member is riding, and whether they're on the bike or standing at the curb (Loading Up).
    /// Index order matches PlayerProfile.Character: 0 Finn, 1 Faye, 2 Fizz-11 (rolls on his own tire, no bike).
    public class RiderLook : MonoBehaviour
    {
        public GameObject bicycle;
        /// The courier scooter, earned through missions (PlayerProfile.Ride == 1).
        public GameObject scooter;
        /// How far the saddle sits from the bicycle's, so the seated rider lands on the scooter seat.
        public Vector3 scooterSeat;
        public GameObject[] seated;
        public GameObject[] standing;

        Vector3[] seatBase;

        public void Apply(int character, bool onBike)
        {
            character = Mathf.Clamp(character, 0, seated.Length - 1);
            bool rolling = character == 2;
            // The bike stays in shot at the curb too; only Fizz-11 never has one.
            bool onScooter = scooter && PlayerProfile.Ride == 1;
            if (bicycle) bicycle.SetActive(!rolling && !onScooter);
            if (scooter) scooter.SetActive(!rolling && onScooter);
            if (seatBase == null)
            {
                seatBase = new Vector3[seated.Length];
                for (int i = 0; i < seated.Length; i++) if (seated[i]) seatBase[i] = seated[i].transform.localPosition;
            }
            for (int i = 0; i < seated.Length; i++)
            {
                if (seated[i] && i != 2) seated[i].transform.localPosition = seatBase[i] + (onScooter ? scooterSeat : Vector3.zero);
                if (seated[i]) seated[i].SetActive(onBike && i == character);
                if (i < standing.Length && standing[i]) standing[i].SetActive(!onBike && i == character);
            }
        }
    }
}
