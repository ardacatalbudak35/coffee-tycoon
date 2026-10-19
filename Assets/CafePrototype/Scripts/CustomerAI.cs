using UnityEngine;

public class CustomerAI : MonoBehaviour
{
    public Transform target;
    public float speed = 1.8f;
    private float wait = 4f;

    private void Update()
    {
        if (target == null) { Destroy(gameObject, 2f); return; }
        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude > 0.35f)
        {
            transform.position += flat.normalized * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(flat.normalized);
            return;
        }
        wait -= Time.deltaTime;
        if (wait <= 0f)
        {
            CafeGameManager.Instance?.AddMoney(5);
            Destroy(gameObject);
        }
    }
}
