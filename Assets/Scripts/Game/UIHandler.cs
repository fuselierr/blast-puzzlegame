using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GridSpawner gridSpawner;
    public void Detonate()
    {
        gridSpawner.DetonateMines();
    }
}
