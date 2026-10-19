using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    public Transform player;
    public float eyeHeight = 1.65f;
    public float sensitivity = 0.08f;
    public float maxLookUp = 80f;
    public float maxLookDown = 80f;
    private float yaw;
    private float pitch;

    private void Start()
    {
        if (player != null) yaw = player.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (player == null) return;
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -maxLookDown, maxLookUp);
        }
        player.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.position = player.position + Vector3.up * eyeHeight;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
