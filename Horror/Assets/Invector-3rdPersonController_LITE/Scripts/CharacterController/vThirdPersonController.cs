using UnityEngine;

namespace Invector.vCharacterController
{
    public class vThirdPersonController : vThirdPersonAnimator
    {
        // Reference to the PlayerController to check equipping state
        private PlayerController playerController;

        private void Awake()
        {
            // Find the PlayerController component on the same GameObject
            playerController = GetComponent<PlayerController>();
        }

         private void Update()
        {
            UpdateAnimatorParameters();
        }

        private void UpdateAnimatorParameters()
        {
            if (playerController == null) return;

            // Проверяем, держит ли игрок меч
            if (playerController.isEquipped)
            {
                animator.SetBool("IsEquipped", true);
            }
            else
            {
                animator.SetBool("IsEquipped", false);
            }
        }

        public void ControlAnimatorRootMotion()
        {
            if (playerController != null && (playerController.isEquipping || playerController.isBlocking || playerController.isKicking || playerController.isAttacking))
            {
                
                if (useRootMotion)
                {
                    animator.applyRootMotion = false; // Отключение применения root motion от анимации
                }
                moveDirection = Vector3.zero; // Зануляем направление движения
                inputSmooth = Vector3.zero; // Зануляем сглаженное значение ввода
                animator.SetFloat("InputHorizontal", 0); // Зануляем горизонтальное значение
                animator.SetFloat("InputVertical", 0); // Зануляем вертикальное значение
                return; // Прерываем выполнение метода
            }

            if (!this.enabled) return;

            if (inputSmooth == Vector3.zero)
            {
                transform.position = animator.rootPosition;
                transform.rotation = animator.rootRotation;
            }

            if (useRootMotion)
                MoveCharacter(moveDirection);
        }


        public void ControlLocomotionType() // Removed 'override'
        {
            // Prevent movement when equipping
            if (playerController != null && playerController.isEquipping) return;

            if (lockMovement) return;

            if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
            {
                SetControllerMoveSpeed(freeSpeed);
                SetAnimatorMoveSpeed(freeSpeed);
            }
            else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
            {
                isStrafing = true;
                SetControllerMoveSpeed(strafeSpeed);
                SetAnimatorMoveSpeed(strafeSpeed);
            }

            if (!useRootMotion)
                MoveCharacter(moveDirection);
        }

        public void ControlRotationType() // Removed 'override'
        {
            // Prevent rotation when equipping
            if (playerController != null && playerController.isEquipping) return;

            if (lockRotation) return;

            bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);

            if (validInput)
            {
                // Calculate input smooth
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        public void UpdateMoveDirection(Transform referenceTransform = null) // Removed 'override'
        {
            // Prevent move direction updates when equipping
            if (playerController != null && playerController.isEquipping) return;

            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                // Get the right-facing direction of the referenceTransform
                var right = referenceTransform.right;
                right.y = 0;
                // Get the forward direction relative to referenceTransform Right
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                // Determine the direction the player will face based on input and the referenceTransform's right and forward directions
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        public void Sprint(bool value)
        {
            // Prevent sprinting when equipping
            if (playerController != null && playerController.isEquipping) return;

            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

            if (value && sprintConditions)
            {
                if (input.sqrMagnitude > 0.1f)
                {
                    if (isGrounded && useContinuousSprint)
                    {
                        isSprinting = !isSprinting;
                    }
                    else if (!isSprinting)
                    {
                        isSprinting = true;
                    }
                }
                else if (!useContinuousSprint && isSprinting)
                {
                    isSprinting = false;
                }
            }
            else if (isSprinting)
            {
                isSprinting = false;
            }
        }

        public void Strafe()
        {
            // Prevent strafing when equipping
            if (playerController != null && playerController.isEquipping) return;

            isStrafing = !isStrafing;
        }

        public void Jump()
        {
            // Prevent jumping when equipping
            if (playerController != null && playerController.isEquipping) return;

            // Trigger jump behaviour
            jumpCounter = jumpTimer;
            isJumping = true;

            // Trigger jump animations
            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }
    }
}