namespace MapManager
{
    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.Enable), new Type[] { typeof(bool), typeof(bool) })]
    internal class Panel_Map_Initialize
    {
        private static Panel_Map pendingPanel;
        private static int framesUntilCheck;
        private static int checkStage;

        private static void Postfix(Panel_Map __instance, ref bool enable, ref bool cameFromDetailSurvey)
        {
            if (!enable) return;

            try
            {
                ApplyMapSettings(__instance, cameFromDetailSurvey);
            }
            catch (Exception error)
            {
                MelonLogger.Msg("MapDiag ERROR in map patch: " + error);
            }
        }

        private static void ApplyMapSettings(Panel_Map panel, bool cameFromDetailSurvey)
        {
            Panel_Map.ResetOpts resetOpts = Panel_Map.ResetOpts.Zoomed;
            if (Settings.Instance.EnableArrow)
            {
                resetOpts |= Panel_Map.ResetOpts.ShowPlayer;
            }
            if (Settings.Instance.CenterOnPlayer)
            {
                resetOpts |= Panel_Map.ResetOpts.CenterOnPlayer;
            }
            MelonLogger.Msg($"MapDiag open: arrow={Settings.Instance.EnableArrow}, center={Settings.Instance.CenterOnPlayer}, survey={cameFromDetailSurvey}, opts={resetOpts}");
            panel.ResetToNormal(resetOpts);
            LogState("after reset", panel);

            if (Settings.Instance.EnableArrow)
            {
                Panel_Map.s_ForceShowPlayerIcon = true;
                panel.RefreshIconVisibility();
                EnsurePlayerArrow(panel);
            }
            LogState("after patch", panel);
            pendingPanel = panel;
            framesUntilCheck = 2;
            checkStage = 0;
        }

        internal static void Tick()
        {
            if (pendingPanel == null || --framesUntilCheck > 0) return;
            LogState(checkStage == 0 ? "after 2 frames" : "after 32 frames", pendingPanel);
            if (checkStage++ == 0)
                framesUntilCheck = 30;
            else
                pendingPanel = null;
        }

        private static void LogState(string stage, Panel_Map panel)
        {
            try
            {
                Transform icon = panel.m_PlayerIcon;
                if (icon == null)
                {
                    MelonLogger.Msg($"MapDiag {stage}: PlayerIcon=NULL, force={Panel_Map.s_ForceShowPlayerIcon}");
                    return;
                }

                UITexture arrow = icon.GetComponentInChildren<UITexture>(true);
                UIPanel uiPanel = icon.GetComponentInParent<UIPanel>();
                int widgets = icon.GetComponentsInChildren<UIWidget>(true).Length;
                int renderers = icon.GetComponentsInChildren<Renderer>(true).Length;
                MelonLogger.Msg($"MapDiag {stage}: force={Panel_Map.s_ForceShowPlayerIcon}, active={icon.gameObject.activeSelf}/{icon.gameObject.activeInHierarchy}, parent={icon.parent?.name}, uiPanel={uiPanel?.name}/{uiPanel?.gameObject.activeInHierarchy}, children={icon.childCount}, widgets={widgets}, renderers={renderers}, position={icon.localPosition}, rotation={icon.localEulerAngles}, scale={icon.localScale}, arrow={arrow != null}, texture={arrow?.mainTexture != null}, size={arrow?.width}x{arrow?.height}, depth={arrow?.depth}");
            }
            catch (Exception error)
            {
                MelonLogger.Msg($"MapDiag {stage} ERROR: {error}");
            }
        }

        // Supply a separate NGUI arrow when the existing player widget does not draw.
        private static void EnsurePlayerArrow(Panel_Map panel)
        {
            Transform icon = panel.m_PlayerIcon;
            if (icon == null || icon.Find("MapManagerPlayerArrow") != null)
                return;

            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Upward-pointing triangle with a notch at its base.
                    bool inside = y >= 3 && y <= 29 &&
                                  System.Math.Abs(x - 15.5f) <= (29 - y) * 0.48f + 1f &&
                                  !(y < 12 && System.Math.Abs(x - 15.5f) < (12 - y) * 0.38f);
                    texture.SetPixel(x, y, inside ? Color.red : Color.clear);
                }
            }
            texture.Apply();

            GameObject arrowObject = new GameObject("MapManagerPlayerArrow");
            arrowObject.transform.SetParent(icon, false);
            UITexture arrow = arrowObject.AddComponent<UITexture>();
            arrow.mainTexture = texture;
            arrow.width = 12;
            arrow.height = 12;
            UIWidget existingWidget = icon.GetComponent<UIWidget>();
            arrow.depth = existingWidget != null ? existingWidget.depth + 1 : 100;
            MelonLogger.Msg("MapDiag fallback arrow created.");
        }
    }
}
