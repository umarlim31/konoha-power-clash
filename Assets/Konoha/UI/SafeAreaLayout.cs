using UnityEngine;

namespace Konoha.UI
{
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        public RectTransform safeRoot;
        public RectTransform joystick;
        public bool compactJoystick;
        private Rect previous;
        private Vector2Int dimensions;

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            if (size.x <= 0 || size.y <= 0 || (safe == previous && size == dimensions)) return;
            previous = safe;
            dimensions = size;
            safeRoot.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            safeRoot.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            // Canvas scale follows the short edge; pad stays close to the grip on 4:3 tablets.
            bool tabletShape = (float)size.x / size.y < 1.6f;
            joystick.anchoredPosition = compactJoystick
                ? (tabletShape ? new Vector2(118f, 116f) : new Vector2(125f, 112f))
                : (tabletShape ? new Vector2(145f, 140f) : new Vector2(165f, 135f));
            joystick.sizeDelta = compactJoystick ? new Vector2(170f, 170f)
                : (tabletShape ? new Vector2(210f, 210f) : new Vector2(200f, 200f));
        }
    }
}
