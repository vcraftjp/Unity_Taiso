using UnityEngine;
using UnityEditor;

namespace VCraft
{
	public class TaisoPlayer : ChibiPlayer
	{
		public const int TAISO_COUNT = 13;

		static readonly int[] loopCounts = { 2, 8, 4, 4, 2, 2, 1, 2, 2, 2, 2, 8, 4 };
		static readonly int[] frameCounts = { 100, 50, 100, 100, 200, 200, 400, 200, 200, 200, 200, 50, 100 };
		static readonly int[] nextFrames = { 75, 0, 92, 0, 0, 0, 0, 0, 180, 180, 0, 0, 0 };
		static readonly int[] frameOffsets = { 0, 15, 15, 15, 15, 10, 10, 10, 10, 10, 15, 10, 0 };

		Taiso taiso;
		Animator animator;

		float startTime;
		float startTaisoTime;
		float pauseTime;
		float speed = 1f;
		static float[] startTaisoTimes = new float[TAISO_COUNT + 1];

		int taisoIndex = 0;
		int prevTaisoIndex = -1;
		int loopCount = 0;
		bool isPaused = false;
		int loopCountPausing = 0;
		int startFrame = -1;
		int endFrame = -1;

		const int MIN_PAUSE_FRAME = 10;

		new void Awake() {
			log("Awake");
			base.Awake();
			taiso = FindObjectOfType<Taiso>();
			if (taiso == null) {
				noLoadHairCloth = true;
			}
		}

		new void Start() {
			log("Start");
			base.Start();
			animator = GetComponent<Animator>();
			startTime = Time.time;
			valueChanged();
			updateVisible();
		}

		new void Update() {
			base.Update();

			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (taisoIndex == 0) {
				if (stateInfo.length * stateInfo.normalizedTime >= Taiso.INTRO_TIME) {
					nextTaiso();
				}
			} else if (taisoIndex != -1) {
				if (taisoIndex == prevTaisoIndex) {
					// exclude TaisoXXs transition
					bool isLatter = Time.frameCount - startFrame > frameCounts[taisoIndex - 1] / 2;
					if (stateInfo.normalizedTime >= 1f && !isPaused && isLatter) {
						EndTaiso();
					} else {
						int nextFrame = nextFrames[taisoIndex - 1];
						int maxLoop = loopCounts[taisoIndex - 1];
						if (nextFrame != 0 && loopCount >= maxLoop - 1) {
							if (Time.time - startTime >= (float)nextFrame / FPS) {
								nextTaiso();
							}
						}
					}
				}
				pausing();
			}
		}

	//	void OnApplicationQuit() {
	//	}

		private void restart() {
			log("restart");
			startTime = Time.time;
			animator.Play(0, -1, 0f);
		}

		private void nextTaiso() {
			if (taisoIndex < TAISO_COUNT) {
				taisoIndex++;
				isPaused = false;
				animator.speed = speed;
				setTrigger("NextTaiso");
			}
		}

		private void pausing() {
			if (isPaused) {
				if (Time.time - startTaisoTimes[taisoIndex] >= ((float)frameCounts[taisoIndex - 1] * loopCountPausing) / FPS + pauseTime) {
					isPaused = false;
					animator.speed = speed;
				}
			}
		}

		private void setTrigger(string name) {
			log("trigger: " + name);
			animator.SetTrigger(name);
		}


		public void StartTaiso() {
			if (Time.frameCount == startFrame) return;
			isPaused = false;
			startFrame = Time.frameCount;
			if (taisoIndex != prevTaisoIndex) {
				if (taisoIndex == 0) {
					taisoIndex = getTaisoIndexFromClip();
				}
				prevTaisoIndex = taisoIndex;
				loopCount = 0;
				startTaisoTime = Time.time;
				if (fatigue == 0f) {
					startTaisoTimes[taisoIndex] = startTaisoTime;
				}
				if (isMainPlayer()) {
					taiso.startTaiso(taisoIndex);
				}
				speed = adjustedSpeed(taisoIndex);
			} else {
				loopCount++;
			}
			animator.speed = speed;
			animator.SetInteger("LoopCount", loopCount);
			animator.SetInteger("TaisoIndex", taisoIndex);
			startTime = Time.time;
			log("StartTaiso #" + taisoIndex + " (" + (loopCount + 1) + ")");
		}

		public void EndTaiso() {
			if (Time.frameCount == endFrame) return;
			endFrame = Time.frameCount;
			log("EndTaiso #" + taisoIndex + " (" + (loopCount + 1) + ")");

			int maxLoop = loopCounts[taisoIndex - 1];
			if (loopCount < maxLoop - 1) {
				restart();
			} else if (taisoIndex < TAISO_COUNT) {
				nextTaiso();
			} else {
				taisoIndex = -1;
			}
		}

		public void PauseTaiso() {
			if (Time.frameCount - startFrame < MIN_PAUSE_FRAME) return;
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			pauseTime = stateInfo.length * stateInfo.speedMultiplier * stateInfo.normalizedTime;
			log("PauseTaiso #" + taisoIndex + " frame=" + (pauseTime * FPS));
			isPaused = true;
			loopCountPausing = loopCount;
			animator.speed = 0;
		}

		public void PlayTaiso(int index) {
			taisoIndex = index;
			string stateName = "Base Layer.TaisoBlend" + index.ToString("00");
			int frameOffset = (index == 0) ? 0 : frameOffsets[index - 1];
			if (frameOffset == 0) {
				animator.Play(stateName, -1, 0f);
			} else {
				animator.Play(stateName, -1, (float)frameOffset / frameCounts[index - 1]);
				StartTaiso();
			}
		}

		int getTaisoIndexFromClip() {
			AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
			string name = clipInfo[0].clip.name;
			return int.Parse(name.Substring(5, 2));
		}

		float adjustedSpeed(int index) {
			if (index == 0) {
				return 1f;
			}
			float diff = taiso.getTimeDifference(index);
			float taisoTime = (index == 0) ? Taiso.INTRO_TIME : (frameCounts[index - 1] * loopCounts[index - 1]) / FPS;
			float speed1 = (taisoTime + diff) / taisoTime;
			log("time diff=" + diff + ", speed=" + speed1);
			if (speed1 < 0) {
				speed1 = 1f;
			}
			return speed1;
		}

		public void valueChanged() {
			if (base._ValueChanged()) {
				if (animator) {
					animator.SetFloat("Blend", fatigue);
					animator.SetFloat("Speed", 1.0f + fatigue);
				}
			}
		}

		public override void updateChibi() {
			base.updateChibi();
			valueChanged();
		}
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(TaisoPlayer))]
	public class TaisoPlayerEditor : Editor
	{
		public override void OnInspectorGUI() {
	//        serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			DrawDefaultInspector();
//			drawChibiInspector();

			if (EditorApplication.isPlaying && EditorGUI.EndChangeCheck()) {
				var taisoPlayer = target as TaisoPlayer;
				taisoPlayer.valueChanged();
			}

		}
/*
		void drawChibiInspector() {
			var player = target as TaisoPlayer;

			string[] buttonNames = { "Boy", "Girl" };
			GUIStyle style_radio = new GUIStyle(EditorStyles.radioButton);
			int selected = player.isBoy ? 0 : 1;
			int selected1 = GUILayout.SelectionGrid(selected, buttonNames, 2, style_radio);
			if (selected != selected1) {
				player.isBoy = !player.isBoy;
				if (EditorApplication.isPlaying) {
					ChibiPlayer.createChibi(player);
				}
			}

			if (EditorApplication.isPlaying) {
				ChibiHair hair = player.hair;
				if (hair != null) {
					int index = EditorGUILayout.IntSlider("HairStyle", hair.hairStyle + 1, 1, hair.hairStyleCount) - 1;
					if (index != hair.hairStyle) {
						hair.loadHair(index, 0);
					}
				}
			}
		}
*/
	}
	#endif
}
