using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Spins Bolt's monowheel with the run so the tyre reads as rolling rather than sliding.
    public class WheelSpin : MonoBehaviour
    {
        public float degreesPerSecond = 540f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running || gm.Paused) return;
            transform.Rotate(Vector3.right, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
