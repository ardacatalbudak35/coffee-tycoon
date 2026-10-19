using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.65f, 0.2f);
    public float followSpeed = 10f;
    public float lookHeight = 1.2f;

    private void LateUpdate()
    {
        if (target == null) return;
        // Birinci şahıs kamera: göz hizasında oyuncunun baktığı yöne kilitlenir.
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        Vector3 lookDirection = target.forward + Vector3.up * 0.02f;
        if (lookDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), followSpeed * Time.deltaTime);
    }
}
