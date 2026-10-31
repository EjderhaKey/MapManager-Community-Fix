using ModSettings;

namespace MapManager
{
	internal static class SettingsLocalization
	{
		private static DisplayLanguage currentLanguage = DisplayLanguage.Chinese;

		private static readonly Dictionary<string, string> ChineseText = new()
		{
			["Language Settings"] = "语言设置",
			["Display Language"] = "界面语言",
			["Restart the game to apply language changes."] = "重启游戏后应用语言更改。",
			["Map Settings"] = "地图设置",
			["Enable Map Arrow"] = "显示玩家位置箭头",
			["If enabled, will show where the player is on the map with an arrow"] = "开启后，在地图上显示玩家位置箭头。",
			["Player Arrow Size"] = "玩家箭头大小",
			["Player Arrow Color"] = "玩家箭头颜色",
			["Red"] = "红色",
			["Black"] = "黑色",
			["Gray (#666666)"] = "灰色（#666666）",
			["Navy (#183060)"] = "藏青（#183060）",
			["Center on the player"] = "地图以玩家为中心",
			["If enabled, will center on where the player is on the map"] = "开启后，打开地图时以玩家位置为中心。",
			["BETA: Reveal The map"] = "测试：揭示地图",
			["Set this to be able to map the entire region"] = "按下此按键可绘制当前区域地图。",
			["Map Surveying"] = "地图测绘",
			["Range Mulitplier"] = "测绘范围倍率",
			["Map Surveying Time Multiplier"] = "测绘时间倍率",
			["The amount of time it takes to survey"] = "调整地图测绘所需时间。",
			["Polaroid's Always Discovered"] = "始终发现极光照片",
			["Surveying vistas w/o polaroid's will add the relevant Polaroid to your inventory."] = "没有对应照片时测绘观景点，会将照片加入背包。",
			["Reveal Vista Locations"] = "显示观景点位置",
			["Will force reveal any vista locations within range"] = "强制显示范围内的观景点位置。",
			["Unlock Survey"] = "解锁地图测绘",
			["Allow surveying at any time."] = "允许随时进行地图测绘。",
			["Automatically add all dead corpses to the map"] = "自动将尸体标记到地图",
			["Carry Weight"] = "负重设置",
			["Enable Custom Carry Weight"] = "启用自定义负重",
			["When enabled, sets the player's effective carry capacity to the selected value."] = "开启后，将玩家负重上限设置为所选数值。",
			["Maximum Carry Weight (kg)"] = "负重上限（kg）"
		};

		internal static string Translate(string text)
		{
			if (currentLanguage == DisplayLanguage.Chinese && ChineseText.TryGetValue(text, out string translated))
				return translated;

			return text;
		}

		internal static void SetLanguage(DisplayLanguage language) => currentLanguage = language;
	}

	[HarmonyPatch(typeof(NameAttribute), "get_Name")]
	internal static class NameAttribute_GetName_Localize
	{
		private static void Postfix(ref string __result) => __result = SettingsLocalization.Translate(__result);
	}

	[HarmonyPatch(typeof(DescriptionAttribute), "get_Description")]
	internal static class DescriptionAttribute_GetDescription_Localize
	{
		private static void Postfix(ref string __result) => __result = SettingsLocalization.Translate(__result);
	}

	[HarmonyPatch(typeof(SectionAttribute), "get_Title")]
	internal static class SectionAttribute_GetTitle_Localize
	{
		private static void Postfix(ref string __result) => __result = SettingsLocalization.Translate(__result);
	}

	[HarmonyPatch(typeof(ChoiceAttribute), "get_Names")]
	internal static class ChoiceAttribute_GetNames_Localize
	{
		private static void Postfix(ref string[] __result)
		{
			if (__result == null)
				return;

			for (int i = 0; i < __result.Length; i++)
				__result[i] = SettingsLocalization.Translate(__result[i]);
		}
	}
}
