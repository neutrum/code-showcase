using UnityEngine;

namespace Utils
{
    public static class PlaneBoundaryOffset
    {
        public static Vector2[] ShiftPointsInside(Vector2[] boundaryPoints, float offset)
        {
            if (boundaryPoints == null || boundaryPoints.Length < 3)
            {
                Debug.LogError("Boundary must have at least 3 points.");
                return boundaryPoints;
            }

            // Calculate the centroid
            Vector2 centroid = Vector2.zero;
            foreach (var point in boundaryPoints)
            {
                centroid += point;
            }
            centroid /= boundaryPoints.Length;

            // Create a new array for the shifted points
            Vector2[] shiftedPoints = new Vector2[boundaryPoints.Length];

            // Shift each point towards the centroid
            for (int i = 0; i < boundaryPoints.Length; i++)
            {
                Vector2 direction = (centroid - boundaryPoints[i]).normalized;
                shiftedPoints[i] = boundaryPoints[i] + direction * offset;
            }

            return shiftedPoints;
        }
    }

}