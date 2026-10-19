using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public Transform counterPoint;
    public float spawnEvery = 7f;
    public int maxCustomers = 4;
    private float timer = 2f;

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnEvery;
        if (FindObjectsByType<CustomerAI>().Length >= maxCustomers) return;
        SpawnCustomer();
    }

    private void SpawnCustomer()
    {
        GameObject customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        customer.name = "Customer";
        customer.transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
        customer.transform.localScale = new Vector3(0.45f, 0.75f, 0.45f);
        Renderer renderer = customer.GetComponent<Renderer>();
        renderer.material.color = new Color(Random.Range(0.2f, 0.8f), Random.Range(0.25f, 0.7f), Random.Range(0.3f, 0.9f));
        CustomerAI ai = customer.AddComponent<CustomerAI>();
        ai.target = counterPoint;
    }
}
