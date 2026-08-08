using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private static float transitionSpeedMultiplier = 1f;

    // Clockwise compass directions. A splitter sends a projectile into the
    // two neighboring directions, which are exactly 45 degrees away.
    private static readonly Vector2[] eightDirections =
    {
        Vector2.up,
        new Vector2(1f, 1f).normalized,
        Vector2.right,
        new Vector2(1f, -1f).normalized,
        Vector2.down,
        new Vector2(-1f, -1f).normalized,
        Vector2.left,
        new Vector2(-1f, 1f).normalized
    };

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(1)] private int maxSplitGenerations = 6;
    public Vector2 direction;
    public float speed = 4f;
    private bool hasHit;
    private Tile ignoredTeleporter;
    private HashSet<int> triggeredSplitterIds = new HashSet<int>();
    private int splitGeneration;
    private bool hasEnteredScreen;

    public bool HasEnteredScreen => hasEnteredScreen;

    private const float destroyDistance = 15f;
    private const float pathHitTolerance = 0.15f;
    private const float teleporterIgnoreDuration = 0.1f;
    private Vector2 launchPosition;
    private float teleporterIgnoreTimer;
    private Tile lastTeleporter;

    [SerializeField] private GameObject trailPrefab;
    private GameObject trailInstance;

    public static void SetTransitionSpeedMultiplier(float multiplier)
    {
        transitionSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public static void ResetTransitionSpeedMultiplier()
    {
        transitionSpeedMultiplier = 1f;
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        hasHit = false;
        launchPosition = transform.position;
        triggeredSplitterIds.Clear();
        splitGeneration = 0;
        // Important when gameObject is used as the fallback spawn source: do
        // not inherit ownership of the original projectile's trail instance.
        trailInstance = null;
    }

    void Update()
    {
        Vector2 previousPosition = transform.position;
        float currentSpeed = speed * transitionSpeedMultiplier;
        transform.position +=
            (Vector3)(direction * currentSpeed * Time.deltaTime);

        RefreshScreenEntryState();
        if (teleporterIgnoreTimer > 0f)
        {
            teleporterIgnoreTimer -= Time.deltaTime;
            if (teleporterIgnoreTimer < 0f)
            {
                teleporterIgnoreTimer = 0f;
            }
        }
        ClearIgnoredTileAfterExit(ref ignoredTeleporter);

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
        launchPosition = transform.position;
        RefreshScreenEntryState();

        if (trailPrefab != null)
        {
            trailInstance = Instantiate(trailPrefab, transform.position, Quaternion.identity);
            trailInstance.transform.SetParent(null, true);
            trailInstance.transform.position = transform.position;
            DontDestroyOnLoad(trailInstance);
        }
    }

    public void RefreshScreenEntryState()
    {
        if (hasEnteredScreen)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 viewportPosition =
            mainCamera.WorldToViewportPoint(transform.position);

        hasEnteredScreen =
            viewportPosition.z > 0f &&
            viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f;
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
        Tile tile = other.GetComponentInParent<Tile>();
        if (tile != null && (tile.Type == TileType.Debris || tile.Type == TileType.StrongDebris) && IsOnShotPath(tile.transform.position))
        {
            hasHit = true;
            StartCoroutine(ExplodeAtCenter(tile));
        }
        else if (tile != null && tile.Type == TileType.Wall && IsOnShotPath(tile.transform.position))
        {
            hasHit = true;
            StartCoroutine(ExplodeAtCenter(tile));
        }
        else if (tile != null && tile.Type == TileType.Teleporter && IsOnShotPath(tile.transform.position))
        {
            if (tile == ignoredTeleporter || (teleporterIgnoreTimer > 0f && tile == lastTeleporter))
            {
                return;
            }

            TeleportThrough(tile);
        }
        else if (tile != null && tile.Type == TileType.Splitter && IsOnShotPath(tile.transform.position))
        {
            int splitterId = tile.GetInstanceID();

            // A splitter may only affect a projectile lineage once. Children
            // inherit this set, preventing splitter/teleporter loops from
            // producing exponential clone storms.
            if (triggeredSplitterIds.Contains(splitterId))
            {
                return;
            }

            triggeredSplitterIds.Add(splitterId);

            // At the cap, let this projectile pass through without splitting.
            if (splitGeneration >= maxSplitGenerations)
            {
                return;
            }

            hasHit = true;
            SplitAt(tile);
        }
    }

    private void ClearIgnoredTileAfterExit(ref Tile ignoredTile)
    {
        if (ignoredTile == null)
        {
            return;
        }

        Collider2D tileCollider = ignoredTile.GetComponentInChildren<Collider2D>();
        if (tileCollider == null || !tileCollider.bounds.Contains(transform.position))
        {
            ignoredTile = null;
        }
    }

    private void TeleportThrough(Tile entrance)
    {
        Tile exit = entrance.TeleporterPartner;
        if (exit == null)
        {
            Debug.LogWarning(
                $"Teleporter '{entrance.TeleporterPairId}' has no partner.",
                entrance);
            return;
        }

        // Preserve the shot direction. The exit tile is ignored until this
        // projectile has left it, preventing an immediate teleport back.
        transform.position = exit.transform.position;
        launchPosition = exit.transform.position;
        ignoredTeleporter = exit;
        lastTeleporter = exit;
        teleporterIgnoreTimer = teleporterIgnoreDuration;

        if (trailInstance != null)
        {
            trailInstance.transform.position = transform.position;
        }
    }

    private void SplitAt(Tile splitter)
    {
        if (splitter == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 splitPosition = splitter.transform.position;
        int directionIndex = GetClosestDirectionIndex(direction);
        int previousIndex =
            (directionIndex - 1 + eightDirections.Length) %
            eightDirections.Length;
        int nextIndex =
            (directionIndex + 1) % eightDirections.Length;

        Vector2 leftDirection = eightDirections[previousIndex];
        Vector2 rightDirection = eightDirections[nextIndex];

        SpawnSplitProjectile(splitPosition, leftDirection, splitter);
        SpawnSplitProjectile(splitPosition, rightDirection, splitter);
        Destroy(gameObject);
    }

    private static int GetClosestDirectionIndex(Vector2 incomingDirection)
    {
        Vector2 normalizedDirection = incomingDirection.normalized;
        int closestIndex = 0;
        float highestDot = float.NegativeInfinity;

        for (int i = 0; i < eightDirections.Length; i++)
        {
            float dot = Vector2.Dot(
                normalizedDirection,
                eightDirections[i]);

            if (dot > highestDot)
            {
                highestDot = dot;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void SpawnSplitProjectile(
        Vector2 position,
        Vector2 newDirection,
        Tile sourceSplitter)
    {
        // Falling back to this object means the splitter still works even if
        // projectilePrefab was not assigned on the prefab in the Inspector.
        GameObject sourcePrefab = projectilePrefab != null
            ? projectilePrefab
            : gameObject;

        GameObject newProjectileObject = Instantiate(
            sourcePrefab,
            position,
            Quaternion.identity);

        Projectile newProjectile = newProjectileObject.GetComponent<Projectile>();
        if (newProjectile != null)
        {
            newProjectile.Init(newDirection);
            newProjectile.splitGeneration = splitGeneration + 1;
            newProjectile.triggeredSplitterIds =
                new HashSet<int>(triggeredSplitterIds);

            if (sourceSplitter != null)
            {
                newProjectile.triggeredSplitterIds.Add(
                    sourceSplitter.GetInstanceID());
            }
        }
        else
        {
            Destroy(newProjectileObject);
        }
    }

    private bool IsOnShotPath(Vector2 point)
    {
        Vector2 delta = point - launchPosition;
        float forwardDistance = Vector2.Dot(delta, direction.normalized);
        if (forwardDistance < 0f)
        {
            return false;
        }

        Vector2 nearest = launchPosition + direction.normalized * forwardDistance;
        float offset = Vector2.Distance(point, nearest);
        return offset <= pathHitTolerance;
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