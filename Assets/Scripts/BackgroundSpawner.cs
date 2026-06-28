using UnityEngine;

public class BackgroundSpawner : MonoBehaviour
{
    public GameObject[] prefabs;
    public int count = 20;
    public int tileScale = 2;

    public Vector2 xRange = new Vector2(-10, 10);
    public Vector2 yRange = new Vector2(-5, 5);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < count; i++) {
            GameObject obj = Instantiate(
                prefabs[Random.Range(0, prefabs.Length)],
                transform
            );

            obj.transform.position = new Vector3(
                Random.Range(xRange.x, xRange.y),
                Random.Range(yRange.x, yRange.y),
                0
            );

            obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            obj.transform.localScale = Vector3.one * tileScale;

            obj.AddComponent<SlowRotate>().speed = Random.Range(-120f, 120f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
