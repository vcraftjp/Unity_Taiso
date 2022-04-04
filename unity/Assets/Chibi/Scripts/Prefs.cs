using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VCraft
{
	public class Prefs : Object
	{
		public static void load() {
		}

		public static void save() {
		}

		public static void setInt(string key, int value) {
			PlayerPrefs.SetInt(key, value);
		}

		public static int getInt(string key, int defaultValue = 0) {
			return PlayerPrefs.GetInt(key, defaultValue);
		}

		public static void setFloat(string key, float value) {
			PlayerPrefs.SetFloat(key, value);
		}

		public static float getFloat(string key, float defaultValue = 0f) {
			return PlayerPrefs.GetFloat(key, defaultValue);
		}

		public static void setString(string key, string value) {
			PlayerPrefs.SetString(key, value);
		}

		public static string getString(string key, string defaultValue = "") {
			return PlayerPrefs.GetString(key, defaultValue);
		}

		public static void setBool(string key, bool value) {
			PlayerPrefs.SetInt(key, value ? 1 : 0);
		}

		public static bool getBool(string key, bool defaultValue = false) {
			return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) != 0 : defaultValue;
		}

		public static bool hasKey(string key) {
			return PlayerPrefs.HasKey(key);
		}

		public static void deleteKey(string key) {
			PlayerPrefs.DeleteKey(key);
		}

		public static void deleteAll() {
			PlayerPrefs.DeleteAll();
		}

	}
}