using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class SplineObjPlacer : MonoBehaviour
{
    [SerializeField] private GameObject prefabToPlace;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private int seed = 123;
    [SerializeField] private Vector2 intervalRange = new Vector2(2f, 5f);
    [Range(0f, 20f)][SerializeField] private float raycastOffset = 10f;
    [SerializeField] private Vector3 rotationOffset;

    [HideInInspector]
    [SerializeField] private List<GameObject> spawnedObjects = new List<GameObject>();

    private SplineContainer splineContainer;

    [ContextMenu("gen objs")]
    public void Generate()
    {
        splineContainer = GetComponent<SplineContainer>();

        ClearObjects();

        if (splineContainer == null || prefabToPlace == null)
        {
            Debug.LogWarning("missing prefab");
            return;
        }

        Random.InitState(seed);

        var spline = splineContainer.Spline;
        float length = spline.GetLength();
        float currentDist = 0;

        while (currentDist <= length)
        {
            float t = currentDist / length;

            //local to world
            Vector3 worldPos = transform.TransformPoint(spline.EvaluatePosition(t));
            Vector3 worldTangent = transform.TransformDirection(spline.EvaluateTangent(t));

            Ray ray = new Ray(worldPos + Vector3.up * raycastOffset, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastOffset * 2f, terrainLayer))
                PlaceObject(hit.point, worldTangent, hit.normal);

            float nextInterval = Random.Range(intervalRange.x, intervalRange.y);
            currentDist += nextInterval;

            if (nextInterval <= 0) break;
        }
    }

    private void PlaceObject(Vector3 position, Vector3 forward, Vector3 terrainNormal)
    {
        GameObject obj;

#if UNITY_EDITOR
        obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace, transform);
        Undo.RegisterCreatedObjectUndo(obj, "Generate Spline Objects");
#else
        obj = Instantiate(prefabToPlace, transform);
#endif

        obj.transform.position = position;

        Quaternion alignRotation = Quaternion.LookRotation(forward, terrainNormal);
        obj.transform.rotation = alignRotation * Quaternion.Euler(rotationOffset);

        spawnedObjects.Add(obj);
    }

    [ContextMenu("clear objs")]
    public void ClearObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(obj);
#else
                DestroyImmediate(obj);
#endif
            }
        }

        spawnedObjects.Clear();
    }
}