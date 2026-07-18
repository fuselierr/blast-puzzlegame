using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;

    public float spawnInterval = 0.75f;
    public float speed = 4f;

    public float projScale = 2f;

    public float xExtent = 10f;
    public float yExtent = 6f;
    public float spawnOffset = 1f;

    private float timer;

    private readonly Vector2[] directions =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1,1).normalized,
        new Vector2(1,-1).normalized,
        new Vector2(-1,1).normalized,
        new Vector2(-1,-1).normalized
    };

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnProjectile();
        }
    }

    void SpawnProjectile()
    {
        Vector2 direction = directions[Random.Range(0, directions.Length)];

        Vector2 spawnPos = GetSpawnPosition(direction);

        GameObject obj = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.identity,
            transform
        );

        Projectile projectile = obj.AddComponent<Projectile>();
        projectile.direction = direction;
        projectile.speed = speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        obj.transform.localScale = Vector3.one * projScale;
        obj.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    Vector2 GetSpawnPosition(Vector2 direction)
    {
        if (direction == Vector2.up)
            return new Vector2(Random.Range(-xExtent, xExtent), -yExtent - spawnOffset);

        if (direction == Vector2.down)
            return new Vector2(Random.Range(-xExtent, xExtent), yExtent + spawnOffset);

        if (direction == Vector2.left)
            return new Vector2(xExtent + spawnOffset, Random.Range(-yExtent, yExtent));

        if (direction == Vector2.right)
            return new Vector2(-xExtent - spawnOffset, Random.Range(-yExtent, yExtent));

        if (direction.x > 0 && direction.y > 0)
            return new Vector2(-xExtent - spawnOffset, -yExtent - spawnOffset);

        if (direction.x > 0 && direction.y < 0)
            return new Vector2(-xExtent - spawnOffset, yExtent + spawnOffset);

        if (direction.x < 0 && direction.y > 0)
            return new Vector2(xExtent + spawnOffset, -yExtent - spawnOffset);

        return new Vector2(xExtent + spawnOffset, yExtent + spawnOffset);
    }
}