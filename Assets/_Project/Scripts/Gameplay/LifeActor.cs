using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The small motions that make DistrictLife's residents read as alive without a skeletal rig:
    /// walk-in-place, a football kick, a cricket bowl and bat, a seated diner, and a cheer.
    /// Limbs are pivots at the shoulder/hip with a block child, so a rotation swings the whole limb.
    public enum LifeMotion { Idle, Walk, Kick, Bowl, Bat, Dine, Cheer, Wave }

    public class LifeActor : MonoBehaviour
    {
        public LifeMotion motion = LifeMotion.Idle;
        public float speed = 1f;
        public Transform leftLeg, rightLeg, leftArm, rightArm, head, ball, bat;
        public float ballTravel = 2.4f;

        Vector3 basePos;
        Quaternion baseHead;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            baseHead = head ? head.localRotation : Quaternion.identity;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = (Time.time + phase) * speed;
            float step = Mathf.Sin(t * 3.2f);
            switch (motion)
            {
                case LifeMotion.Walk:
                    if (leftLeg) leftLeg.localRotation = Quaternion.Euler(step * 32f, 0f, 0f);
                    if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-step * 32f, 0f, 0f);
                    if (leftArm) leftArm.localRotation = Quaternion.Euler(-step * 26f, 0f, 0f);
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(step * 26f, 0f, 0f);
                    transform.localPosition = basePos + Vector3.up * Mathf.Abs(step) * 0.035f;
                    break;

                case LifeMotion.Kick:
                    if (rightLeg) rightLeg.localRotation = Quaternion.Euler(Mathf.Lerp(35f, -70f, Mathf.Max(0f, Mathf.Sin(t * 2.1f))), 0f, 0f);
                    if (leftArm) leftArm.localRotation = Quaternion.Euler(-28f, 0f, 18f);
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(24f, 0f, -22f);
                    if (ball)
                    {
                        float k = Mathf.Repeat(t * 0.42f, 1f);
                        float x = Mathf.Lerp(-0.2f, ballTravel, k < 0.5f ? k * 2f : (1f - k) * 2f);
                        float lift = Mathf.Sin(k * Mathf.PI) * 0.55f;
                        ball.localPosition = new Vector3(x, 0.16f + lift, 0.1f);
                    }
                    break;

                case LifeMotion.Bowl:
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(150f, 20f, Mathf.Repeat(t * 0.4f, 1f)), 0f, 0f);
                    if (ball)
                    {
                        float k = Mathf.Repeat(t * 0.4f, 1f);
                        ball.localPosition = new Vector3(0f, Mathf.Lerp(1.25f, 0.18f, Mathf.Clamp01(k * 2f)), Mathf.Lerp(0.2f, 5.6f, k));
                    }
                    break;

                case LifeMotion.Bat:
                    if (bat) bat.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-55f, 70f, Mathf.Repeat(t * 0.4f + 0.35f, 1f)));
                    if (leftLeg) leftLeg.localRotation = Quaternion.Euler(0f, 0f, 16f);
                    if (rightLeg) rightLeg.localRotation = Quaternion.Euler(0f, 0f, -16f);
                    break;

                case LifeMotion.Dine:
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(-20f, -95f, (Mathf.Sin(t * 1.4f) + 1f) * 0.5f), 0f, 0f);
                    if (head) head.localRotation = baseHead * Quaternion.Euler(Mathf.Sin(t * 1.1f) * 7f, 0f, 0f);
                    break;

                case LifeMotion.Cheer:
                    if (leftArm) leftArm.localRotation = Quaternion.Euler(-150f + Mathf.Sin(t * 5f) * 12f, 0f, 22f);
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(-150f - Mathf.Sin(t * 5f) * 12f, 0f, -22f);
                    transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 4f)) * 0.06f;
                    break;

                case LifeMotion.Wave:
                    if (rightArm) rightArm.localRotation = Quaternion.Euler(-120f, 0f, -35f + Mathf.Sin(t * 5.5f) * 22f);
                    break;

                default:
                    transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * 1.2f) * 0.015f;
                    break;
            }
        }
    }
}
