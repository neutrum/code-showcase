#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode] // Makes sure the script runs in the editor
public class CTWShowBoundsInEditor : MonoBehaviour
{
    public Color gizmoColor = Color.green; // Color of the bounds in the scene view
    public bool showBoundsSize = true; // Option to show the size of the bounds

    private void OnDrawGizmos()
    {
        // Get the Renderer or Collider bounds
        Renderer renderer = GetComponent<Renderer>();
        Collider collider = GetComponent<Collider>();

        Bounds bounds;
        if (renderer != null)
        {
            bounds = renderer.bounds;
        }
        else if (collider != null)
        {
            bounds = collider.bounds;
        }
        else
        {
            return; // No bounds to show
        }

        // Draw the bounds in the scene view
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(bounds.center, bounds.size); // Draw a wireframe cube showing the bounds

        // Optionally, display the size of the bounds
        if (showBoundsSize)
          {
            Vector3 boundSize = bounds.size;
            string sizeText = $"Size: {boundSize.x:F2}, {boundSize.y:F2}, {boundSize.z:F2}";
            
            // Draw the size text in the scene view at the center of the bounds
            GUIStyle style = new GUIStyle();
            style.normal.textColor = gizmoColor;
            Handles.Label(bounds.center, sizeText, style);
        }
    }
}
#endif