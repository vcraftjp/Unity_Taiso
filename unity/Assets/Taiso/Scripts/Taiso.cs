using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VCraft
{
	public class Taiso : MonoBehaviour
	{
		public int maxPlayerCount = 36;
		public int playerCount = 10;
		public int frontCount = 2;
		public int rowIncrement = 1;
		public int columnCount = 10;
		public float spacingX = 1.5f;
		int formation;

		public int randomSeed = 1;
		public int maxShuffleIndex = 10;
		public bool isSubPlayersLog = false;
		public bool showLogWindow = false;
		public bool isPaused = false;

		AudioSource audioSource;

		const float T72 = 60f / 72;
		const float T120 = 60f / 120;
		public const float INTRO_TIME = 8.14f;
		readonly float[] musicTimes = { INTRO_TIME, T72 * 8, T72 * 16, T72 * 16, T72 * 16, T72 * 16, T72 * 16, T72 * 16
			, T72 * 16, T72 * 16, T72 * 16, T120 * 14 + T72 * 2, T72 * 16, T72 * 16 };
		float[] taisoTimes = new float[TaisoPlayer.TAISO_COUNT + 1];
		readonly float[] taisoTimesMeasured = {
			0f, 8.28f, 14.19f, 27.52f, 40.73f, 54.21f, 67.63f, 81.05f, 94.46f, 107.88f, 121.15f, 134.38f, 142.44f, 155.80f };

		int taisoIndex = -1;

		TaisoPlayer mainPlayer;
		bool isBoy;
		List<TaisoPlayer> players = new List<TaisoPlayer>();

		const float MIN_BODY_HEIGHT = 0.75f;
		const float MAX_BODY_HEIGHT = 1.25f;
		const float BODY_HEIGHT_RANGE = 0.25f;
		const float FATIGUE_TH = 0.3f;
		const float BODY_WEIGHT_TH = 0.2f;

		Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();

		SliderController playerCountSlider;
		ToggleController formationToggle;
		ToggleController boyGirlToggle;
		SliderController faceSlider;
		SliderController hairStyleSlider;
		SliderController clothStyleSlider;
		SliderController weightSlider;
		SliderController heightSlider;
		SliderController fatigueSlider;
		Button spacingButton;
		Button shuffleButton;
		Button skinColorButton;
		Button glassesButton;
		Button hairColorButton;
		Button headWearButton;
		Button clothColorButton;
		Button resetButton;
		InputField shuffleIndexInput;

		const string PKEY_PLAYER_SPACING = "playerSpacing";
		const string PKEY_SHUFFLE_INDEX = "shuffleIndex";
		const string PKEY_MAX_SHUFFLE_INDEX = "maxShuffleIndex";
		const string PKEY_SKIN_COLOR_INDEX = "skinColorIndex";
		const string PKEY_GLASSES_INDEX = "glassesIndex";
		const string PKEY_HAIR_COLOR_INDEX = "hairColorIndex";
		const string PKEY_HEAD_WEAR_INDEX = "headWearIndex";
		const string PKEY_CLOTH_COLOR_INDEX = "clothColorIndex";
		const string PKEY_AUDIO_VOLUME = "audioVolume";
		const string PKEY_AUDIO_MUTE = "audioMute";

		Button playButton;
		Button nextButton;
		Button prevButton;
		Button restartButton;
		Button fastButton;
		TimelineController timelineSlider;
		Button speakerButton;
		Slider volumeSlider;
		Color fastButtonColor;

		RectTransform rtSidePanel;
		RectTransform rtMediaPanel;
		Button panelButton;
		const float PANEL_MARGIN = 8f;
		const string PKEY_SHOW_PANEL = "isShowPanel";
		bool isShowPanel = false;
		const float OPENPANEL_ELAPSE = 0.5f;
		int openPanelFrame = -1;

		Button screenButton;

		CameraController cameraController;

		Text logText;
		ScrollRect logScrollRect;
		Button logButton;
		RectTransform rtLogPanel;
		bool isShowLogPanel = false;
		const string PKEY_SHOW_LOGPANEL = "isShowLogPanel";
#if !UNITY_EDITOR && UNITY_WEBGL
		bool isWebGLPaused = false;
#endif
		bool isStartPaused = false;
		float speed = 1f;

		// Start is called before the first frame update
		void Awake() {
			Debug.Log("Awake Taiso");

			spriteMap = Utils.loadAssets<Sprite>("Images", typeof(Sprite));
			initLogWindow();

#if !UNITY_EDITOR && UNITY_WEBGL
			Time.timeScale = 0f;
			isWebGLPaused = true;
			isPaused = false;
#else
			if (!isPaused) {
				showHideStartPanel(false);
			}
#endif
			initInfoPanel();

			Prefs.load();

			Application.targetFrameRate = TaisoPlayer.FPS;

			audioSource = GetComponent<AudioSource>();

			ChibiPlayer.loadChibiFaces();
			ChibiHair.init();
			cameraController = FindObjectOfType<CameraController>();

			initTaisoTimes();
			initSidePanel();
			initMediaPanel();
			initCanvasButton();

			if (isPaused) { // for PC recording
				setPaused();
				showHideStartPanel(true);
				isStartPaused = true;
			}

			GameObject prefab = GameObject.FindGameObjectWithTag("Player");
			if (prefab) {
				TaisoPlayer player = prefab.GetComponent<TaisoPlayer>();
				players.Add(player);
				ModifyClone(0);
				CreateClones(prefab);
				ModifyMainPlayer();
				ModifyClones();
			}

			cameraController.setCenter(mainPlayer.transform.position);
		}

		void Start() {
			Debug.Log("Start Taiso");
		}

		void Update() {
#if !UNITY_EDITOR && UNITY_WEBGL
			if (isWebGLPaused) {
				if (Input.GetMouseButtonDown(0)) {
					isWebGLPaused = false;
					cameraController.setIgnoreClick();
					Time.timeScale = 1f;
					audioSource.Play();
					showHideStartPanel(false);
				}
			}
#endif
			if (isStartPaused) {
				if (Time.frameCount >= TaisoPlayer.FPS * 2) {
					showHideStartPanel(false);
					isStartPaused = false;
				}
			}

			if (!timelineSlider.isDragging()) {
				timelineSlider.setValue(audioSource.time);
			}
			onKeyDown();
		}

		void OnApplicationQuit() {
			Prefs.save();
		}

		void initTaisoTimes() {
			float time = 0;
			for (int i = 0; i < TaisoPlayer.TAISO_COUNT + 1; i++) {
				taisoTimes[i] = time;
				time += musicTimes[i];
			}
		}

		public void startTaiso(int index) { // called from TaisoPlayer
			if (taisoIndex == -1) {
				playMusic(index);
			}
			taisoIndex = index;
			cameraController.setMotion(index, musicTimes[index]);
			Debug.Log("audio time #" + index + " time=" + audioSource.time + " (" + (audioSource.time - taisoTimes[index]) + ")");
		}

		public float getTimeDifference(int index) {
			return audioSource.time - taisoTimesMeasured[index];
		}

		void playTaisoFrom(int index) {
			index = Mathf.Clamp(index, 0, TaisoPlayer.TAISO_COUNT);
			taisoIndex = index;
			playMusic(index);
			cameraController.setMotion(index, musicTimes[index]);
			foreach(TaisoPlayer player in players) {
				player.PlayTaiso(index);
			}
		}

		public void playMusic(int taisoIndex) {
#if !UNITY_EDITOR && UNITY_WEBGL
			if (isWebGLPaused) return;
#endif
			float time = taisoTimes[taisoIndex];
			audioSource.time = time;
			audioSource.Play();
			if (isPaused) {
				audioSource.Pause();
			}
			Debug.Log("play music #" + taisoIndex + " from " + time + "s" );
		}

		void CreateClones(GameObject prefab) {
			Vector3 pos = prefab.transform.position;
			for (int i = 1; i < maxPlayerCount; i++) {
				float x, z;
				getClonePosition(i, out x, out z);
				GameObject go = Instantiate(prefab, new Vector3(pos.x + x, pos.y, pos.z - z), prefab.transform.rotation);
				go.name = prefab.name + "_" + i;
				TaisoPlayer player = go.GetComponent<TaisoPlayer>();
				players.Add(player);
				if (i >= playerCount) {
					player.setVisible(false);
				}
				Debug.Log("create " + go.name + " [" + x + ", " + z + "]");
			}
		}

		void getClonePosition(int index, out float x, out float z) {
			int row = 1;
			int col = frontCount;
			if (rowIncrement == 0 && frontCount > playerCount - 1) {
				col = playerCount - 1;
			}
			int sum = col;
			while (index > sum) {
				row++;
				col += rowIncrement;
				sum += col;
			}
			x = -(spacingX * (col - 1) / 2f) + (sum - index) * spacingX;
			z = (spacingX * 0.5f) * row;
		}

		void ModifyClones(bool beUpdate = false) {
			int shuffleIndex = Prefs.getInt(PKEY_SHUFFLE_INDEX, 0);
			shuffleIndexInput.text = (shuffleIndex + 1).ToString();
			int seed;
			if (shuffleIndex >= 0) {
				seed = randomSeed * (shuffleIndex + 1);
			} else {
				seed = System.DateTime.Now.Millisecond;
			}
			Random.InitState(seed);
			Debug.Log("shuffleIndex=" + shuffleIndex + ", randomSeed=" + seed);

			float[] fatigues = Utils.calcDistribution(maxPlayerCount - 1, false);
			float[] bodyWeights = Utils.calcDistribution(maxPlayerCount - 1, true);
			float[] bodyHeights = Utils.calcDistribution(maxPlayerCount - 1, true);
			float[] skinColors = Utils.calcDistribution(maxPlayerCount - 1, true);

			for (int i = 1; i < maxPlayerCount; i++) {
				ModifyClone(i, shuffleIndex != 0, beUpdate, fatigues[i - 1], bodyWeights[i - 1], 1f + bodyHeights[i - 1] * BODY_HEIGHT_RANGE, skinColors[i - 1]);
			}
		}

		void ModifyClone(int index, bool isRandom = false, bool beUpdate = false, float fatigue = 0f, float bodyWeight = 0f, float bodyHeight = 1.0f, float skinColor = 0f) {
			TaisoPlayer player = players[index];
			bool _isBoy = player.isBoy = (index == 0) ? isBoy : (index % 2) == 1;
			player.playerIndex = index;
			player.isDebugLog = (index == 0) || isSubPlayersLog;
			if (index == 0) {
				mainPlayer = player;
			} else if (index <= 2 && !isRandom) { // for capture icon
				player.isRandomStyle = false;
				player.setFace(_isBoy ? 3 : 1);
				player.setHeadWare(0);
				player.setHairStyle(_isBoy ? 0 : 7, 0);
				player.setClothStyle(_isBoy ? 0 : 2, _isBoy ? 3 : 6);
			} else {
				player.isRandomStyle = true;
				player.setFace(Random.Range(0, player.faceCount));
				player.setHeadWare(Utils.rnd(_isBoy ? 2 : 4) ? Random.Range(0, ChibiHair.getHeadWearColorCount(_isBoy)) : -1);
				player.setGlasses(Utils.rnd(5) ? ChibiHair.getRandomGlassStyle() : -1);
				player.setHairStyle(Random.Range(0, ChibiHair.getHairStyleCount(_isBoy)), Random.Range(0, ChibiHair.MAX_HAIR_COLOR_INDEX));
				player.setClothStyle(Random.Range(0, ChibiCloth.getClothStyleCount(_isBoy)), Random.Range(0, ChibiCloth.MAX_CLOTH_COLOR_INDEX));
			}
			player.fatigue = (fatigue < FATIGUE_TH) ? 0f : fatigue;
			player.setBodyWeight(Mathf.Abs(bodyWeight) < BODY_WEIGHT_TH ? 0f : bodyWeight);
			player.bodyHeight = bodyHeight;
			player.skinColorValue = skinColor;
			if (beUpdate) {
				player.updateChibi();
			}
		}

		void ModifyMainPlayer() {
			mainPlayer.setFace(faceSlider.getIntValue() - 1);
			mainPlayer.setSkinColorIndex(Prefs.getInt(PKEY_SKIN_COLOR_INDEX, 0));
			mainPlayer.setGlasses(Prefs.getInt(PKEY_GLASSES_INDEX, -1));
			int hairStyle = hairStyleSlider.getIntValue() - 1;
			int hairColorIndex = Prefs.getInt(PKEY_HAIR_COLOR_INDEX);
			mainPlayer.setHairStyle(hairStyle, hairColorIndex);
			int headWearIndex = Prefs.getInt(PKEY_HEAD_WEAR_INDEX, -1);
			mainPlayer.setHeadWare(headWearIndex);
			int clothStyle = clothStyleSlider.getIntValue() - 1;
			int clothColorIndex = Prefs.getInt(PKEY_CLOTH_COLOR_INDEX);
			mainPlayer.setClothStyle(clothStyle, clothColorIndex);
			mainPlayer.setBodyWeight(weightSlider.getValue());
			mainPlayer.bodyHeight = heightSlider.getValue();
			mainPlayer.fatigue = fatigueSlider.getValue();
		}

		void logClicked(Button button) {
			Debug.Log(button.name + " clicked");
		}

		void initSidePanel() {
			playerCountSlider = SliderController.findSlider("GroupPanel");
			playerCountSlider.setMinMaxValue(1, maxPlayerCount);
			playerCountSlider.setPrefsKey("PlayerCount", playerCount);
			playerCount = playerCountSlider.getIntValue();
			playerCountSlider.onValueChanged = (value) => {
				playerCount = (int)value;
				if (formation == 1) {
					setPlayersPosition();
				}
				for (int i = 1; i < players.Count; i++) {
					players[i].setVisible(i < playerCount);
				}
			};

			formationToggle = ToggleController.findToggle("GroupPanel");
			formationToggle.setPrefsKey("Formation", 0);
			formation = formationToggle.getSelected();
			formationToggle.onValueChanged = onFormationChanged;

			spacingButton = Utils.findComponent<Button>("GroupPanel", "SpacingButton");
			spacingButton.onClick.AddListener(onSpacingButtonClicked);
			shuffleButton = Utils.findComponent<Button>("GroupPanel", "ShuffleButton");
			shuffleButton.onClick.AddListener(onShuffleButtonClicked);
			spacingX = Prefs.getFloat(PKEY_PLAYER_SPACING, spacingX);

			shuffleIndexInput = Utils.findComponent<InputField>("GroupPanel", "ShuffleIndex");
			shuffleIndexInput.onEndEdit.AddListener(onShuffleIndexChanged);
			maxShuffleIndex = Prefs.getInt(PKEY_MAX_SHUFFLE_INDEX, maxShuffleIndex);

			onFormationChanged(formation);

			boyGirlToggle = ToggleController.findToggle("BoyGirlPanel");
			boyGirlToggle.setPrefsKey("IsBoy", 0);
			isBoy = boyGirlToggle.getSelected() == 0;
			boyGirlToggle.onValueChanged = (index) => {
				isBoy =  index == 0;
				Debug.Log("isBoy=" + isBoy);
				mainPlayer.isBoy = isBoy;
				// Debug.Log("hairStyleCount=" +  mainPlayer.hair.hairStyleCount);
				hairStyleSlider.setMinMaxValue(1, mainPlayer.hair.hairStyleCount);
				hairStyleSlider.setValue(1);
				mainPlayer.setHairStyle(0, Prefs.getInt(PKEY_HAIR_COLOR_INDEX, 0));
				clothStyleSlider.setMinMaxValue(1, mainPlayer.cloth.clothStyleCount);
				clothStyleSlider.setValue(1);
				mainPlayer.setClothStyle(0, 0);
				ChibiPlayer.createChibi(mainPlayer);
				swapBoyGirlIcons();
			};

			faceSlider = SliderController.findSlider("FacePanel");
			faceSlider.setMinMaxValue(1, ChibiPlayer._faceCount(isBoy));
			faceSlider.setPrefsKey("faceType", 1);
			faceSlider.onValueChanged = (value) => {
				ChibiPlayer.createFace(mainPlayer.gameObject, mainPlayer.isBoy, (int)value - 1);
			};

			hairStyleSlider = SliderController.findSlider("HairPanel");
			hairStyleSlider.setMinMaxValue(1, ChibiHair.getHairStyleCount(isBoy));
			hairStyleSlider.setPrefsKey("HairStyle", 1);
			hairStyleSlider.onValueChanged = (value) => {
				int hairColorIndex = Prefs.getInt(PKEY_HAIR_COLOR_INDEX);
				mainPlayer.hair.loadHair((int)value - 1, hairColorIndex);
				mainPlayer.updateRenderers();
			};

			clothStyleSlider = SliderController.findSlider("ClothPanel");
			clothStyleSlider.setMinMaxValue(1, ChibiCloth.getClothStyleCount(isBoy));
			clothStyleSlider.setPrefsKey("ClothStyle", 1);
			clothStyleSlider.onValueChanged = (value) => {
				int clothColorIndex = Prefs.getInt(PKEY_CLOTH_COLOR_INDEX);
				mainPlayer.cloth.loadCloth(mainPlayer.gameObject, (int)value - 1, clothColorIndex, false);
			};

			skinColorButton = Utils.findComponent<Button>("FacePanel", "Button1");
			skinColorButton.onClick.AddListener(onSkinColorButtonClicked);
			glassesButton =  Utils.findComponent<Button>("FacePanel", "Button2");
			glassesButton.onClick.AddListener(onGlassesButtonClicked);
			hairColorButton = Utils.findComponent<Button>("HairPanel", "Button1");
			hairColorButton.onClick.AddListener(onHairColorButtonClicked);
			headWearButton = Utils.findComponent<Button>("HairPanel", "Button2");
			headWearButton.onClick.AddListener(onHeadWearButtonClicked);
			clothColorButton = Utils.findComponent<Button>("ClothPanel", "Button1");
			clothColorButton.onClick.AddListener(onClothColorButtonClicked);
			resetButton = Utils.findComponent<Button>("SidePanel", "ResetButton");
			resetButton.onClick.AddListener(onResetButtonClicked);

			weightSlider = SliderController.findSlider("WeightPanel");
			weightSlider.setMinMaxValue(-1f, 1f, 0.1f);
			weightSlider.setPrefsKey("BodyWeight", 0f);
			weightSlider.onValueChanged = (value) => {
				mainPlayer.setBodyWeight(value);
				mainPlayer.valueChanged();
			};

			heightSlider = SliderController.findSlider("HeightPanel");
			heightSlider.setMinMaxValue(MIN_BODY_HEIGHT, MAX_BODY_HEIGHT, 0.05f);
			heightSlider.setPrefsKey("BodyHeight", 1f);
			heightSlider.onValueChanged = (value) => {
				mainPlayer.bodyHeight = value;
				mainPlayer.valueChanged();
			};

			fatigueSlider = SliderController.findSlider("FatiguePanel");
			fatigueSlider.setMinMaxValue(0f, 1f, 0.1f);
			fatigueSlider.setPrefsKey("Fatigue", 0f);
			fatigueSlider.onValueChanged = (value) => {
				mainPlayer.fatigue = value;
				mainPlayer.valueChanged();
			};

			if (!isBoy) {
				swapBoyGirlIcons();
			}
		}

		void initMediaPanel() {
			playButton = Utils.findComponent<Button>("PlayButton");
			nextButton = Utils.findComponent<Button>("NextButton");
			prevButton = Utils.findComponent<Button>("PrevButton");
			restartButton = Utils.findComponent<Button>("RestartButton");
			fastButton = Utils.findComponent<Button>("FastButton");
			playButton.onClick.AddListener(onPlayButtonClicked);
			nextButton.onClick.AddListener(onNextButtonClicked);
			prevButton.onClick.AddListener(onPrevButtonClicked);
			restartButton.onClick.AddListener(onRestartButtonClicked);
			fastButton.onClick.AddListener(onFastButtonClicked);
			fastButtonColor = fastButton.colors.normalColor;

			timelineSlider = TimelineController.findSlider("TimelineSlider");
			timelineSlider.setMinMaxValue(0f, audioSource.clip.length);
			timelineSlider.setTimeline(taisoTimes);
			timelineSlider.onValueChanged = onTimelineChanged;

			speakerButton = Utils.findComponent<Button>("SpeakerButton");
			speakerButton.onClick.AddListener(onSpeakerButtonClicked);
			setSpeakerMute();
			volumeSlider = Utils.findComponent<Slider>("VolumeSlider");
			volumeSlider.value = Prefs.getInt(PKEY_AUDIO_VOLUME, (int)volumeSlider.maxValue);
			setSpeakerVolume();
			volumeSlider.onValueChanged.AddListener(onVolumeChanged);
		}

		void initCanvasButton() {
			panelButton = Utils.findComponent<Button>("PanelButton");
			panelButton.onClick.AddListener(onPanelButtonClicked);
			rtSidePanel = Utils.findComponent<RectTransform>("SidePanel");
			rtMediaPanel = Utils.findComponent<RectTransform>("MediaPanel");
			isShowPanel = Prefs.getBool(PKEY_SHOW_PANEL);
			if (!isShowPanel) {
				showHidePanels();
			}
			cameraController.setDisableMotion(isShowPanel);

			screenButton = Utils.findComponent<Button>("ScreenButton");
			screenButton.onClick.AddListener(onScreenButtonClicked);
		}

		void swapBoyGirlIcons() {
			swapBoyGirlIcons(faceSlider, "button_face", true);
			swapBoyGirlIcons(hairStyleSlider, "button_hair", true);
			swapBoyGirlIcons(clothStyleSlider, "button_cloth", true);
			swapBoyGirlIcons(weightSlider, "icon_weight");
			swapBoyGirlIcons(heightSlider, "icon_height");
			swapBoyGirlIcons(fatigueSlider, "icon_fatigue");
		}

		void swapBoyGirlIcons(SliderController controller, string iconName, bool isButton = false) {
			List<Image> images = Utils.findChildren<Image>(controller.gameObject, isButton ? "Button" : "Image");
			// Debug.Log(controller.name + ": images=" + images.Count);
			if (images.Count == 1) {
				swapBoyGirlIcon(images[0], iconName);
			} else if (images.Count == 2) {
				swapBoyGirlIcon(images[0], iconName + "1");
				swapBoyGirlIcon(images[1], iconName + "2");
			}
		}

		void swapBoyGirlIcon(Image image, string iconName) {
			try {
				image.sprite = spriteMap[iconName + (isBoy ? "_b" : "_g")];
			} catch (KeyNotFoundException) {}
		}

		void onFormationChanged(int selected) {
			if (selected == 0) {
				frontCount = 2;
				rowIncrement = 1;
			} else {
				frontCount = columnCount;
				rowIncrement = 0;
			}
			if (players.Count > 0) {
				setPlayersPosition();
			}
		}

		void setPlayersPosition() {
			for (int i = 1; i < players.Count; i++) {
				float x, z;
				getClonePosition(i, out x, out z);
				players[i].gameObject.transform.position = new Vector3(x, 0, -z);
			}
		}

		public (float x, float z) getPlayersSpan() {
			float spanX = 0;
			float spanZ = 0;

			for (int i = 0; i < playerCount; i++) {
				TaisoPlayer player = players[i];
				float x = player.gameObject.transform.position.x;
				float z = player.gameObject.transform.position.z;
				spanX = Mathf.Max(Mathf.Abs(x), spanX);
				spanZ = Mathf.Max(z, spanZ);
			}
			return (spanX, spanZ);
		}

		void onSpacingButtonClicked() {
			logClicked(spacingButton);
			spacingX = (spacingX == 1f) ? 1.5f : (spacingX == 1.5f) ? 2f : 1f;
			Prefs.setFloat(PKEY_PLAYER_SPACING, spacingX);
			setPlayersPosition();
		}

		void onShuffleButtonClicked() {
			logClicked(shuffleButton);
			int shuffleIndex = Prefs.getInt(PKEY_SHUFFLE_INDEX, 0);
			if (shuffleIndex >= 0) {
				if (++shuffleIndex >= maxShuffleIndex) {
					shuffleIndex = 0;
				}
				Prefs.setInt(PKEY_SHUFFLE_INDEX, shuffleIndex);
			}
			ModifyClones(true);
		}

		void onShuffleIndexChanged(string text) {
			int n = int.Parse(text);
			if (n >= 0) {
				maxShuffleIndex = n;
				Prefs.setInt(PKEY_SHUFFLE_INDEX, n - 1);
				Prefs.setInt(PKEY_MAX_SHUFFLE_INDEX, n);
				ModifyClones(true);
			}
		}

		void onSkinColorButtonClicked() {
			logClicked(skinColorButton);
			int skinColorIndex = Prefs.getInt(PKEY_SKIN_COLOR_INDEX, 0);
			if (++skinColorIndex >= ChibiPlayer.skinColorCount) {
				skinColorIndex = 0;
			}
			Prefs.setInt(PKEY_SKIN_COLOR_INDEX, skinColorIndex);
			mainPlayer.setSkinColorIndex(skinColorIndex);
			mainPlayer.setSkinColor();
		}

		void onGlassesButtonClicked() {
			logClicked(glassesButton);
			int glassesIndex = Prefs.getInt(PKEY_GLASSES_INDEX, -1);
			if (++glassesIndex >= ChibiHair.glassesStyleCount) {
				glassesIndex = -1;
			}
			Prefs.setInt(PKEY_GLASSES_INDEX, glassesIndex);
			mainPlayer.hair.setGlasses(glassesIndex);
		}

		void onHairColorButtonClicked() {
			logClicked(hairColorButton);
			int hairColorIndex = Prefs.getInt(PKEY_HAIR_COLOR_INDEX, 0);
			if (++hairColorIndex >= ChibiHair.MAX_HAIR_COLOR_INDEX) {
				hairColorIndex = 0;
			}
			Prefs.setInt(PKEY_HAIR_COLOR_INDEX, hairColorIndex);
			mainPlayer.hair.setHairColor(hairColorIndex);
		}

		void onHeadWearButtonClicked() {
			logClicked(headWearButton);
			int headWearIndex = Prefs.getInt(PKEY_HEAD_WEAR_INDEX, -1);
			if (++headWearIndex >= ChibiHair.getHeadWearColorCount(isBoy)) {
				headWearIndex = -1;
			}
			Prefs.setInt(PKEY_HEAD_WEAR_INDEX, headWearIndex);
			mainPlayer.hair.setHeadWear(headWearIndex);
		}

		void onClothColorButtonClicked() {
			logClicked(clothColorButton);
			int clothColorIndex = Prefs.getInt(PKEY_CLOTH_COLOR_INDEX, 0);
			if (++clothColorIndex >= ChibiCloth.MAX_CLOTH_COLOR_INDEX) {
				clothColorIndex = 0;
			}
			Prefs.setInt(PKEY_CLOTH_COLOR_INDEX, clothColorIndex);
			int clothStyle = clothStyleSlider.getIntValue() - 1;
			mainPlayer.cloth.loadCloth(mainPlayer.gameObject, clothStyle, clothColorIndex, false);
		}

		void onResetButtonClicked() {
			logClicked(resetButton);
			faceSlider.setValue(1);
			Prefs.setInt(PKEY_SKIN_COLOR_INDEX, 0);
			Prefs.setInt(PKEY_GLASSES_INDEX, -1);
			hairStyleSlider.setValue(1);
			Prefs.setInt(PKEY_HAIR_COLOR_INDEX, 0);
			Prefs.setInt(PKEY_HEAD_WEAR_INDEX, -1);
			clothStyleSlider.setValue(1);
			Prefs.setInt(PKEY_CLOTH_COLOR_INDEX, 0);
			weightSlider.setValue(0f);
			heightSlider.setValue(1f);
			fatigueSlider.setValue(0f);
			ModifyMainPlayer();
			mainPlayer.valueChanged();
			ChibiPlayer.createChibi(mainPlayer);
			mainPlayer.hair.setHairColor(0);
			mainPlayer.hair.setHeadWear(-1);
			mainPlayer.hair.setGlasses(-1);
		}

		void onPlayButtonClicked() {
			logClicked(playButton);
			isPaused = !isPaused;
			setPaused();
		}

		void setPaused() {
			playButton.image.overrideSprite = isPaused ? spriteMap["button_play"] : null;
			Time.timeScale = isPaused ? 0f : 1f;
			if (isPaused) {
				audioSource.Pause();
			} else {
				audioSource.UnPause();
			}
		}

		void onNextButtonClicked() {
			logClicked(nextButton);
			playTaisoFrom(taisoIndex + 1);
		}

		void onPrevButtonClicked() {
			logClicked(prevButton);
			playTaisoFrom(taisoIndex - 1);
		}

		void onRestartButtonClicked() {
			logClicked(restartButton);
			playTaisoFrom(0);
		}

		void onFastButtonClicked() {
			logClicked(fastButton);
			speed = (speed == 1f) ? 2f : 1f;
			Time.timeScale = speed;
			audioSource.pitch = speed;
			var colors = fastButton.colors;
			colors.normalColor = (speed == 1f) ? fastButtonColor : colors.selectedColor;
			fastButton.colors = colors;
		}

		void onTimelineChanged(float value) {
			if (timelineSlider.isDragging()) return;
			int index = taisoTimes.Length - 1;
			for (int i = 1; i < taisoTimes.Length; i++) {
				if (value <= taisoTimes[i]) {
					index = i - 1;
					break;
				}
			}
			if (index != taisoIndex) {
				Debug.Log("onTimelineChanged " + taisoIndex + "->" + index);
				playTaisoFrom(index);
				return;
			}
		}

		void onSpeakerButtonClicked() {
			logClicked(speakerButton);
			bool isMute = Prefs.getBool(PKEY_AUDIO_MUTE);
			Prefs.setBool(PKEY_AUDIO_MUTE, !isMute);
			setSpeakerMute();
		}

		void onVolumeChanged(float value) {
			Prefs.setInt(PKEY_AUDIO_VOLUME, (int)value);
			setSpeakerVolume();
		}

		void setSpeakerMute() {
			bool isMute = Prefs.getBool(PKEY_AUDIO_MUTE);
			audioSource.mute = isMute;
			speakerButton.image.overrideSprite = isMute ? spriteMap["button_mute"] : null;
		}

		void setSpeakerVolume() {
			int volume = Prefs.getInt(PKEY_AUDIO_VOLUME, (int)volumeSlider.maxValue);
			audioSource.volume = (float)volume / volumeSlider.maxValue;
		}


		void showHidePanels() {
			rtSidePanel.anchoredPosition = new Vector2(isShowPanel ? 0 : rtSidePanel.sizeDelta.x, rtSidePanel.anchoredPosition.y);
			rtMediaPanel.anchoredPosition = new Vector2(0, isShowPanel ? PANEL_MARGIN : -rtMediaPanel.sizeDelta.y);
			panelButton.image.overrideSprite = isShowPanel ? null : spriteMap["button_open"];
			cameraController.setDisableMotion(isShowPanel);
		}

		void onPanelButtonClicked() {
			logClicked(panelButton);
			if (openPanelFrame != -1) return;
			isShowPanel = !isShowPanel;
			Prefs.setBool(PKEY_SHOW_PANEL, isShowPanel);
			openPanelFrame = Time.frameCount;
			StartCoroutine(openClosePanels());
		}

		void onScreenButtonClicked() {
			logClicked(screenButton);
			bool isFullScreen = Screen.fullScreen;
			Screen.fullScreen = !Screen.fullScreen;
			screenButton.image.overrideSprite = isFullScreen ? null : spriteMap["button_window"];
		}

		IEnumerator openClosePanels() {
			for (;;) {
				float t = getTransitionDelta(isShowPanel);
				if (float.IsNaN(t)) break;
				rtSidePanel.anchoredPosition = new Vector2(rtSidePanel.sizeDelta.x * t, rtSidePanel.anchoredPosition.y);
				rtMediaPanel.anchoredPosition = new Vector2(0, PANEL_MARGIN - rtMediaPanel.sizeDelta.y * t);
				yield return null;
			}
			showHidePanels();
			yield break;
		}

		float getTransitionDelta(bool isShow) {
			float elapse = (float)(Time.frameCount - openPanelFrame) / TaisoPlayer.FPS;
			if (elapse >= OPENPANEL_ELAPSE) {
				openPanelFrame = -1;
				return float.NaN;
			}
			float t = elapse / OPENPANEL_ELAPSE;
			if (isShow) {
				t = 1f - t;
			}
			return t * t * t; // easing
		}

		void initLogWindow() {
			GameObject logView = GameObject.Find("LogView");
			GameObject logButtonObject = GameObject.Find("LogButton");
			if (logView) {
				if (!showLogWindow) {
					logView.SetActive(false);
					logButtonObject.SetActive(false);
				} else {
					logText = logView.GetComponentInChildren<Text>();
					logScrollRect = logView.GetComponent<ScrollRect>();
					rtLogPanel = logView.GetComponent<RectTransform>();
					Application.logMessageReceived += HandleLog;
					logButton = logButtonObject.GetComponentInChildren<Button>();
					logButton.onClick.AddListener(onLogButtonClicked);
					isShowLogPanel = Prefs.getBool(PKEY_SHOW_LOGPANEL);
					showHideLogPanel();
				}
			} else {
				showLogWindow = false;
			}
		}

		void HandleLog(string logString, string stackTrace, LogType type) {
			logText.text += logString + "\n";
			logScrollRect.verticalNormalizedPosition = 0;
		}

		void showHideStartPanel(bool isShow) {
			GameObject panel = GameObject.Find("StartPanel");
			if (isShow) {
				Text text = panel.GetComponentInChildren<Text>();
				text.text = "Hit Space to Start";
			} else {
				panel.SetActive(false);
			}
		}

		void showHideLogPanel() {
			logButton.image.overrideSprite = isShowLogPanel ? spriteMap["button_hide"] : null;
			rtLogPanel.anchoredPosition = new Vector2(isShowLogPanel ? 0 : -rtLogPanel.sizeDelta.x, 0);
		}

		void onLogButtonClicked() {
			logClicked(logButton);
			if (openPanelFrame != -1) return;
			isShowLogPanel = !isShowLogPanel;
			Prefs.setBool(PKEY_SHOW_LOGPANEL, isShowLogPanel);
			openPanelFrame = Time.frameCount;
			StartCoroutine(openCloseLogPanel());
		}

		IEnumerator openCloseLogPanel() {
			for (;;) {
				float t = getTransitionDelta(isShowLogPanel);
				if (float.IsNaN(t)) break;
				rtLogPanel.anchoredPosition = new Vector2(-rtLogPanel.sizeDelta.x * t, 0);
				yield return null;
			}
			showHideLogPanel();
			yield break;
		}

		void initInfoPanel() {
			Text versionText = GameObject.Find("VersionText").GetComponent<Text>();
			versionText.text = Application.version;
			StartCoroutine(fadeoutInfoPanel());
		}

		IEnumerator fadeoutInfoPanel() {
			GameObject infoPanel = GameObject.Find("InfoPanel");
			CanvasGroup canvas = infoPanel.GetComponent<CanvasGroup>();
			canvas.alpha = 1f;
			yield return new WaitForSeconds(INTRO_TIME / 2);
			float startTime = Time.time;
			const float ELAPSE = 0.5f;
			for (;;) {
				float t = (Time.time - startTime) / ELAPSE;
				if (t >= 1f) break;
				canvas.alpha = 1f - t;
				yield return null;
			}
			infoPanel.SetActive(false);
			yield break;
		}

		void onKeyDown() {
			if (Input.GetKeyDown(KeyCode.Space)) {
				onPlayButtonClicked();
			} else if (Input.GetKeyDown(KeyCode.Tab)) {
				onPanelButtonClicked();
			}
		}
	}
}
