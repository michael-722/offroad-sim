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

        _colorPropID = _instanceMaterial.HasProperty("_BaseColor") ?
                       Shader.PropertyToID("_BaseColor") :
                       Shader.PropertyToID("_Color");

        _initialColor = _instanceMaterial.GetColor(_colorPropID);

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
    }

    void Update()
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
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;
        Bounds b = r.bounds;
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(b.center, b.size);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size + Vector3.one * (fadeEndDistance * 2f));

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
        Gizmos.DrawWireCube(b.center, b.size + Vector3.one * (fadeStartDistance * 2f));
    }
}