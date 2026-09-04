using UnityEngine;
using UnityEngine.UI;

namespace SP2FreeCamera
{
    internal sealed class FreeCameraMenuInputBlocker : MonoBehaviour
    {
        private FreeCameraRuntime _runtime;
        private Canvas _canvas;
        private readonly RectTransform[] _blockerRects = new RectTransform[3];

        internal void Initialize(FreeCameraRuntime runtime)
        {
            _runtime = runtime;
        }

        private void Awake()
        {
            GameObject canvasObject = new GameObject("SP2 Free Camera Input Blocker");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
            canvasObject.AddComponent<GraphicRaycaster>();

            for (int i = 0; i < _blockerRects.Length; i++)
            {
                GameObject blockerObject = new GameObject("Free Camera UI Input Blocker " + i);
                blockerObject.transform.SetParent(canvasObject.transform, false);
                RectTransform blockerRect = blockerObject.AddComponent<RectTransform>();
                blockerRect.anchorMin = new Vector2(0f, 1f);
                blockerRect.anchorMax = new Vector2(0f, 1f);
                blockerRect.pivot = new Vector2(0f, 1f);

                Image image = blockerObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
                blockerObject.SetActive(false);
                _blockerRects[i] = blockerRect;
            }

            canvasObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_canvas == null)
            {
                return;
            }

            bool anyVisible = false;
            for (int i = 0; i < _blockerRects.Length; i++)
            {
                RectTransform blockerRect = _blockerRects[i];
                Rect rect = default(Rect);
                bool visible = blockerRect != null && _runtime != null &&
                    _runtime.TryGetInputBlockerRect(i, out rect);
                if (blockerRect != null && blockerRect.gameObject.activeSelf != visible)
                {
                    blockerRect.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                blockerRect.anchoredPosition = new Vector2(rect.xMin, -rect.yMin);
                blockerRect.sizeDelta = new Vector2(rect.width, rect.height);
                anyVisible = true;
            }

            if (_canvas.gameObject.activeSelf != anyVisible)
            {
                _canvas.gameObject.SetActive(anyVisible);
            }
        }
    }
}
