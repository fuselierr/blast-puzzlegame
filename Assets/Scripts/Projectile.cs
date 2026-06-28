using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 4f;

    private const float destroyDistance = 15f;

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x) > destroyDistance ||
            Mathf.Abs(transform.position.y) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }
}
