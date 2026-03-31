using System.Collections.Generic;
using UnityEngine;

public class LessonLayoutRegister : MonoBehaviour
{
    public static LessonLayoutRegister Instance;

    private readonly Dictionary<string, LessonLayout> _layouts = new Dictionary<string, LessonLayout>();

    //layouts self register - look up for lesson manager

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Register(LessonLayout layout)
    {
        if (string.IsNullOrEmpty(layout.lessonId))
        {
            Debug.LogWarning($"layout: {layout.name} has no lessonId - skipping");
            return;
        }

        if (_layouts.ContainsKey(layout.lessonId))
        {
            Debug.LogWarning($"duplicate lessonId '{layout.lessonId}' on {layout.name}");
            return;
        }

        _layouts[layout.lessonId] = layout;
    }

    //returns null if no layout exists for this id
    public LessonLayout Get(string lessonId)
    {
        _layouts.TryGetValue(lessonId, out var layout);
        return layout;
    }

    public void DeactivateAll()
    {
        foreach (var layout in _layouts.Values)
            layout.Deactivate();
    }
}