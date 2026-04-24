using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CTWCollidersOverlapChecker : EditorWindow
{
    [MenuItem("Tools/Check Collider Overlaps")]
    public static void ShowWindow()
    {
        GetWindow<CTWCollidersOverlapChecker>("Collider Overlap Checker");
    }

    private Vector2 scroll;
    private List<(Collider, Collider)> overlaps = new();

    void OnGUI()
    {
        if (GUILayout.Button("Scan Scene for Overlapping Colliders"))
        {
            FindOverlaps();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var pair in overlaps)
        {
            EditorGUILayout.LabelField($"{pair.Item1.name} <-> {pair.Item2.name}");

            if (GUILayout.Button("Select Both", GUILayout.Width(100)))
            {
                Selection.objects = new Object[] { pair.Item1.gameObject, pair.Item2.gameObject };
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void FindOverlaps()
    {
        overlaps.Clear();
        Collider[] all = Object.FindObjectsByType<Collider>(FindObjectsSortMode.InstanceID);
        HashSet<(Collider, Collider)> checkedPairs = new();

        for (int i = 0; i < all.Length; i++)
        {
            for (int j = i + 1; j < all.Length; j++)
            {
                Collider a = all[i];
                Collider b = all[j];

                if (a == null || b == null || a == b) continue;

                if (a.bounds.Intersects(b.bounds))
                {
                    if (!checkedPairs.Contains((b, a)))
                    {
                        overlaps.Add((a, b));
                        checkedPairs.Add((a, b));
                    }
                }
            }
        }

        Debug.Log($"Found {overlaps.Count} overlapping collider pairs.");
    }
}
