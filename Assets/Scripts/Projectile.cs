using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 4f;
    private bool hasHit;

    private const float destroyDistance = 15f;
    [SerializeField] private GameObject trailPrefab;
    private GameObject trailInstance;

    public void Init(Vector2 dir)
    {
        direction = dir;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (trailInstance != null)
        {
            trailInstance.transform.position = transform.position;
        }

        if (!hasHit)
        {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, 0.1f);
            foreach (var overlap in overlaps)
            {
                HandleCollision(overlap.gameObject);
                if (hasHit)
                {
                    break;
                }
            }
        }

        if (Mathf.Abs(transform.position.x) > destroyDistance ||
            Mathf.Abs(transform.position.y) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (trailPrefab != null)
        {
            trailInstance = Instantiate(trailPrefab, transform.position, Quaternion.identity);
            trailInstance.transform.SetParent(null, true);
            trailInstance.transform.position = transform.position;
            DontDestroyOnLoad(trailInstance);
        }
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (hasHit) return;

        ClassicStone stone = other.GetComponentInParent<ClassicStone>();

        if (stone != null)
        {
            stone.BreakStone();
            return;
        }
        Tile tile = other.GetComponent<Tile>();
        if (tile != null && tile.Type == TileType.Debris)
        {
            hasHit = true;
            StartCoroutine(ExplodeAtCenter(tile));
        }
    }

    private System.Collections.IEnumerator ExplodeAtCenter(Tile tile)
    {
        transform.position = tile.transform.position;
        tile.ExplodeDebris();
        yield return new WaitForSeconds(0.05f);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (trailInstance == null) return;

        var ps = trailInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            // Give remaining particles time to fade, then actually destroy it —
            // otherwise it's DontDestroyOnLoad with nothing ever destroying it,
            // leaking one trail object per shot for the life of the app.
            Destroy(trailInstance, main.startLifetime.constantMax + 0.5f);
        }
        else
        {
            Destroy(trailInstance);
        }
    }
}