using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VCraft
{
	public class ChibiPlayer : MonoBehaviour
	{
		public static readonly int FPS = 30;

		[Range(0f, 1f)]
		public float fatigue = 0f;
		float _fatigue = float.NaN;

		[Range(-1f, 1f)]
		public float bodyWeight = 0f;
		float _bodyWeight = float.NaN;

		[Range(0.5f, 1.5f)]
		public float bodyHeight = 1f;
		float _bodyHeight = float.NaN;

		[Range(-1f, 1f)]
		public float headShape = 0f;
		float _headShape = float.NaN;

		public bool isDebugLog { get; set; } = true;
		public bool isBoy { get; set; }
		public bool isRandomStyle { get; set; } = true;

		public int playerIndex { get; set; }

		[System.NonSerialized]
		public ChibiHair hair;
		int hairStyle = -1;
		int hairColorIndex = -1;
		int headWear = -1;

		[System.NonSerialized]
		public ChibiCloth cloth;
		int clothStyle = -1;
		int clothColorIndex = -1;

		protected bool noLoadHairCloth;

		const float skinHue = 0.058f;
		static readonly Color skinColorDefault = Color.HSVToRGB(skinHue, 0.38f, 0.96f); // new Color32(245, 185, 152, 255);
		static readonly Color skinColorLight = Color.HSVToRGB(skinHue, 0.15f, 1.00f);
		static readonly Color skinColorDark = Color.HSVToRGB(skinHue, 0.6f, 0.8f);
		public float skinColorValue { get; set; } = 0f; // -1: lightest, 1:darkest;

		static readonly float[] skinColorValues = { 0f, -1f, -0.5f, 0.5f, 1f };
		public static int skinColorCount = skinColorValues.Length;

		static Dictionary<string, Texture> faceTextureMap = null;
		static int faceCountBoy = 0;
		static int faceCountGirl = 0;
		int faceIndex = -1;
		int glassesIndex = -1;

		public int faceCount {
			get {
				return isBoy ? faceCountBoy: faceCountGirl;
			}
		}

		public static int _faceCount(bool isBoy) {
			return isBoy ? faceCountBoy: faceCountGirl;
		}

		Renderer[] renderers;
		List<SkinnedMeshRenderer> bodyRenderers = new List<SkinnedMeshRenderer>();
		SkinnedMeshRenderer headRenderer;

		Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
		float hipPosY;
		List<string> boneArmLegNames = new List<string>(){ "ArmUp_L", "ArmUp_R", "LegUp_L", "LegUp_R" };


		public void log(string msg) {
			if (isDebugLog) {
				Debug.Log(gameObject.name + ": " + msg);
			}
		}

		protected void Awake() {
		}

		protected void Start() {
			hair = new ChibiHair(this);
			cloth = new ChibiCloth(this);

			createFace(this.gameObject, isBoy, faceIndex, fatigue);
			setSkinColor();

			if (!noLoadHairCloth) {
				int index = clothStyle != -1 ? clothStyle : -1;
				int colorIndex = clothColorIndex != -1 ? clothColorIndex : -1;
				cloth.loadCloth(this.gameObject, index, colorIndex, !isMainPlayer() && isRandomStyle);
			}

			SkinnedMeshRenderer[] renderers = this.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			foreach (SkinnedMeshRenderer renderer in renderers) {
				if (renderer.gameObject.name == "Head") {
					headRenderer = renderer;
					hair.setHeadRenderer(renderer);
				} else {
					bodyRenderers.Add(renderer);
				}
			}
			setFaceType();

			Transform[] tfs = this.GetComponentsInChildren<Transform>();
			foreach (Transform tf in tfs) {
				if (tf.name.StartsWith("B_")) {
					string name = tf.name.Substring(2);
					boneMap.Add(name, tf);
					if (name == "Hip") {
						hipPosY = tf.position.y;
					}
				}
			}
			hair.setHeadBone(boneMap["Head"].gameObject);
			if (!noLoadHairCloth) {
				setupHeadAndHair();
			}
			updateRenderers();
			setColliderSize();
		}

		protected void Update() {
		}

		public bool isMainPlayer() {
			return playerIndex == 0;
		}

		public static void createChibi(GameObject root, bool isBoy) { // called by ToolMenu.cs
			TaisoPlayer player = root.GetComponent<TaisoPlayer>();
			player.isBoy = isBoy;
			player.hair = new ChibiHair(player);
			player.cloth = new ChibiCloth(player);
			loadChibiFaces();
			createChibi(player);
		}

		public static void createChibi(ChibiPlayer player) {
			bool isBoy = player.isBoy;
			Debug.Log("create " + player.name + " (" + (isBoy ? "Boy" : "Girl") + ")");
			GameObject root = player.gameObject;
			ChibiPlayer.createFace(root, isBoy, 0);
			player.hair.create(root);
			player.cloth.create(root);
		}

		public static void loadChibiFaces() {
			if (faceTextureMap == null) {
				faceTextureMap = Utils.loadAssets<Texture>("Textures", typeof(Texture));
				foreach(string name in faceTextureMap.Keys) {
					if (name.StartsWith("faceb")) {
						faceCountBoy++;
					} else if (name.StartsWith("faceg")) {
						faceCountGirl++;
					}
				}
			}
		}

		public static void createFace(GameObject root, bool isBoy, int index, float fatigue = 0f) {
			GameObject head = VCraft.Utils.findChildren(root, "Head");
			if (head) {
				Renderer renderer = head.GetComponentInChildren<Renderer>();
				Material material;
#if UNITY_EDITOR
				if (!EditorApplication.isPlaying) {
					if (!renderer.sharedMaterial) {
						renderer.sharedMaterial = Resources.Load("Materials/Head", typeof(Material)) as Material;
					}
					material = renderer.sharedMaterial;
				} else {
					material = renderer.material;
				}
#else
				material = renderer.material;
#endif
				string name = (isBoy ? "faceb" : "faceg") + (index + 1).ToString();
				material.color = skinColorDefault;
				material.SetTexture("_MainTex", faceTextureMap[name]);
				material.mainTextureScale = new Vector2(0.5f, 0.5f);
			}
		}

		public virtual void updateChibi() {
			ChibiPlayer.createFace(this.gameObject, isBoy, faceIndex, fatigue);
			setSkinColor();
			setFaceType();
			cloth.loadCloth(this.gameObject, clothStyle, clothColorIndex, !isMainPlayer() && isRandomStyle);
			setupHeadAndHair();
			updateRenderers();
		}

		void setFaceType() {
			headRenderer.sharedMaterial.mainTextureOffset = (fatigue < 0.5) ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0f);
		}

		public bool isVisible { get; set; } = true;

		public void setVisible(bool isVisible) {
			this.isVisible = isVisible;
			updateVisible();
		}

		protected void updateVisible() {
			if (renderers != null) {
				foreach (Renderer renderer in renderers) {
					if (renderer != null) {
						renderer.enabled = isVisible;
					}
				}
			}
		}

		public void updateRenderers() {
			renderers = this.GetComponentsInChildren<Renderer>(true);
			updateVisible();
		}

		public void setSkinColorIndex(int index) {
			skinColorValue = skinColorValues[index];
		}

		public void setSkinColor() {
			float t = skinColorValue;
			Color color = (t < 0) ? Color.Lerp(skinColorLight, skinColorDefault, t + 1f) : Color.Lerp(skinColorDefault, skinColorDark, t);
			Utils.setMaterialColor(this.gameObject, new string[] { "Body", "Head", "Hand_R", "Hand_L" }, color);
		}

		public void setFace(int index) {
			faceIndex = index;
		}

		public void setGlasses(int index) {
			glassesIndex = index;
		}

		public void setHairStyle(int index, int colorIndex) {
			hairStyle = index;
			hairColorIndex = colorIndex;
		}

		public void setClothStyle(int index, int colorIndex) {
			clothStyle = index;
			clothColorIndex = colorIndex;
		}

		public void setHeadWare(int index) {
			headWear = index;
		}

		void setupHeadAndHair() {
			hair.loadHair(hairStyle, hairColorIndex);
			hair.setHeadWear(headWear);
			hair.setGlasses(glassesIndex);
		}

		public void setBodyWeight(float value) {
			bodyWeight = value * 0.8f;
			headShape = -value;
		}

		void setColliderSize() {
			CapsuleCollider collider = this.GetComponent<CapsuleCollider>();
			Vector3 pos = boneMap["Head"].TransformPoint(0f, hair.topOfHead, 0f);
			collider.height = pos.y / this.transform.localScale.y;
			collider.center = new Vector3(0f, collider.height / 2, 0f);
			collider.radius = 0.2f + ((bodyWeight > 0f) ? bodyWeight * 0.2f : 0);
		}

		protected bool _ValueChanged() {
			bool isResult = false;
			if (fatigue != _fatigue) {
				_fatigue = fatigue;
				setFaceType();
				isResult = true;
			}
			bool isUpdated = false;
			if (bodyWeight != _bodyWeight || bodyHeight != _bodyHeight) {
				isUpdated = true;
				_bodyWeight = bodyWeight;
				_bodyHeight = bodyHeight;

				morphBodyWeight(bodyWeight);
				setBodyHeight(bodyHeight, bodyWeight);
				setColliderSize();
			}

			if (headShape != _headShape) {
				isUpdated = true;
				_headShape = headShape;

				morphHeadShape(headShape);
				hair.resetHairPosition();
			}

			if (isUpdated) {
				setHeadScale(headShape, bodyHeight);
			}

			return isResult;
		}

		void morphBodyWeight(float bodyWeight) {
			foreach (SkinnedMeshRenderer renderer in bodyRenderers) {
				if (bodyWeight >= 0) {
					renderer.SetBlendShapeWeight(0, bodyWeight * 100f);
					renderer.SetBlendShapeWeight(1, 0f);
				} else {
					renderer.SetBlendShapeWeight(0, 0f);
					renderer.SetBlendShapeWeight(1, -bodyWeight * 100f);
				}
			}
		}

		void morphHeadShape(float headShape) {
			if (headShape >= 0) {
				headRenderer.SetBlendShapeWeight(0, headShape * 100f);
				headRenderer.SetBlendShapeWeight(1, 0f);
			} else {
				headRenderer.SetBlendShapeWeight(0, 0f);
				headRenderer.SetBlendShapeWeight(1, -headShape * 100f);
			}
		}

		void setBodyHeight(float bodyHeight, float bodyWeight) {
			float scale = bodyHeight;
			this.transform.localScale = new Vector3(scale, scale, scale);
		}

		void setHeadScale(float headShape, float bodyHeight) {
			const float headScaleRate = 0.2f;

			float scale;
			if (headShape >= 0) {
				headRenderer.SetBlendShapeWeight(0, headShape * 100f);
				headRenderer.SetBlendShapeWeight(1, 0f);
				scale = 1 - (headShape * headScaleRate);
			} else {
				headRenderer.SetBlendShapeWeight(0, 0f);
				headRenderer.SetBlendShapeWeight(1, -headShape * 100f);
				scale = 1 + (-headShape * headScaleRate);
			}
			scale = scale / Mathf.Sqrt(bodyHeight);
			setBoneScale("Head", scale, scale);
		}

		void setBoneScale(string name, float scaleY, float scaleXZ = 1f) {
			boneMap[name].localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
		}
	}
}
