using UnityEditor;
using UnityEngine;

// Needed to use Handles and GUIStyle

namespace Utils
{
    [ExecuteInEditMode] // Ensures the annotation shows in edit mode
    public class CTWCustomAnnotationComponent : MonoBehaviour
    {
        [Header("Annotation Settings")]
        [Tooltip("Custom text to display as an annotation in the Scene view.")]
        public string annotationText = "Default Annotation";

        [Tooltip("Text color for the annotation.")]
        public Color textColor = Color.white;

        [Tooltip("Background color for the annotation.")]
        public Color backgroundColor = Color.black;

        [Tooltip("Offset for positioning the text above the object.")]
        public Vector3 textOffset = new Vector3(0, 2, 0);

        private GUIStyle style;

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // Initialize the GUIStyle if it's not set up yet
            if (style == null)
            {
                style = new GUIStyle();
                style.normal.textColor = textColor;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                style.padding = new RectOffset(5, 5, 5, 5); // Padding around the text
            }

            // Set background color (you can optionally add transparency)
            style.normal.background = MakeTexture(2, 2, backgroundColor);

            // Draw label with background
            Handles.Label(transform.position + textOffset, annotationText, style);
#endif
        }

        // Helper method to create a texture for the background
        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}