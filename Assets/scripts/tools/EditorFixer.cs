using UnityEngine;
using UnityEditor;

//public class EditorFixer : EditorWindow
//{
//    [MenuItem("Tools/Force Clear Inspector Errors")]
//    public static void CleanInternalState()
//    {
//        Selection.activeGameObject = null;
//        System.GC.Collect();
//        Undo.ClearAll();

//        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
//        int fakeNulls = 0;
//        foreach (var go in allObjects)
//        {
//            if (go == null) continue;

//            var components = go.GetComponents<Component>();
//            foreach (var c in components)
//            {
//                if (c == null)
//                {
//                    Debug.LogError($"Found a 'Dead Component' on {go.name}. Unity thinks it's null but it's still attached.", go);
//                    fakeNulls++;
//                }
//            }
//        }

//        Debug.Log($"Internal cleanup complete. Found {fakeNulls} dead components.");
//        Debug.Log("If errors persist, restart Unity and delete the 'Library' folder.");
//    }
//}