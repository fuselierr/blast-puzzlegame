using UnityEngine;

public class ClassicStone : MonoBehaviour
{
    private bool isDestroyed = false;

    public void BreakStone()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;
        gameObject.SetActive(false);
    }
}
