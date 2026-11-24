using System.Collections;
using ChronowalkGame.Game.Data;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class ActorAnimation : MonoBehaviour
    {
        [SerializeField] private Transform actorRoot;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float animationSpeedMultiplier = 1f;

        private bool movementAnimationFinished;
        private bool isMoving;
        private float baseAnimatorSpeed = 1f;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (actorRoot == null)
                actorRoot = transform.parent != null ? transform.parent : transform;
            if (animator != null)
                baseAnimatorSpeed = animator.speed;
        }

        public void Configure(Transform root)
        {
            actorRoot = root != null ? root : transform;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator != null)
                    baseAnimatorSpeed = animator.speed;
            }
        }

        public void SetIdle(Direction direction)
        {
            isMoving = false;
            movementAnimationFinished = true;
            if (animator == null || !animator.isActiveAndEnabled)
                return;

            ApplyAnimationSpeed();
            PlayState($"Idle{direction}");
        }

        public IEnumerator MoveTo(Vector3 destination, Direction direction)
        {
            if (actorRoot == null)
            {
                Debug.LogError("ActorAnimation requires an actor root transform.", this);
                yield break;
            }

            Vector3 start = actorRoot.position;
            if ((destination - start).sqrMagnitude <= Mathf.Epsilon)
            {
                actorRoot.position = destination;
                yield break;
            }

            string moveStateName = $"Walk{direction}";
            int moveState = Animator.StringToHash($"Base Layer.{moveStateName}");
            int moveShortName = Animator.StringToHash(moveStateName);
            bool waitForAnimation = animator != null && animator.isActiveAndEnabled &&
                                    animator.HasState(0, moveState);
            if (!waitForAnimation)
            {
                Debug.LogError($"Animator is missing the Walk{direction} state.", this);
                actorRoot.position = destination;
                yield break;
            }

            ApplyAnimationSpeed();
            animator.Play(moveState, 0, 0f);
            animator.Update(0f);
            movementAnimationFinished = false;
            isMoving = true;

            try
            {
                while (!movementAnimationFinished)
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.shortNameHash == moveShortName)
                    {
                        actorRoot.position = Vector3.Lerp(
                            start,
                            destination,
                            Mathf.Clamp01(stateInfo.normalizedTime));
                    }

                    if (stateInfo.shortNameHash == moveShortName && stateInfo.normalizedTime >= 1f)
                        movementAnimationFinished = true;

                    yield return null;
                }

                actorRoot.position = destination;
            }
            finally
            {
                isMoving = false;
            }
        }

        public void Interrupt(Direction idleDirection)
        {
            isMoving = false;
            movementAnimationFinished = true;
            SetIdle(idleDirection);
        }

        public void OnMovementAnimationFinished()
        {
            if (isMoving)
                movementAnimationFinished = true;
        }

        private void PlayState(string stateName)
        {
            int stateHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogError($"Animator is missing the {stateName} state.", this);
                return;
            }

            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }

        private void ApplyAnimationSpeed()
        {
            if (animator != null)
                animator.speed = baseAnimatorSpeed * animationSpeedMultiplier;
        }
    }
}
