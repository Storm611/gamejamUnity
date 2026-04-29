using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputActionAsset InputActions;
    [SerializeField] CharacterController controller;
    public GameObject PlayerMesh;

    private InputAction moveAction;
    private InputAction spinAction;
    private InputAction secondaryUseAction;

    private Vector2 moveVals;
    private Vector3 moveDirection;

    public float speed = 5.0f;
    public float pushForce = 10f;
    private float smoothRotation = 0.05f;
    public float gravity = -9.81f;
    private Vector3 velocity;
    private float currentRotationVelocity;

    private Inventory _inventory;
    private IWeapon _currentWeapon;

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }
    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        spinAction = InputSystem.actions.FindAction("Spin");

        if (spinAction == null)
        {
            spinAction = InputSystem.actions.FindAction("SecondaryUse");
        }

        secondaryUseAction = InputSystem.actions.FindAction("SecondaryUse");
        _inventory = GetComponent<Inventory>();
    }

    void Update()
    {
        if (spinAction != null && spinAction.triggered)
        {
            TryUseSpecialAbility();
        }

        if (secondaryUseAction != null && secondaryUseAction.triggered)
        {
            TrySecondaryUse();
        }
    }

    private void TryUseSpecialAbility()
    {
        if (_inventory == null) return;

        IPickupable currentItem = _inventory.GetCurrentItem();

        if (currentItem is Stick stick)
        {
            stick.StartSpinFromPlayer(this.gameObject);
        }
        else if (currentItem is WoodPiece)
        {
            Debug.Log("WoodPiece не имеет способности кручения!");
        }
        else
        {
            Debug.Log("У вас в руках нет оружия со специальной способностью!");
        }
    }

    private void TrySecondaryUse()
    {
        if (_inventory == null) return;

        IPickupable currentItem = _inventory.GetCurrentItem();

        if (currentItem is WoodPiece woodPiece && !woodPiece.IsBroken())
        {
            woodPiece.OnSecondaryUse(this.gameObject);
        }
    }

    private void Walking()
    {
        bool isSpinning = false;
        if (_currentWeapon is Stick stick && stick.IsSpinning())
        {
            isSpinning = true;
        }

        if (isSpinning)
        {
            moveVals = moveAction.ReadValue<Vector2>();
            if (moveVals.magnitude > 0.1f)
            {
                moveDirection = new Vector3(moveVals.x, 0, moveVals.y).normalized;
                float currentSpeed = speed * 0.5f;
                Vector3 targetPosition = moveDirection * currentSpeed * Time.fixedDeltaTime;
                controller.Move(targetPosition);
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }
        else
        {
            moveVals = moveAction.ReadValue<Vector2>();
            if (moveVals.magnitude > 0.1f)
            {
                moveDirection = new Vector3(moveVals.x, 0, moveVals.y).normalized;
                Vector3 targetPosition = moveDirection * speed * Time.fixedDeltaTime;
                controller.Move(targetPosition);
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }
    }

    private void Rotation()
    {
        if (moveDirection.magnitude > 0)
        {
            var targetangle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            var angle = Mathf.SmoothDampAngle(PlayerMesh.transform.eulerAngles.y, targetangle, ref currentRotationVelocity, smoothRotation);
            PlayerMesh.transform.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    private void FixedUpdate()
    {
        Walking();
        Rotation();
        ForceGravity();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        Vector3 pushDir = hit.gameObject.transform.position - transform.position;
        pushDir.y = 0;
        pushDir.Normalize();

        body.AddForceAtPosition(pushDir * pushForce, transform.position, ForceMode.Impulse);
    }

    private void ForceGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    // Методы для работы с IWeapon
    public void SetCurrentWeapon(IWeapon weapon)
    {
        _currentWeapon = weapon;
    }

    public void ClearCurrentWeapon()
    {
        if (_currentWeapon != null)
        {
            if (_currentWeapon is Stick stick && stick.IsSpinning())
            {
                return;
            }
            _currentWeapon = null;
        }
    }

    // Методы для обратной совместимости
    public void SetCurrentStick(Stick stick)
    {
        _currentWeapon = stick;
    }

    public void ClearCurrentStick()
    {
        ClearCurrentWeapon();
    }
}