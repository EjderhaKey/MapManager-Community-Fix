using ModSettings;

namespace MapManager
{
	internal enum DisplayLanguage
	{
		English,
		Chinese
	}

	internal enum PlayerArrowColor
	{
		Red,
		Black,
		Gray,
		Navy
	}

	internal class Settings : JsonModSettings
	{
		internal static Settings Instance { get; } = new();
		private DisplayLanguage confirmedLanguage;

		[Section("Language Settings")]
		[Name("Display Language")]
		[Description("Restart the game to apply language changes.")]
		[Choice("English", "简体中文")]
		public DisplayLanguage Language = DisplayLanguage.Chinese;

		[Section("Map Settings")]
		[Name("Enable Map Arrow")]
		[Description("If enabled, will show where the player is on the map with an arrow")]
		public bool EnableArrow                 = false;

		[Name("Player Arrow Size")]
		[Slider(8f, 28f, NumberFormat = "{0:F0}")]
		public float PlayerArrowSize = 16f;

		[Name("Player Arrow Color")]
		[Choice("Red", "Black", "Gray (#666666)", "Navy (#183060)")]
		public PlayerArrowColor PlayerArrowColor = PlayerArrowColor.Red;

		[Name("Center on the player")]
		[Description("If enabled, will center on where the player is on the map")]
		public bool CenterOnPlayer              = false;

		[Name("BETA: Reveal The map")]
		[Description("Set this to be able to map the entire region")]
		public KeyCode RevealMap                = KeyCode.KeypadDivide;

		[Section("Map Surveying")]
		[Name("Range Mulitplier")]
		//[Description("Vanilla = 25")]
		[Slider(-0.99f, 25f, NumberFormat = "{0:F2}")]
		public float MapSurveyMult              = 1f;

		[Name("Map Surveying Time Multiplier")]
		[Description("The amount of time it takes to survey")]
		[Slider(0.1f, 5f, NumberFormat = "{0:F2}")]
		public float MapSurveyMultTime          = 1f;

		[Name("Polaroid's Always Discovered")]
		[Description("Surveying vistas w/o polaroid's will add the relevant Polaroid to your inventory.")]
		public bool MapWithPolariods            = false;

		[Name("Reveal Vista Locations")]
		[Description("Will force reveal any vista locations within range")]
		public bool RevealVista                 = false;

		[Name("Unlock Survey")]
		[Description("Allow surveying at any time.")]
		public bool UnlockSurvey                = false;

		[Name("Automatically add all dead corpses to the map")]
		[Description("")]
		public bool AddCorpseToMap = false;

		[Section("Carry Weight")]
		[Name("Enable Custom Carry Weight")]
		[Description("When enabled, sets the player's effective carry capacity to the selected value.")]
		public bool EnableCustomCarryWeight = false;

		[Name("Maximum Carry Weight (kg)")]
		[Slider(1f, 999f, NumberFormat = "{0:F0}")]
		public float CustomCarryWeightKG = 30f;


		internal static void OnLoad()
		{
			Instance.confirmedLanguage = Instance.Language;
			SettingsLocalization.SetLanguage(Instance.Language);
			Instance.AddToModSettings($"{BuildInfo.GUIName}");
			Instance.RefreshGUI();
		}

		protected override void OnConfirm()
		{
			base.OnConfirm();
			Save();

			if (Language != confirmedLanguage)
			{
				confirmedLanguage = Language;
				string message = Language == DisplayLanguage.Chinese
					? "语言设置已保存。下次启动游戏时生效。"
					: "Language saved. It will take effect the next time you start the game.";
				InterfaceManager.GetPanel<Panel_Confirmation>()
					.ShowGenericMessage(message, new Action(() => { }));
			}
		}
	}
}
