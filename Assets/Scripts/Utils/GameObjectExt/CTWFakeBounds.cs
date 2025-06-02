using UnityEngine;

[ExecuteAlways] // So it works in edit mode too
public class CTWFakeBounds : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public Vector3 size = Vector3.one;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.25f); // translucent green

    public Bounds GetBounds()
    {
        return new Bounds(transform.position + center, size);
    }

    private void OnDrawGizmos()
    {
        DrawBoundsGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawBoundsGizmo(highlighted: true);
    }

    private void DrawBoundsGizmo(bool highlighted = false)
    {
        Gizmos.color = highlighted ? Color.yellow : gizmoColor;
        var bounds = GetBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        if (!highlighted)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.2f);
            Gizmos.DrawCube(bounds.center, bounds.size);
        }
    }
}