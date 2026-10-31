namespace MapManager
{
    [HarmonyPatch(typeof(Panel_Map), nameof(Panel_Map.Enable), new Type[] { typeof(bool), typeof(bool) })]
    internal class Panel_Map_Initialize
    {
        private static Panel_Map pendingPanel;
        private static int framesUntilCheck;
        private static int checkStage;
        private static Texture2D playerArrowTexture;
        private static readonly float[] ArrowVerticesX = { 2f, 15f, 15f, 30f, 15f, 15f, 2f };
        private static readonly float[] ArrowVerticesY = { 11f, 11f, 4f, 16f, 28f, 21f, 21f };

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
            if (icon == null)
                return;

            Transform arrowTransform = icon.Find("MapManagerPlayerArrow");
            UITexture arrow;
            UIWidget existingWidget = icon.GetComponent<UIWidget>();
            if (arrowTransform == null)
            {
                GameObject arrowObject = new GameObject("MapManagerPlayerArrow");
                arrowObject.transform.SetParent(icon, false);
                arrow = arrowObject.AddComponent<UITexture>();
                arrow.depth = existingWidget != null ? existingWidget.depth + 1 : 100;
                MelonLogger.Msg("MapDiag fallback arrow created.");
            }
            else
            {
                arrow = arrowTransform.GetComponent<UITexture>();
            }

            if (arrow == null)
                return;

            if (playerArrowTexture == null)
            {
                playerArrowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            UpdateArrowTexture(playerArrowTexture, GetArrowColor(Settings.Instance.PlayerArrowColor));
            arrow.mainTexture = playerArrowTexture;
            int size = Mathf.RoundToInt(Settings.Instance.PlayerArrowSize);
            arrow.width = Mathf.RoundToInt(size * 1.15f);
            arrow.height = size;
        }

        private static Color GetArrowColor(PlayerArrowColor color)
        {
            switch (color)
            {
                case PlayerArrowColor.Black:
                    return new Color(0.04f, 0.04f, 0.04f, 1f);
                case PlayerArrowColor.Gray:
                    return new Color(102f / 255f, 102f / 255f, 102f / 255f, 1f);
                case PlayerArrowColor.Navy:
                    return new Color(24f / 255f, 48f / 255f, 96f / 255f, 1f);
                default:
                    return new Color(0.92f, 0.025f, 0.025f, 1f);
            }
        }

        private static void UpdateArrowTexture(Texture2D texture, Color fillColor)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float fill = ShapeCoverage(x, y, 0f, 0f);
                    float outline = 0f;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                            outline = Mathf.Max(outline, ShapeCoverage(x, y, ox * 1.1f, oy * 1.1f));
                    }
                    float shadow = ShapeCoverage(x, y, -1.1f, 1.1f);

                    Color pixel = new Color(0f, 0f, 0f, shadow * 0.28f);
                    pixel = Blend(pixel, Color.white, Mathf.Max(0f, outline - fill) * 0.95f);
                    pixel = Blend(pixel, fillColor, fill);
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();
        }

        private static float ShapeCoverage(int x, int y, float offsetX, float offsetY)
        {
            int inside = 0;
            for (int sx = 0; sx < 4; sx++)
            {
                for (int sy = 0; sy < 4; sy++)
                {
                    float px = x + (sx + 0.5f) * 0.25f + offsetX;
                    float py = y + (sy + 0.5f) * 0.25f + offsetY;
                    if (IsArrowShape(px, py))
                        inside++;
                }
            }
            return inside / 16f;
        }

        private static bool IsArrowShape(float x, float y)
        {
            // Right-facing arrowhead with a broad shaft, matching the ➤ silhouette.
            bool inside = false;
            for (int i = 0, j = ArrowVerticesX.Length - 1; i < ArrowVerticesX.Length; j = i++)
            {
                bool crosses = (ArrowVerticesY[i] > y) != (ArrowVerticesY[j] > y) &&
                    x < (ArrowVerticesX[j] - ArrowVerticesX[i]) * (y - ArrowVerticesY[i]) /
                    (ArrowVerticesY[j] - ArrowVerticesY[i]) + ArrowVerticesX[i];
                if (crosses)
                    inside = !inside;
            }
            return inside;
        }

        private static Color Blend(Color background, Color foreground, float opacity)
        {
            float alpha = Mathf.Clamp01(opacity);
            float resultAlpha = alpha + background.a * (1f - alpha);
            if (resultAlpha <= 0f)
                return Color.clear;

            Color result = (foreground * alpha + background * (background.a * (1f - alpha))) / resultAlpha;
            result.a = resultAlpha;
            return result;
        }
    }
}
