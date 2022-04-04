using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VCraft
{
	public class TimelineController : SliderController
	{
		float[] timeline;
		int prevSec = -1;
		int prevIndex = -1;

		public override void setTextValue(float value) {
			int sec = (int)value;
			int index = timeline.Length - 1;
			for (int i = 1; i < timeline.Length; i++) {
				if (value <= timeline[i]) {
					index = i - 1;
					break;
				}
			}
			if (sec != prevSec || index != prevIndex) {
				prevSec = sec;
				prevIndex = index;
				string s = string.Format("{0}:{1:00} #{2}", sec / 60, sec % 60, index);
				inputField.text = s;
			}
		}

		public static new TimelineController findSlider(string name) {
			GameObject go = GameObject.Find(name);
			if (go) {
				TimelineController controller = go.GetComponentInChildren<TimelineController>();
				if (controller) {
					controller.init();
					return controller;
				}
			}
			return null;
		}

		public override void OnEndDrag(PointerEventData eventData) {
			base.OnEndDrag(eventData);
			onSliderValueChanged(slider.value);
		}

		public void setTimeline(float[] timeline) {
			this.timeline = timeline;
		}

	}
}