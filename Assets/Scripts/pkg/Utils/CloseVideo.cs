using UnityEngine;

public class CloseVideo : MonoBehaviour
{
    public void Close()
    {
        Destroy(gameObject); // Destroy the video prefab instance
    }
}