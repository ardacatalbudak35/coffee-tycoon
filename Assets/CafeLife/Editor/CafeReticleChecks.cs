using System;
using UnityEngine;

namespace CafeLife
{
    public static class CafeReticleChecks
    {
        public static string Report;
        static void Check(bool ok, string label)
        {
            if (!ok) throw new Exception(label);
            Report += "PASS " + label + "\n";
        }
        public static void Validate(CafeGame game)
        {
            Report = "";
            var mark = UnityEngine.Object.FindFirstObjectByType<CafeReticle>();
            Check(mark, "Reticle exists in the rendered HUD");
            game.Continue();
            mark.SendMessage("LateUpdate");
            Check(mark.canvasRenderer.GetAlpha() > .9f, "Reticle is visible during free camera play");
            Canvas.ForceUpdateCanvases();
            var center = RectTransformUtility.WorldToScreenPoint(null, mark.rectTransform.position);
            Check(Vector2.Distance(center, game.player.view.pixelRect.center) < 1,
                "Reticle aligns with camera ray within one pixel");
            Check(!game.player.OverInterface(center), "Reticle does not intercept centre-screen pointer input");
            game.player.TogglePointer();
            mark.SendMessage("LateUpdate");
            Check(mark.canvasRenderer.GetAlpha() == 0, "Reticle hides in free pointer mode");
            game.ui.EditorTouchPreview = true;
            mark.SendMessage("LateUpdate");
            Check(mark.canvasRenderer.GetAlpha() > .9f, "Touch reticle remains visible without a locked desktop pointer");
            game.ui.EditorTouchPreview = false;
            game.OpenOperations();
            mark.SendMessage("LateUpdate");
            Check(mark.canvasRenderer.GetAlpha() == 0, "Reticle hides behind management menus");
            game.Continue();
            game.cup = new CupWork { recipe=1, hasMilk=true, foamed=true, brewed=true };
            var pour = game.GetComponent<CafeLattePour>();
            pour.Begin(game);
            mark.SendMessage("LateUpdate");
            Check(mark.canvasRenderer.GetAlpha() == 0, "Reticle hides during latte drawing without pausing the game");
            pour.Cancel();game.Discard();game.player.Lock(true);
            mark.SendMessage("LateUpdate");
        }
    }
}
