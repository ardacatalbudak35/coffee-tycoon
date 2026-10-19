using UnityEngine;
using UnityEngine.UI;

namespace CafeLife
{
    // Screen-space geometry stays crisp without depending on font size or glyph metrics.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CafeReticle : MaskableGraphic
    {
        public CafeGame game;
        float emphasis;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            canvasRenderer.SetAlpha(0);
        }

        void LateUpdate()
        {
            var player = game ? game.player : null;
            bool touch = Application.isMobilePlatform;
#if UNITY_EDITOR
            touch |= game && game.ui && game.ui.EditorTouchPreview;
#endif
            bool visible = player && player.view && player.view.isActiveAndEnabled
                && !player.WorldInputBlocked && !CafeFocusedMode.CounterControls
                && (touch || !player.PointerMode);
            canvasRenderer.SetAlpha(visible ? 1 : 0);
            if (!visible) return;

            // Camera aim uses the viewport centre, independent of a phone's safe area.
            rectTransform.anchorMin = rectTransform.anchorMax = player.view.rect.center;
            float next = Mathf.MoveTowards(emphasis, player.target ? 1 : 0, Time.unscaledDeltaTime * 8);
            if (!Mathf.Approximately(next, emphasis))
            {
                emphasis = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var shade = new Color(.035f, .045f, .04f, .38f);
            var cream = new Color(.94f, .93f, .86f, Mathf.Lerp(.64f, .88f, emphasis));
            Band(mesh, 0, 1.8f, shade, shade);
            Band(mesh, 1.8f, 2.4f, shade, Clear(shade));
            Band(mesh, 0, 1.2f, cream, cream);
            Band(mesh, 1.2f, 1.8f, cream, Clear(cream));
            if (emphasis <= 0) return;

            shade.a = .3f * emphasis;
            cream.a = .68f * emphasis;
            Band(mesh, 4.2f, 6.3f, shade, shade);
            Band(mesh, 6.3f, 6.8f, shade, Clear(shade));
            Band(mesh, 4.2f, 4.8f, Clear(cream), cream);
            Band(mesh, 4.8f, 5.6f, cream, cream);
            Band(mesh, 5.6f, 6.2f, cream, Clear(cream));
        }

        static Color Clear(Color c) { c.a = 0; return c; }

        static void Band(VertexHelper mesh, float inner, float outer, Color inside, Color outside)
        {
            const int segments = 48;
            int start = mesh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                mesh.AddVert(direction * inner, inside, Vector2.zero);
                mesh.AddVert(direction * outer, outside, Vector2.zero);
                if (i == 0) continue;
                int n = start + i * 2;
                mesh.AddTriangle(n - 2, n - 1, n);
                mesh.AddTriangle(n, n - 1, n + 1);
            }
        }
    }
}
