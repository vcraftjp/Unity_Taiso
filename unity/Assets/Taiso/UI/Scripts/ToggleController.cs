using UnityEngine;
using UnityEngine.UI;

namespace VCraft
{
	public class ToggleController : MonoBehaviour
	{
		Toggle[] toggles;
		Color[] normalColors;
		int selectedIndex;
		string prefsKey;

		public delegate void OnValueChanged(int index);
		public OnValueChanged onValueChanged;

		void init() {
			toggles = GetComponentsInChildren<Toggle>();
			normalColors = new Color[toggles.Length];

			for (int i = 0; i < toggles.Length; i++ ) {
				Toggle toggle = toggles[i];
				normalColors[i] = toggle.colors.normalColor;
				toggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(toggle); });
			}
		}

		public static ToggleController findToggle(string name) {
			GameObject go = GameObject.Find(name);
			if (go) {
				ToggleController controller = go.GetComponentInChildren<ToggleController>();
				if (controller) {
					controller.init();
					return controller;
				}
			}
			return null;
		}

		public void setPrefsKey(string key) {
			this.prefsKey = key;
		}

		public void setPrefsKey(string key, int defaultValue) {
			this.prefsKey = key;
			setSelected(Prefs.getInt(key, defaultValue));
		}

		public void setSelected(int index, bool isNotify = false) {
			for (int i = 0; i < toggles.Length; i++) {
				setIsOn(i, i == index, isNotify);
			}
		}

		public int getSelected() {
			for (int i = 0; i < toggles.Length; i++) {
				if (toggles[i].isOn) return i;
			}
			return -1;
		}

		void setIsOn(int index, bool isOn, bool isNotify = false) {
			Toggle toggle = toggles[index];
			if (isNotify) {
				toggle.isOn = isOn;
			} else {
				toggle.SetIsOnWithoutNotify(isOn);
			}
			setToggleColor(index, isOn);
		}

		void setToggleColor(int index, bool isOn) {
			Toggle toggle = toggles[index];
			var colors = toggle.colors;
			colors.normalColor = isOn ? colors.selectedColor : normalColors[index];
			toggle.colors = colors;
		}

		void OnToggleValueChanged(Toggle toggle) {
			Debug.Log(toggle.name + "=" + toggle.isOn);
			if (toggle.isOn) {
				int selected = -1;
				for (int i = 0; i < toggles.Length; i++) {
					Toggle toggle1 = toggles[i];
					if (toggle1 == toggle) {
						selected = i;
						setToggleColor(i, true);
					} else {
						setIsOn(i, false);
					}
				}
				if (onValueChanged != null && selected != -1) {
					onValueChanged(selected);
				}
				if (prefsKey != null) {
					Prefs.setInt(prefsKey, selected);
				}
			}
		}
	}
}