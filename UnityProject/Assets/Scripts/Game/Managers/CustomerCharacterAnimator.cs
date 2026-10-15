using UnityEngine;
using UnityEngine.AI;

namespace AIBusinessTycoon.Managers
{
    public class CustomerCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Transform animatedVisual;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private float walkFrequency = 7f;
        [SerializeField] private float swingAngle = 28f;
        [SerializeField] private float bobHeight = 0.035f;

        private NavMeshAgent agent;
        private Vector3 visualStartPosition;
        private float phase;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animatedVisual == null)
            {
                Transform character = transform.Find("AnimatedCustomer");
                Transform visual = character != null ? character.Find("AnimatedVisual") : transform.Find("AnimatedVisual");
                AssignVisualAndLimbs(visual);
            }

            if (animatedVisual != null)
                visualStartPosition = animatedVisual.localPosition;
        }

        private void Update()
        {
            if (animatedVisual == null)
                return;

            bool isMoving = agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.04f;
            if (isMoving)
            {
                phase += Time.deltaTime * walkFrequency;
                float stride = Mathf.Sin(phase);
                SetLimbRotation(leftArm, stride * swingAngle);
                SetLimbRotation(rightArm, -stride * swingAngle);
                SetLimbRotation(leftLeg, -stride * swingAngle);
                SetLimbRotation(rightLeg, stride * swingAngle);
                animatedVisual.localPosition = visualStartPosition + Vector3.up * (Mathf.Abs(stride) * bobHeight);
            }
            else
            {
                phase = 0f;
                SetLimbRotation(leftArm, 0f);
                SetLimbRotation(rightArm, 0f);
                SetLimbRotation(leftLeg, 0f);
                SetLimbRotation(rightLeg, 0f);
                animatedVisual.localPosition = Vector3.Lerp(animatedVisual.localPosition, visualStartPosition, Time.deltaTime * 8f);
            }
        }

        private static void SetLimbRotation(Transform limb, float angle)
        {
            if (limb != null)
                limb.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void AssignVisualAndLimbs(Transform visual)
        {
            if (visual == null)
                return;

            animatedVisual = visual;
            leftArm = visual.Find("LeftArm");
            rightArm = visual.Find("RightArm");
            leftLeg = visual.Find("LeftLeg");
            rightLeg = visual.Find("RightLeg");
        }
    }
}