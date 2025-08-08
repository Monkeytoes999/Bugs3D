using Unity.Bugs.Game;
using UnityEngine;

namespace Unity.Bugs.Gameplay
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Tooltip("Sensitivity multiplier for moving the camera around")]
        public float LookSensitivity = 1f;

        [Tooltip("Additional sensitivity multiplier for WebGL")]
        public float WebglLookSensitivityMultiplier = 0.25f;

        [Tooltip("Limit to consider an input when using a trigger on a controller")]
        public float TriggerAxisThreshold = 0.4f;

        [Tooltip("Used to flip the vertical input axis")]
        public bool InvertYAxis = false;

        [Tooltip("Used to flip the horizontal input axis")]
        public bool InvertXAxis = false;

        GameLoop m_gameLoop;
        PlayerController m_PlayerController;
        bool m_FireInputWasHeld;

        void Start()
        {
            m_PlayerController = GetComponent<PlayerController>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerController, PlayerInputHandler>(m_PlayerController, this, gameObject);

            m_gameLoop = FindObjectOfType<GameLoop>();
            DebugUtility.HandleErrorIfNullFindObject<GameLoop, PlayerInputHandler>(m_gameLoop, this);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            m_FireInputWasHeld = GetFireInputHeld();
        }

        public bool CanProcessInput()
        {
            return Cursor.lockState == CursorLockMode.Locked && !m_gameLoop.gameOver;
        }

        public Vector3 GetMoveInput()
        {
            if (CanProcessInput())
            {
                Vector3 move = new Vector3(Input.GetAxisRaw(GameConstants.k_AxisNameHorizontal), 0f, Input.GetAxisRaw(GameConstants.k_AxisNameVertical));

                move = Vector3.ClampMagnitude(move, 1);

                return move;
            }

            return Vector3.zero;
        }

        public float GetLookInputsHorizontal()
        {
            return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameHorizontal, GameConstants.k_AxisNameJoystickLookHorizontal);
        }

        public float GetLookInputsVertical()
        {
            return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameVertical, GameConstants.k_AxisNameJoystickLookVertical);
        }

        public bool GetJumpInputDown()
        {
            if (CanProcessInput())
            {
                return Input.GetButtonDown(GameConstants.k_ButtonNameJump);
            }

            return false;
        }

        public bool GetJumpInputHeld()
        {
            if (CanProcessInput())
            {
                return Input.GetButton(GameConstants.k_ButtonNameJump);
            }

            return false;
        }

        public bool GetFireInputDown()
        {
            return GetFireInputHeld() && !m_FireInputWasHeld;
        }

        public bool GetFireInputReleased()
        {
            return !GetFireInputHeld() && m_FireInputWasHeld;
        }

        public bool GetFireInputHeld()
        {
            if (CanProcessInput())
            {
                bool isGamepad = Input.GetAxis(GameConstants.k_ButtonNameGamepadFire) != 0f;
                if (isGamepad)
                {
                    return Input.GetAxis(GameConstants.k_ButtonNameGamepadFire) >= TriggerAxisThreshold;
                }
                else
                {
                    return Input.GetButton(GameConstants.k_ButtonNameFire);
                }
            }

            return false;
        }

        float GetMouseOrStickLookAxis(string mouseInputName, string stickInputName)
        {
            //if (CanProcessInput())
            {
                if (m_gameLoop.gameOver) {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                }

                // Check if this look input is coming from the mouse
                bool isGamepad = Input.GetAxis(stickInputName) != 0f;
                float i = isGamepad ? Input.GetAxis(stickInputName) : Input.GetAxisRaw(mouseInputName);

                // handle inverting vertical input
                if (InvertYAxis)
                    i *= -1f;

                // apply sensitivity multiplier
                i *= LookSensitivity;

                if (isGamepad)
                {
                    // since mouse input is already deltaTime-dependant, only scale input with frame time if it's coming from sticks
                    i *= Time.deltaTime;
                }
                else
                {
                    // reduce mouse input amount to be equivalent to stick movement
                    i *= 0.01f;
#if UNITY_WEBGL
                    // Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
                    i *= WebglLookSensitivityMultiplier;
#endif
                }

                return i;
            }

            //return 0f;
        }
    }
}