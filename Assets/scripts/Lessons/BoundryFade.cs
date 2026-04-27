using UnityEngine;

public class BoundryFade : MonoBehaviour
{
    [SerializeField] private float fadeStartDistance = 10f;
    [SerializeField] private float fadeEndDistance = 2f;

    [Range(0f, 1f)][SerializeField] private float maxAlpha = 0.85f;

    [SerializeField] private float pulseSpeed = 2f;
    [Range(0f, 1f)] public float pulseDepth = 0.3f;

    private MeshRenderer _renderer;
    private Transform _player;
    private Color _initialColor;
    private int _colorPropID;
    private Material _instanceMaterial;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();

        _instanceMaterial = _renderer.material;
        _colorPropID = _instanceMaterial.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor") : Shader.PropertyToID("_Color");
        _initialColor = _instanceMaterial.GetColor(_colorPropID);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _player = player.transform;
    }

    void FixedUpdate()
    {
        if (_player == null || _instanceMaterial == null) return;

        Vector3 closestPoint = _renderer.bounds.ClosestPoint(_player.position);
        float dist = Vector3.Distance(_player.position, closestPoint);
        float alpha = CalculateAlpha(dist);

        Color newColor = _initialColor;
        newColor.a = alpha;

        _instanceMaterial.SetColor(_colorPropID, newColor);
    }

    float CalculateAlpha(float dist)
    {
        if (dist >= fadeStartDistance) return 0f;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        return maxAlpha - (pulse * pulseDepth);
    }

    private void OnDestroy()
    {
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }

    void OnDrawGizmosSelected()
    {
        // 1. Set the Gizmo matrix to match the object's transform
        // This ensures the boxes rotate and scale with the object
        Gizmos.matrix = transform.localToWorldMatrix;

        // 2. Draw in local space. 
        // A standard Cube mesh has a local size of (1, 1, 1) and center (0, 0, 0)
        Vector3 localCenter = Vector3.zero;
        Vector3 localSize = gameObject.transform.localScale;

        // Fade End (Red) - note we divide by scale to keep the meters consistent
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Vector3 endOffset = new Vector3(fadeEndDistance * 2 / transform.lossyScale.x, fadeEndDistance * 2 / transform.lossyScale.y, fadeEndDistance * 2 / transform.lossyScale.z);
        Gizmos.DrawWireCube(localCenter, localSize + endOffset);

        // Fade Start (Yellow)
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
        Vector3 startOffset = new Vector3(fadeStartDistance * 2 / transform.lossyScale.x, fadeStartDistance * 2 / transform.lossyScale.y, fadeStartDistance * 2 / transform.lossyScale.z);
        Gizmos.DrawWireCube(localCenter, localSize + startOffset);
    }
}