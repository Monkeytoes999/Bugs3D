using System.Collections;
using System.Collections.Generic;
using Unity.Bugs.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Bugs.Gameplay {
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerController : MonoBehaviour {

        [Header("References")] [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;

        [Tooltip("Audio source for footsteps, jump, etc...")]
        public AudioSource AudioSource;

        [Header("General")] [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")] [Tooltip("Max movement speed when grounded")]
        public float MaxSpeed = 10f;

        [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")] [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Header("Stance")] [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.3f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 1.8f;

        [Header("Audio")] [Tooltip("Amount of footstep sounds played when moving one meter")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("Sound played for footsteps")]
        public AudioClip FootstepSfx;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool IsDead { get; private set; }

        public float RotationMultiplier
        {
            get
            {
                return 1f;
            }
        }

        Health m_Health;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        Actor m_Actor;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        float m_CameraVerticalAngle = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;

        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake() {
            ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
            if (actorsManager != null) actorsManager.SetPlayer(gameObject);
        }

        void Start() {
            m_Controller = GetComponent<CharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller, this, gameObject);

            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler, this, gameObject);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

            m_Controller.enableOverlapRecovery = true;

            m_Health.OnDie += OnDie;

            UpdateCharacterHeight(true);
        }

        void Update() {
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            bool wasGrounded = IsGrounded;
            GroundCheck();

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie() {
            IsDead = true;
            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck() {
            float chosenGroundCheckDistance = IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                QueryTriggerInteraction.Ignore))
            {
                m_GroundNormal = hit.normal;

                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(m_GroundNormal))
                {
                    IsGrounded = true;

                    if (hit.distance > m_Controller.skinWidth)
                    {
                        m_Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }

        void HandleCharacterMovement()
        {
            {
                transform.Rotate(new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier), 0f), Space.Self);
            }

            {
                m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
                PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            }

            {
                Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());
                if (IsGrounded) {
                    Vector3 targetVelocity = worldspaceMoveInput * MaxSpeed;
                    targetVelocity.y = 0;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;
                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity, MovementSharpnessOnGround * Time.deltaTime);

                    if (m_FootstepDistanceCounter >= 1f / FootstepSfxFrequency)
                    {
                        m_FootstepDistanceCounter = 0f;
                        AudioSource.PlayOneShot(FootstepSfx);
                    }

                    m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
                } else {
                    CharacterVelocity += worldspaceMoveInput * Time.deltaTime;

                    float verticalVelocity = CharacterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeed);
                    CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
                }
            }

            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);
        }

        bool IsNormalUnderSlopeLimit(Vector3 normal) {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        Vector3 GetCapsuleBottomHemisphere() {
            return transform.position + (transform.up * m_Controller.radius);
        }

        Vector3 GetCapsuleTopHemisphere(float atHeight) {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal) {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force) {
            if (force) {
                m_TargetCharacterHeight = CapsuleHeightStanding;
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
        }
    }
}