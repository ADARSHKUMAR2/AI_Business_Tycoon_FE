using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Simple procedural walk cycle for the player character model.
    /// Attach to the same root as PlayerController and CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacterAnimator : MonoBehaviour
    {
        [Header("Character Rig")]
        [SerializeField] private Transform animatedVisual;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Walk Cycle")]
        [SerializeField, Min(0.1f)] private float walkFrequency = 7f;
        [SerializeField, Range(0f, 60f)] private float swingAngle = 28f;
        [SerializeField, Min(0f)] private float bobHeight = 0.035f;
        [SerializeField, Min(0f)] private float movementThreshold = 0.08f;

        private CharacterController characterController;
        private Vector3 visualStartPosition;
        private float phase;
        private Vector3 previousPosition;
        private bool hasPreviousPosition;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveCharacterRig();

            if (animatedVisual != null)
                visualStartPosition = animatedVisual.localPosition;
            previousPosition = transform.position;
            hasPreviousPosition = true;
        }

        private void Update()
        {
            if (animatedVisual == null)
                return;

            Vector3 frameVelocity = Vector3.zero;
            if (hasPreviousPosition && Time.deltaTime > 0f)
                frameVelocity = (transform.position - previousPosition) / Time.deltaTime;

            previousPosition = transform.position;
            hasPreviousPosition = true;
            frameVelocity.y = 0f;

            if (frameVelocity.sqrMagnitude >= movementThreshold * movementThreshold)
                AnimateWalk(frameVelocity.magnitude);
            else
                SetIdlePose();
        }

        private void ResolveCharacterRig()
        {
            if (animatedVisual == null)
            {
                Transform character = transform.Find("AnimatedPlayer");
                animatedVisual = character != null
                    ? character.Find("AnimatedVisual")
                    : transform.Find("AnimatedVisual");
            }

            if (animatedVisual == null)
                return;

            leftArm = FindRigTransform("LeftArm", leftArm);
            rightArm = FindRigTransform("RightArm", rightArm);
            leftLeg = FindRigTransform("LeftLeg", leftLeg);
            rightLeg = FindRigTransform("RightLeg", rightLeg);
        }

        private Transform FindRigTransform(string targetName, Transform current)
        {
            if (current != null && current.IsChildOf(animatedVisual))
                return current;

            foreach (Transform child in animatedVisual.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == targetName)
                    return child;
            }

            Debug.LogWarning($"PlayerCharacterAnimator could not find {targetName} under {animatedVisual.name}.", this);
            return null;
        }

        private void AnimateWalk(float movementSpeed)
        {
            float speedScale = Mathf.Clamp(movementSpeed / 4f, 0.6f, 1.5f);
            phase += Time.deltaTime * walkFrequency * speedScale;
            float stride = Mathf.Sin(phase);

            SetLimbRotation(leftArm, stride * swingAngle);
            SetLimbRotation(rightArm, -stride * swingAngle);
            SetLimbRotation(leftLeg, -stride * swingAngle);
            SetLimbRotation(rightLeg, stride * swingAngle);

            animatedVisual.localPosition = visualStartPosition
                + Vector3.up * (Mathf.Abs(stride) * bobHeight);
        }

        private void SetIdlePose()
        {
            phase = 0f;
            SetLimbRotation(leftArm, 0f);
            SetLimbRotation(rightArm, 0f);
            SetLimbRotation(leftLeg, 0f);
            SetLimbRotation(rightLeg, 0f);
            animatedVisual.localPosition = Vector3.Lerp(
                animatedVisual.localPosition,
                visualStartPosition,
                Time.deltaTime * 8f);
        }

        private static void SetLimbRotation(Transform limb, float angle)
        {
            if (limb != null)
                limb.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}