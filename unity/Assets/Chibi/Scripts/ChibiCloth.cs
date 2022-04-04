using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VCraft
{
	public class ChibiCloth : Object
	{
		public const string PREFIX = "C_";
		public const int MAX_CLOTH_COLOR_INDEX = 8;

		static readonly string[] clothsBoyBase = { "UPants1", "Pants1" };
		static readonly string[][] clothsBoy = {
			new string[] { "UShirt1"},
			new string[] { "UShirt2"},
			new string[] { "UShirt3"},
			new string[] { "UShirt2", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp" },
			new string[] { "UShirt2", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp", "ButtonBaseLo", "ButtonsLo" },
			new string[] { "UShirt2", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp", "ButtonBaseLo", "ButtonsLo", "Suspender1" },
		};
		static readonly string[] clothsGirlBase = { "UShirt1", "UPants1" };
		static readonly string[][] clothsGirl = {
			new string[] { "Skirt1", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp" },
			new string[] { "Skirt1", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp", "ButtonBaseLo", "ButtonsLo" },
			new string[] { "Skirt1", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp", "ButtonBaseLo", "ButtonsLo", "Suspender1" },
			new string[] { "Onepiece1", "Shirt1", "Collar1", "ButtonBaseUp", "ButtonsUp" },
		};

		static readonly string[] clothColorsBoy = { "Blue", "Green", "DarkBlue", "DarkGreen", "Cyan", "Teal", "Yellow", "Lime" };
		static readonly string[] clothColorsGirl = { "Red", "Magenta", "Lemon", "Salmon", "Crimson", "Pink", "Yellow", "Orange" };
		static readonly string[] footColorsBoy = { "Blue", "Green", "DarkBlue", "DarkGreen" };
		static readonly string[] footColorsGirl = { "Red", "Magenta", "Pink", "Crimson", "Salmon" };
		static readonly string[] buttonsColors = { "Black", "DarkBlue", "DarkGreen", "DarkRed" };

		ChibiPlayer player;

		bool isBoy {
			get {
				return player.isBoy;
			}
		}

		public static int getClothStyleCount(bool isBoy) {
			return isBoy ? clothsBoy.Length : clothsGirl.Length;
		}

		public int clothStyleCount {
			get {
				return getClothStyleCount(isBoy);
			}
		}

		public ChibiCloth(ChibiPlayer player) {
			this.player = player;
		}

		public void create(GameObject root) {
			loadCloth(root, 0, 0, false);
		}

		public void loadCloth(GameObject root, int index, int colorIndex, bool isRandom) {
			player.log("loadCloth index=" + index + ", color=" + colorIndex + ", isRandom=" + isRandom);
			index %= clothStyleCount;
			colorIndex %= MAX_CLOTH_COLOR_INDEX;

			Random.State state = Random.state;
			Random.InitState((player.playerIndex << 6) | ((index + 1) << 3) | colorIndex);

			string[] clothsBase = isBoy ? clothsBoyBase : clothsGirlBase;
			string[] cloths = isBoy ? clothsBoy[index] : clothsGirl[index];
			string[] clothColors = isBoy ? clothColorsBoy : clothColorsGirl;
			string[] footColors =  isBoy ? footColorsBoy : footColorsGirl;
			string buttonsColor = null;
			string buttonBaseColor = null;
			string shirtColor = null;
			string collarColor = null;

			foreach (Transform t in root.transform) {
				if (!t.name.StartsWith(PREFIX)) continue;
				string name = t.name.Substring(PREFIX.Length);
				if (!clothsBase.Contains(name) && !cloths.Contains(name)) {
					t.gameObject.SetActive(false);
				} else {
					t.gameObject.SetActive(true);
					string color = "White";
					bool isNextColor = false;
					if (name.StartsWith("Buttons")) {
						if (buttonsColor != null) {
							color = buttonsColor;
						} else {
							buttonsColor = color = randomColor(buttonsColors, colorIndex, isRandom, "Black");
						}
					} else if (name.StartsWith("Collar") || name.StartsWith("ButtonBase")) {
						color = randomColor(clothColors, colorIndex, isRandom, "White");
						if (name.StartsWith("ButtonBase")) {
							if (buttonBaseColor != null) {
								color = buttonBaseColor;
							} else {
								buttonBaseColor = color;
							}
						} else { // Collar
							collarColor = color;
						}
					} else if (!name.StartsWith("U") || (name.StartsWith("US") && isBoy && index < 2)) { // UShirt1,2
						isNextColor = true;
						color = randomColor(clothColors, colorIndex, isRandom);
						if (name.StartsWith("Shirt")) {
							if (color == collarColor) {
								color = randomColor(clothColors, colorIndex + 1, isRandom);
							}
							shirtColor = color;
						} else if (color == shirtColor) {
							color = randomColor(clothColors, colorIndex + 1, isRandom);
						}
					}
					Renderer renderer = t.gameObject.GetComponentInChildren<Renderer>();
					Material material = Utils.getMaterial(color);
					if (material) {
						renderer.sharedMaterial = material;
					}
					//player.log(t.name + ": " + color + " (" + colorIndex + ")");
					if (isNextColor) {
						if (isRandom) {
							colorIndex += Random.Range(1, MAX_CLOTH_COLOR_INDEX - 1);
						} else {
							// TODO: Non-random color variations depend on the NAME of cloths
							colorIndex++;
						}
						colorIndex %= MAX_CLOTH_COLOR_INDEX;
					}
				}
			}
			string footColor = randomColor(footColors, colorIndex, isRandom, "LightGray");
			Utils.setMaterial(root, new string[] { "Foot_R", "Foot_L"}, footColor);

			Random.state = state;
		}

		string randomColor(string[] colors, int colorIndex, bool isRandom, string defaultColor = null, int defaultRate = 4) {
			if (defaultColor != null) {
				if (!isRandom || Random.Range(0, defaultRate) != 0) {
					return defaultColor;
				}
			}
			return colors[colorIndex % colors.Length];
		}
	}
}
