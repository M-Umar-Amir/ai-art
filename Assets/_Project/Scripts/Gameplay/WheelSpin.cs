using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Spins Bolt's monowheel with the run so the tyre reads as rolling rather than sliding.
    public class WheelSpin : MonoBehaviour
    {
        public float tireRadius = .33f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running || gm.Paused) return;
            transform.Rotate(Vector3.right, gm.Speed / Mathf.Max(.1f, tireRadius) * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
        }
    }
}
