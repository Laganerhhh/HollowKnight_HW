using UnityEngine;

[ExecuteInEditMode]
public class ParallaxEffect : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;
    public bool lockYAxis = true;

    private Vector3 startPosition;
    private Transform camTransform;

    void Start()
    {
        startPosition = transform.position;
        if (Camera.main != null)
            camTransform = Camera.main.transform;
    }

    void Update()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;

        if (camTransform != null)
        {
            Vector3 newPos = startPosition + (camTransform.position * parallaxFactor);
            if (lockYAxis)
                newPos.y = transform.position.y;
            
            transform.position = newPos;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示视差范围
        Gizmos.color = Color.cyan;
        float width = 10f * parallaxFactor;
        float height = 5f * parallaxFactor;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
    }
    #endif
}