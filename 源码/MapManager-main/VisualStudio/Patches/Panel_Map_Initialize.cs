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

            const int textureSize = 32;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Draw a compact red triangle with a dark outline and a soft offset shadow.
                    bool shadow = IsArrowTriangle(x - 1.0f, y + 1.0f, 3.0f, 28.0f, 9.5f);
                    bool outline = IsArrowTriangle(x, y, 4.0f, 29.0f, 9.0f);
                    bool fill = IsArrowTriangle(x, y, 5.0f, 27.5f, 6.6f);
                    Color pixel = Color.clear;
                    if (shadow)
                        pixel = new Color(0f, 0f, 0f, 0.25f);
                    if (outline)
                        pixel = new Color(0.18f, 0.01f, 0.01f, 0.95f);
                    if (fill)
                        pixel = new Color(0.95f, 0.02f, 0.02f, 1f);
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();

            GameObject arrowObject = new GameObject("MapManagerPlayerArrow");
            arrowObject.transform.SetParent(icon, false);
            UITexture arrow = arrowObject.AddComponent<UITexture>();
            arrow.mainTexture = texture;
            arrow.width = 10;
            arrow.height = 10;
            UIWidget existingWidget = icon.GetComponent<UIWidget>();
            arrow.depth = existingWidget != null ? existingWidget.depth + 1 : 100;
            MelonLogger.Msg("MapDiag fallback arrow created.");
        }

        private static bool IsArrowTriangle(float x, float y, float baseY, float tipY, float baseHalfWidth)
        {
            if (y < baseY || y > tipY)
                return false;

            float progress = (tipY - y) / (tipY - baseY);
            float halfWidth = 0.65f + baseHalfWidth * progress;
            return System.Math.Abs(x - 15.5f) <= halfWidth;
        }
    }
}
