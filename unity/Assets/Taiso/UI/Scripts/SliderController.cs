using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VCraft
{
	public class SliderController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
	{
		protected Slider slider;
		protected InputField inputField;
		protected bool _isDragging;
		protected string sliderName;
		string prefsKey;
		float step = 0f;

		public delegate void OnValueChanged(float value);
		public OnValueChanged onValueChanged;

		protected void log(string msg){
			Debug.Log(sliderName + ": " + msg);
		}

		protected void init() {
			slider = GetComponentInChildren<Slider>();
			sliderName = slider.name;
			if (sliderName == "Slider") {
				sliderName = slider.transform.parent.name;
			}
			slider.onValueChanged.AddListener(onSliderValueChanged);
			inputField = GetComponentInChildren<InputField>();
			if (inputField == null) {
				inputField = transform.parent.gameObject.GetComponentInChildren<InputField>();
			}
			inputField.onEndEdit.AddListener(onTextValueChanged);
		}

		protected void onSliderValueChanged(float value) {
			value = fromStepValue(value);
			log("ValueChanged=" + value);
			setTextValue(value);
			valueChanged(value);
		}

		void valueChanged(float value) {
			if (onValueChanged != null) {
				onValueChanged(value);
			}
			if (prefsKey != null) {
				Prefs.setFloat(prefsKey, value);
			}
		}

		void onTextValueChanged(string text) {
			float value = float.Parse(text);
			float rawValue = toStepValue(value);
			if (rawValue < slider.minValue || rawValue > slider.maxValue) {
				setTextValue(getValue());
			} else {
				slider.SetValueWithoutNotify(rawValue);
				valueChanged(value);
			}
		}

	    public virtual void OnBeginDrag(PointerEventData eventData) {
			log("OnBeginDrag");
			_isDragging = true;
		}

		public virtual void OnEndDrag(PointerEventData eventData) {
			log("OnEndDrag");
			_isDragging = false;
		}

		public bool isDragging() {
			return _isDragging;
		}

		public virtual void setTextValue(float value) {
			inputField.text = value.ToString();
		}

		public static SliderController findSlider(string name) {
			GameObject go = GameObject.Find(name);
			if (go) {
				SliderController controller = go.GetComponentInChildren<SliderController>();
				if (controller) {
					controller.init();
					return controller;
				}
			}
			Utils.logErrorNotFound(name);
			return null;
		}

		public void setPrefsKey(string key) {
			this.prefsKey = key;
		}

		public void setPrefsKey(string key, float defaultValue) {
			this.prefsKey = key;
			setValue(Prefs.getFloat(key, defaultValue));
		}

		float toStepValue(float value) {
			return (step == 0f) ? value : value / step;
		}

		float fromStepValue(float value) {
			return (step == 0f) ? value : value * step;
		}

		public void setMinMaxValue(float minValue, float maxValue, float step = 0f) {
			this.step = step;
			slider.minValue = toStepValue(minValue);
			slider.maxValue = toStepValue(maxValue);
		}

		public void setValue(float value, bool isNotify = false) {
			if (isNotify) {
				slider.value = toStepValue(value);
			} else {
				slider.SetValueWithoutNotify(toStepValue(value));
				setTextValue(value);
			}
			if (prefsKey != null) {
				Prefs.setFloat(prefsKey, value);
			}
		}

		public float getValue() {
			return fromStepValue(slider.value);
		}

		public int getIntValue() {
			return (int)fromStepValue(slider.value);
		}

		public bool isFloat() {
			return !slider.wholeNumbers;
		}

	}
}