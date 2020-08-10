using System.Collections.Generic;
using UnityEngine;


namespace ProjectAce
{
    public enum AnchorPresets
    {
        TOP_LEFT,
        TOP_CENTER,
        TOP_RIGHT,
        MIDDLE_LEFT,
        MIDDLE_CENTER,
        MIDDLE_RIGHT,
        BOTTOM_LEFT,
        BOTTOM_CENTER,
        BOTTOM_RIGHT
    };

    public static class AnchorPresetsUtils
    {    
        private class AnchorPresetPositions
        {
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;

            public AnchorPresetPositions(float aMinX, float aMinY, float aMaxX, float aMaxY, float pivotX, float pivotY)
            {
                anchorMin = new Vector2(aMinX, aMinY);
                anchorMax = new Vector2(aMaxX, aMaxY);
                pivot = new Vector2(pivotX, pivotY);
            }
        }

        private static readonly Dictionary<AnchorPresets, AnchorPresetPositions> anchorPresetPositions = new Dictionary<AnchorPresets, AnchorPresetPositions>()
        {
            { AnchorPresets.TOP_LEFT, new AnchorPresetPositions(0f, 1f, 0f, 1f, 0f, 1f) },
            { AnchorPresets.TOP_CENTER, new AnchorPresetPositions(0.5f, 1f, 0.5f, 1f, 0.5f, 1f) },
            { AnchorPresets.TOP_RIGHT, new AnchorPresetPositions(1f, 1f, 1f, 1f, 1f, 1f) },

            { AnchorPresets.MIDDLE_LEFT, new AnchorPresetPositions(0f, 0.5f, 0f, 0.5f, 0f, 0.5f) },
            { AnchorPresets.MIDDLE_CENTER, new AnchorPresetPositions(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f) },
            { AnchorPresets.MIDDLE_RIGHT, new AnchorPresetPositions(1f, 0.5f, 1f, 0.5f, 1f, 0.5f) },

            { AnchorPresets.BOTTOM_LEFT, new AnchorPresetPositions(0f, 0f, 0f, 0f, 0f, 0f) },
            { AnchorPresets.BOTTOM_CENTER, new AnchorPresetPositions(0.5f, 0f, 0.5f, 0f, 0.5f, 0f) },
            { AnchorPresets.BOTTOM_RIGHT, new AnchorPresetPositions(1f, 0f, 1f, 0f, 1f, 0f) },
        };

        public static void AssignAnchor(AnchorPresets preset, ref RectTransform rectTransform)
        {
            var presetPositions = anchorPresetPositions[preset];
            rectTransform.anchorMin = new Vector2(presetPositions.anchorMin.x, presetPositions.anchorMin.y);
            rectTransform.anchorMax = new Vector2(presetPositions.anchorMax.x, presetPositions.anchorMax.y);
            rectTransform.pivot = new Vector2(presetPositions.pivot.x, presetPositions.pivot.y);
        }

    }
}
