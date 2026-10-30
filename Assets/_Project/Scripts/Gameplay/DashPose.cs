using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Transform animation keeps the crew expressive without a skeletal rig: hips stay planted,
    // shoulders and knees swing the limbs around the authored pose.
    public class DashPose : MonoBehaviour
    {
        public Transform leftLeg, rightLeg, leftArm, rightArm;
        public Transform leftKnee, rightKnee, leftElbow, rightElbow;
        public bool robot;
        public bool riding;

        Quaternion legL, legR, armL, armR, kneeL, kneeR, elbowL, elbowR;
        bool captured;
        float phase;

        void Awake() { Capture(); }

        void Capture()
        {
            captured = true;
            legL = leftLeg ? leftLeg.localRotation : Quaternion.identity;
            legR = rightLeg ? rightLeg.localRotation : Quaternion.identity;
            armL = leftArm ? leftArm.localRotation : Quaternion.identity;
            armR = rightArm ? rightArm.localRotation : Quaternion.identity;
            kneeL = leftKnee ? leftKnee.localRotation : Quaternion.identity;
            kneeR = rightKnee ? rightKnee.localRotation : Quaternion.identity;
            elbowL = leftElbow ? leftElbow.localRotation : Quaternion.identity;
            elbowR = rightElbow ? rightElbow.localRotation : Quaternion.identity;
        }

        void Update()
        {
            if (!captured) Capture();
            var gm = GameManager.Instance;
            bool moving = gm && gm.State == RunState.Running && !gm.Paused;
            if (!moving) { Rest(); return; }

            phase += Time.deltaTime * gm.Speed * (riding || robot ? 0.26f : 0.42f);
            float s = Mathf.Sin(phase);

            if (riding || robot)
            {
                // Balance sway: arms breathe with the run, legs stay planted on the deck or pegs.
                if (leftArm) leftArm.localRotation = armL * Quaternion.Euler(s * 2.2f, 0f, s * 1.4f);
                if (rightArm) rightArm.localRotation = armR * Quaternion.Euler(-s * 2.2f, 0f, -s * 1.4f);
                if (leftElbow) leftElbow.localRotation = elbowL * Quaternion.Euler(Mathf.Abs(s) * 3f, 0f, 0f);
                if (rightElbow) rightElbow.localRotation = elbowR * Quaternion.Euler(Mathf.Abs(s) * 3f, 0f, 0f);
                return;
            }

            const float swing = 27f;
            if (leftLeg) leftLeg.localRotation = legL * Quaternion.Euler(s * swing, 0f, 0f);
            if (rightLeg) rightLeg.localRotation = legR * Quaternion.Euler(-s * swing, 0f, 0f);
            if (leftKnee) leftKnee.localRotation = kneeL * Quaternion.Euler(Mathf.Max(0f, -s) * 36f, 0f, 0f);
            if (rightKnee) rightKnee.localRotation = kneeR * Quaternion.Euler(Mathf.Max(0f, s) * 36f, 0f, 0f);
            if (leftArm) leftArm.localRotation = armL * Quaternion.Euler(-s * 21f, 0f, 0f);
            if (rightArm) rightArm.localRotation = armR * Quaternion.Euler(s * 21f, 0f, 0f);
            if (leftElbow) leftElbow.localRotation = elbowL * Quaternion.Euler(6f + Mathf.Max(0f, s) * 14f, 0f, 0f);
            if (rightElbow) rightElbow.localRotation = elbowR * Quaternion.Euler(6f + Mathf.Max(0f, -s) * 14f, 0f, 0f);
        }

        void Rest()
        {
            if (leftLeg) leftLeg.localRotation = legL;
            if (rightLeg) rightLeg.localRotation = legR;
            if (leftArm) leftArm.localRotation = armL;
            if (rightArm) rightArm.localRotation = armR;
            if (leftKnee) leftKnee.localRotation = kneeL;
            if (rightKnee) rightKnee.localRotation = kneeR;
            if (leftElbow) leftElbow.localRotation = elbowL;
            if (rightElbow) rightElbow.localRotation = elbowR;
        }
    }
}
