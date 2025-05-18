using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[AddComponentMenu("XR/Locomotion/Free Move Provider")]
[HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit")]
public class FreeMoveProvider : LocomotionProvider
{
    public XRRayInteractor rightRayInteractor;
    public XRRayInteractor leftRayInteractor;

    [Header("Input Actions")]
    [SerializeField]
    private InputActionProperty m_VerticalMoveAction;

    [SerializeField]
    private InputActionProperty m_TurnAction;

    [SerializeField]
    private InputActionProperty m_HorizontalMoveAction;

    [SerializeField]
    private InputActionProperty m_SpeedToggleAction;

    [Header("Transforms")]
    [SerializeField]
    private Transform m_RigTransform;

    [SerializeField]
    private Transform m_FaceDirection;

    [Header("Speed Settings")]
    [SerializeField]
    private float m_SlowSpeed = 1.0f;

    [SerializeField]
    private float m_FastSpeed = 3.0f;

    [SerializeField]
    private float m_SlowTurnSpeed = 40f;

    [SerializeField]
    private float m_FastTurnSpeed = 60f;

    private CharacterController characterController;
    private bool m_IsFastMode = false;
    
    float gimbalAngleX = 0f; 

    private void OnEnable()
    {
        m_VerticalMoveAction.action?.Enable();
        m_HorizontalMoveAction.action?.Enable();
        m_SpeedToggleAction.action?.Enable();
        m_TurnAction.action?.Enable();
        m_SpeedToggleAction.action.performed += OnSpeedToggle;
    }

    private void OnDisable()
    {
        m_VerticalMoveAction.action?.Disable();
        m_HorizontalMoveAction.action?.Disable();
        m_SpeedToggleAction.action?.Disable();
        m_TurnAction.action?.Disable();
        m_SpeedToggleAction.action.performed -= OnSpeedToggle;
    }

    private void Start()
    {
        if (m_RigTransform != null)
            characterController = m_RigTransform.GetComponent<CharacterController>();

        if (characterController == null)
            Debug.LogWarning("CharacterController not found on rigTransform.");
    }

    private void OnSpeedToggle(InputAction.CallbackContext context)
    {
        m_IsFastMode = !m_IsFastMode;
    }

    private void Update()
    {
        Vector2 horizontalInput = m_HorizontalMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        float verticalInput = m_VerticalMoveAction.action?.ReadValue<Vector2>().y ?? 0f;
        float turnInput = m_TurnAction.action?.ReadValue<Vector2>().x ?? 0f;

        var target = rightRayInteractor.selectTarget ?? leftRayInteractor.selectTarget;

        float speed = m_IsFastMode ? m_FastSpeed : m_SlowSpeed;

        if (target != null)
        {
            var gimbal = target.transform.Find("Gimbal");

            if (gimbal != null)
            {
                float gimbalRotationSpeed = 20f;
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    // Update the manually tracked angle
                    gimbalAngleX -= verticalInput * gimbalRotationSpeed * Time.deltaTime;
                    gimbalAngleX = Mathf.Clamp(gimbalAngleX, -80f, 80f);

                    // Apply rotation
                    gimbal.localEulerAngles = new Vector3(gimbalAngleX, 0f, 0f);
                }
            }

            float turnRotationSpeed = 30f;  
            if (Mathf.Abs(turnInput) > 0.1f)
            {
                target.transform.Rotate(0f, turnInput * turnRotationSpeed * Time.deltaTime, 0f);
            }

        }
        else
        {
            if (!CanBeginLocomotion() || m_RigTransform == null || characterController == null || m_FaceDirection == null)
                return;

            Vector3 move = Vector3.zero;

            if (Mathf.Abs(verticalInput) > 0.2f)
            {
                move += m_FaceDirection.up * verticalInput * speed;
            }

            if (horizontalInput != Vector2.zero)
            {
                Vector3 forward = m_FaceDirection.forward;
                Vector3 right = m_FaceDirection.right;

                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                move += (forward * horizontalInput.y + right * horizontalInput.x) * speed;
            }

            if (move != Vector3.zero && BeginLocomotion())
            {
                characterController.Move(move * Time.deltaTime);
                EndLocomotion();
            }

            if (Mathf.Abs(turnInput) > 0.2f)
            {
                float turnSpeed = m_IsFastMode ? m_FastTurnSpeed : m_SlowTurnSpeed;
                m_RigTransform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);
            }
        }
    }
}
