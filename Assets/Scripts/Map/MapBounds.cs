using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MapBounds : MonoBehaviour
{
    [Header("Computed world-space bounds of attached RectTransform")]
    public Vector2 min;   // Bottom-left (world-space X/Y)
    public Vector2 max;   // Top-right (world-space X/Y)

    private RectTransform rectTransform;
    private static readonly Vector3[] worldCorners = new Vector3[4];

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateBounds();
    }

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateBounds();
    }

    private void Update()
    {
#if UNITY_EDITOR
        // In Edit mode, keep bounds updated as you move/resize the UI
        if (!Application.isPlaying)
        {
            UpdateBounds();
        }
#endif
    }

    /// <summary>
    /// Recalculate min/max from the attached RectTransform's world corners.
    /// </summary>
    public void UpdateBounds()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.GetWorldCorners(worldCorners);

        // worldCorners order: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        float minX = worldCorners[0].x;
        float minY = worldCorners[0].y;
        float maxX = worldCorners[0].x;
        float maxY = worldCorners[0].y;

        for (int i = 1; i < 4; i++)
        {
            Vector3 c = worldCorners[i];
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }

        min = new Vector2(minX, minY);
        max = new Vector2(maxX, maxY);
    }

    /// <summary>
    /// Clamp a world-space position to the RectTransform's current world bounds.
    /// </summary>
    public Vector3 ClampPosition(Vector3 position)
    {
        // Ensure bounds are current
        UpdateBounds();

        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.y = Mathf.Clamp(position.y, min.y, max.y);
        return position;
    }

    /// <summary>
    /// Get the current world-space Rect of the attached RectTransform.
    /// </summary>
    public Rect GetRect()
    {
        // Ensure bounds are current
        UpdateBounds();

        Vector2 size = max - min;
        return new Rect(min, size);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UpdateBounds();

        Gizmos.color = Color.yellow;
        Rect rect = GetRect();
        Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f);
        Vector3 size = new Vector3(rect.size.x, rect.size.y, 0f);

        Gizmos.DrawWireCube(center, size);
    }
#endif
}
