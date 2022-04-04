using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VCraft
{
	public class Utils
	{
		public static bool rnd(int n) {
			return Random.Range(0, n) == 0;
		}

		public static void logErrorNotFound(string name) {
			Debug.LogError("'" + name + "' not found");
		}

		public static void copyComponent<T>(GameObject goSrc, GameObject goDest, bool enabled = true) where T : Component {
			T src = goSrc.GetComponent<T>();
			T dest = goDest.AddComponent<T>();
			copyComponentValues<T>(src, dest);
		}

		public static void copyComponentValues<T>(T src, T dest) where T : Component {
	//		Debug.Log(src.GetType().Name + ":");

			System.Reflection.FieldInfo[] fields = src.GetType().GetFields();
			foreach (System.Reflection.FieldInfo field in fields) {
				if (field.IsLiteral || field.IsInitOnly) continue;
	//			Debug.Log("  " + field.Name);
				field.SetValue(dest, field.GetValue(src));
			}

			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;
			PropertyInfo[] pinfos = src.GetType().GetProperties(flags);
			foreach (var pinfo in pinfos) {
				if (pinfo.Name == "name" || pinfo.Name == "tag" || pinfo.Name == "enabled") continue;
				if (pinfo.CanWrite) {
					try {
	//					Debug.Log("  " + pinfo.Name);
						pinfo.SetValue(dest, pinfo.GetValue(src));
					} catch {

					}
				}
			}
		}

		public static GameObject findChildren(GameObject root, string prefix, bool isNoError = false) {
			Transform[] transforms = root.GetComponentsInChildren<Transform>();
			foreach (Transform t in transforms) {
				if (t.name.StartsWith(prefix)) return t.gameObject;
			}
			if (!isNoError) {
				logErrorNotFound(prefix);
			}
			return null;
		}

		public static List<T> findChildren<T>(GameObject root, string prefix = null) where T : Object {
			List<T> list = new List<T>();
//			Transform[] transforms = root.GetComponentsInChildren<Transform>();
			foreach (Transform t in root.transform) {
				if (!t.gameObject.activeSelf) continue;
				T child = t.gameObject.GetComponent<T>();
				if (child != null) {
					if (prefix != null) {
						if (!child.name.StartsWith(prefix)) continue;
					}
					list.Add(child as T);
				}
			}
			return list;
		}

		public static T findComponent<T>(string name) {
			GameObject go = GameObject.Find(name);
			if (go) {
				return go.GetComponentInChildren<T>();
			}
			logErrorNotFound(name);
			return default(T);
		}

		public static T findComponent<T>(string rootName, string name) {
			GameObject root = GameObject.Find(rootName);
			if (root) {
				GameObject go = findChildren(root, name);
				if (go) {
					return go.GetComponentInChildren<T>();
				}
			}
			logErrorNotFound(name);
			return default(T);
		}

		public static void deleteChild(GameObject root, string prefix) {
			Transform[] transforms = root.GetComponentsInChildren<Transform>();
			foreach (Transform t in transforms) {
				if (t == null) continue;
				if (t.name.StartsWith(prefix)) {
#if UNITY_EDITOR
					if (EditorApplication.isPlaying) {
						GameObject.Destroy(t.gameObject);
					} else {
						GameObject.DestroyImmediate(t.gameObject);
					}
#else
					GameObject.Destroy(t.gameObject);
#endif
//					Debug.Log(t.gameObject.name + " destroyed");
				}
			}
		}

		public static Bounds getBounds(GameObject root) {
			Renderer[] meshes = root.GetComponentsInChildren<Renderer>();
			Bounds maxBounds = new Bounds(Vector3.zero, Vector3.zero);
			foreach (Renderer mesh in meshes) {
//				Debug.Log(mesh.name + ": " + mesh.bounds.center + ", " + mesh.bounds.size);
				if (maxBounds.size == Vector3.zero) {
					maxBounds = mesh.bounds;
				} else {
					maxBounds.Encapsulate(mesh.bounds);
				}

			}
			return maxBounds;
		}

		public static Dictionary<string, T> loadAssets<T>(string path, System.Type type) where T : Object {
			var assetsMap = new Dictionary<string, T>();
			Object[] objects = Resources.LoadAll(path, type);
			Debug.Log("load " + objects.Length + " " + type.Name + "(s) from '" + path + "'");
			foreach (Object o in objects) {
				assetsMap[o.name] = o as T;
			}
			return assetsMap;
		}

		public static string materialPath = "Materials/Colors";

		static Dictionary<string, Material> materialMap;

		static Dictionary<string, Material> loadMaterials(string path) {
			return loadAssets<Material>(path, typeof(Material));
		}

		public static Material getMaterial(string name) {
			if (materialMap == null) {
				materialMap = loadMaterials(materialPath);
			}
			try {
				return materialMap[name];
			} catch (KeyNotFoundException) {
				logErrorNotFound(name);
				return null;
			}
		}

		public static bool setMaterial(GameObject root, string name, string materialName) {
			GameObject go = findChildren(root, name);
			if (go) {
				Material material = getMaterial(materialName);
				if (material) {
					go.GetComponentInChildren<Renderer>().sharedMaterial = material;
					return true;
				}
			}
			return false;
		}

		public static bool setMaterial(GameObject root, string[] names, string materialName) {
			foreach(string name in names) {
				if (!setMaterial(root, name, materialName)) return false;
			}
			return true;
		}

		public static void setMaterialColor(GameObject root, string name, Color color) {
			GameObject go = findChildren(root, name);
			if (go) {
				go.GetComponentInChildren<Renderer>().material.color = color;
			}
		}

		public static void setMaterialColor(GameObject root, string[] names, Color color) {
			foreach(string name in names) {
				setMaterialColor(root, name, color);
			}
		}

		public static Color32 rgbToColor(uint rgb) {
			return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
		}

		public static Color32 rgbaToColor(uint rgba) {
			return new Color32((byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba, (byte)(rgba >> 24));
		}

		public static float[] calcDistribution(int count, bool isSigned, bool isWeighted = true) {
			float[] values = new float[count];
			for (int i = 0; i < count; i++) {
				float n = Random.Range(isSigned ? -1f : 0f, 1f);
				if (isWeighted) {
					n = n * n * n;
				}
				values[i] = n;
			}
			return values;
		}
	}
}