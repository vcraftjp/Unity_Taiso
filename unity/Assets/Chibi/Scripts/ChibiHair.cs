using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VCraft
{
	public class ChibiHair : Object
	{
		public const int MAX_HAIR_COLOR_INDEX = 4;

		ChibiPlayer player;

		bool isBoy {
			get {
				return player.isBoy;
			}
		}

		GameObject headBone;
		SkinnedMeshRenderer headRenderer;
		public float topOfHead;
		int topOfHeadIndex;

		GameObject hair;
		static bool initialized = false;
		static int hairCountBoy = 0;
		static int hairCountGirlUp = 0;
		static int hairCountGirlLo = 0;

		public int hairStyle { get; set; }
		Color hairColor;
		readonly Color brightHairColor = new Color(0.6f, 0f, 0f, 1f);

		public static int getHairStyleCount(bool isBoy) {
			return isBoy ? hairCountBoy: hairCountGirlUp * hairCountGirlLo;
		}

		public int hairStyleCount {
			get {
				return getHairStyleCount(isBoy);
			}
		}

		static readonly int[,] capColors = { { 0xFFFFFF, 0 }, { 0, 0xFFE000 }, { 0x0000FF, -1 }, { 0x0080FF, -1 }, { 0xE00000, -1 } };
		static readonly uint[] bandColors = { 0xFFFF00, 0xFF0000, 0xFF00FF, 0xFF80FF, 0xFF8000 };

		int headWear = -1;
		public static int getHeadWearColorCount(bool isBoy) {
			return isBoy ? capColors.GetLength(0) : bandColors.Length;
		}

		static readonly uint[,] glassesColors = { { 0x0, 0x00FFFFFF }, { 0x800000, 0x00FFFFFF }, { 0x0, 0xF0FFFFFF } };
		public static int glassesStyleCount {
			get {
				return glassesColors.GetLength(0) * 2;
			}
		}

		public ChibiHair(ChibiPlayer player) {
			this.player = player;
		}

		public static void init() {
			if (!initialized) {
				initialized = true;
				GameObject root = GameObject.Find("hairs");
				if (root != null) {
					foreach (Transform t in root.transform) {
						if (t.name.StartsWith("Hair")) {
							if (t.name.StartsWith("Hairb")) {
								hairCountBoy++;
							} else if (t.name.StartsWith("Hairgu")) {
								hairCountGirlUp++;
							} else if (t.name.StartsWith("Hairgl")) {
								hairCountGirlLo++;
							}
						}
					}
				}
				Debug.Log("ChibiHair initialized");
			}
		}

		public void create(GameObject root) {
			init();
			GameObject head = Utils.findChildren(root, "Head");
			setHeadRenderer(head.GetComponent<SkinnedMeshRenderer>());
			GameObject b_head = Utils.findChildren(root, "B_Head");
			setHeadBone(b_head);
			loadHair(0, 0);
			resetHairPosition();
		}

		public void setHeadRenderer(SkinnedMeshRenderer headRenderer) {
			this.headRenderer = headRenderer;
			getTopOfHeadIndex();
		}

		public void setHeadBone(GameObject headBone) {
			this.headBone = headBone;
		}

		void getTopOfHeadIndex() {
			Vector3[] vertices = headRenderer.sharedMesh.vertices;
			topOfHead = 0f;
			for (int i = 0; i < vertices.Length; i++) {
				if (vertices[i].y > topOfHead) {
					topOfHead = vertices[i].y;
					topOfHeadIndex = i;
				}
			}
		}

		Vector3 getHairPosition() {
			const float headSize = 0.36f;
			return new Vector3(0, topOfHead + 0.02f - headSize / 2, 0);
		}

		public void resetHairPosition() {
			if (!hair) {
				hair = findHairObject();
				if (hair == null) return;
			}
			Mesh mesh = new Mesh();
			headRenderer.BakeMesh(mesh);
			topOfHead = mesh.vertices[topOfHeadIndex].y;
			hair.transform.localPosition = getHairPosition();
		}

		GameObject findHairObject() {
			foreach (Transform t in headBone.transform) {
				if (t.name.StartsWith("Hairb") || t.name.StartsWith("Hairgu")) {
					return t.gameObject;
				}
			}
			return null;
		}

		public void loadHair(int index, int hairColorIndex) {
			if (hairStyleCount == 0) return;

			Utils.deleteChild(headBone, "Hair");

			index %= hairStyleCount;
			hairColorIndex %= MAX_HAIR_COLOR_INDEX;
			hairStyle = index;
			hairColor = getHairColor(hairColorIndex);
			if (isBoy) {
				hair = loadHair("Hairb" + (index + 1).ToString());
			} else {
				hair = loadHair("Hairgu" + ((index % hairCountGirlUp) + 1).ToString());
				loadHair("Hairgl" + (((index / hairCountGirlUp) % hairCountGirlLo) + 1).ToString(), hair);
			}
			if (headWear != -1) {
				setHeadWear(headWear);
			}
		}

		GameObject loadHair(string name, GameObject parent = null) {
			GameObject hairPrefab = GameObject.Find(name);
			if (hairPrefab) {
				Transform tParent = (parent == null) ? headBone.transform : parent.transform;
				Vector3 pos = tParent.TransformPoint( (parent == null) ? getHairPosition() : Vector3.zero);
				GameObject go = Instantiate(hairPrefab, pos, Quaternion.identity, tParent);
				go.transform.localRotation = Quaternion.identity;
				Renderer renderer = go.GetComponentInChildren<Renderer>();
#if UNITY_EDITOR
				if (EditorApplication.isPlaying) {
					renderer.material.color = hairColor;
				} else {
					renderer.sharedMaterial = Utils.getMaterial("Black");
				}
#else
				renderer.material.color = hairColor;
#endif
				return go;
			}
			Utils.logErrorNotFound(name);
			return null;
		}

		public void setHairColor(int index) {
			if (hair) {
				setHairColor(hair, index);
				if (hair.transform.childCount > 0) {
					setHairColor(hair.transform.GetChild(0).gameObject, index);
				}
			}
		}

		void setHairColor(GameObject go, int index) {
			Color color = getHairColor(index);
			Renderer renderer = go.GetComponentInChildren<Renderer>();
			renderer.material.color = color;
		}

		Color getHairColor(int index) {
			return Color.Lerp(Color.black, brightHairColor, (float)index / (MAX_HAIR_COLOR_INDEX - 1));
		}

		public void setHeadWear(int index) {
			headWear = index;
			if (index == -1) {
				Utils.deleteChild(headBone, "Cap");
				Utils.deleteChild(headBone, "Band");
			} else {
				string name = isBoy ? "Cap1" : "Band1";
				GameObject go = Utils.findChildren(hair, name, true);
				if (!go) {
					go = loadHair(name, hair);
				}
				if (isBoy) {
					int color = capColors[index, 0];
					setMaterialColor(go, (uint)color);
					int color1 = capColors[index, 1];
					if (color1 == -1) {
						color1 = color;
					}
					setMaterialColor(go.transform.GetChild(0).gameObject,  (uint)color1);
				} else {
					setMaterialColor(go, bandColors[index]);
				}

			}
		}

		void setMaterialColor(GameObject go, uint color, bool hasAlpha = false) {
			Renderer renderer = go.GetComponentInChildren<Renderer>();
			renderer.material.color = hasAlpha ? Utils.rgbaToColor(color) : Utils.rgbToColor(color);
			if (hasAlpha) {
				if ((color & 0xFF000000) == 0) {
					if (player.isMainPlayer()) {
						renderer.enabled = false;
					} else {
						go.SetActive(false);
					}
				} else {
					go.SetActive(true);
					renderer.enabled = true;
					Material material = renderer.material;
					material.SetOverrideTag("RenderType", "Transparent");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
				}
			}
		}

		public void setGlasses(int index) {
			player.log("setGlasses " + index);
			Utils.deleteChild(headBone, "Glasses");
			if (index >= 0) {
				int colorCount = glassesColors.GetLength(0);
				string name = "Glasses" + ((index & 1) + 1).ToString();
				index /= 2;
				GameObject go = Utils.findChildren(hair, name, true);
				if (!go) {
					go = loadHair(name, hair);
				}
				go.GetComponentInChildren<Renderer>();
				setMaterialColor(go, glassesColors[index, 0]);
				setMaterialColor(go.transform.GetChild(0).gameObject, glassesColors[index, 1], true);
			}
		}

		public static int getRandomGlassStyle() {
			int colorCount = glassesColors.GetLength(0);
			int index = Utils.rnd(4) ? colorCount - 1 : Random.Range(0, colorCount - 1);
			return index * Random.Range(1, 3);
		}
	}
}
