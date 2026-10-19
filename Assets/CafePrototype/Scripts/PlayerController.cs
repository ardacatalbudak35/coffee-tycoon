using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    public Transform cameraTransform;
    public MobileJoystick joystick;
    public bool firstPersonLook = true;
    public Interactable CurrentInteractable { get; private set; }
    private CharacterController controller;
    private float verticalVelocity;

    private void Awake() => controller = GetComponent<CharacterController>();

    private void Update()
    {
        Vector2 input = joystick != null ? joystick.Input : Vector2.zero;
        if (input.sqrMagnitude < 0.01f)
            input = ReadKeyboardInput();
        Vector3 forward = cameraTransform != null ? Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        right.y = 0f; right.Normalize();
        Vector3 move = (forward * input.y + right * input.x);
        if (move.sqrMagnitude > 1f) move.Normalize();
        controller.Move(move * moveSpeed * Time.deltaTime);
        if (!firstPersonLook && move.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), rotationSpeed * Time.deltaTime);
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        ScanForInteraction();
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Interact();
    }

    public Vector3 MovementInput => new Vector3(ReadKeyboardInput().x, 0f, ReadKeyboardInput().y);

    private Vector2 ReadKeyboardInput()
    {
        if (Keyboard.current == null) return Vector2.zero;
        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private void ScanForInteraction()
    {
        CurrentInteractable = null;
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 0.8f, 1.6f);
        float best = float.MaxValue;
        foreach (Collider hit in hits)
        {
            Interactable candidate = hit.GetComponentInParent<Interactable>();
            if (candidate == null) continue;
            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < best) { best = distance; CurrentInteractable = candidate; }
        }
        CafeHUD.Instance?.SetInteraction(CurrentInteractable == null ? string.Empty : CurrentInteractable.GetHint());
    }

    public void Interact()
    {
        if (CurrentInteractable != null) CurrentInteractable.Interact(this);
    }
}
