using UnityEngine;
using UnityEditor;

namespace VCraft
{
	public class ToolMenu : EditorWindow
	{
		[MenuItem("Tools/Modify 'Chibi'")]
		static void ModifyAvatar() {
			GameObject go = Selection.activeGameObject;
			string path = AssetDatabase.GetAssetPath(go);
			var prefab = PrefabUtility.LoadPrefabContents(path);
			if (prefab) {
				prefab.tag = "Player";
				prefab.layer = 8; // Player Layer

				if (prefab.GetComponentInChildren<TaisoPlayer>() == null) {
					prefab.AddComponent<TaisoPlayer>();
				}
				if (prefab.GetComponentInChildren<CapsuleCollider>() == null) {
					prefab.AddComponent<CapsuleCollider>();
				}
				createChibi(prefab);

				setTaisoAimator(prefab);

				PrefabUtility.SaveAsPrefabAsset(prefab, path);
				PrefabUtility.UnloadPrefabContents(prefab);
			}
		}

		static void createChibi(GameObject root) {
			GameObject head = VCraft.Utils.findChildren(root, "Head");
			Material materal = AssetDatabase.LoadAssetAtPath("Assets/Chibi/Materials/Head.mat", typeof(Material)) as Material;
			SkinnedMeshRenderer renderer = head.GetComponentInChildren<SkinnedMeshRenderer>();
			renderer.sharedMaterial = materal;

			Utils.setMaterial(root, new string[]{ "Body", "Hand_R", "Hand_L" }, "Skin");
			Utils.setMaterial(root, new string[]{ "Foot_R", "Foot_L" }, "LightGray");

			ChibiPlayer.createChibi(root, false); // true:boy, false:girl
		}

		static void setTaisoAimator(GameObject root) {
			RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath("Assets/Taiso/Animations/Taiso Animator.controller", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
			Animator animator = root.GetComponent<Animator>();
			animator.runtimeAnimatorController = controller;
			Debug.Log("Set animator controller: " + animator);
		}
	}

}