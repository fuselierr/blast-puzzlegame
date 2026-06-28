using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    public float speed = 60f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
