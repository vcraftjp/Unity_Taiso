using UnityEngine;
using UnityEditor;

namespace VCraft
{
	public class AssetPostProcessorTest : AssetPostprocessor {

		void OnPreprocessModel() {
			if (assetPath.StartsWith("Assets/Chibi")) {
				ModelImporter importer = (ModelImporter) assetImporter;
				Debug.Log ("OnPreprocessModel: " + importer.assetPath);

				int n = assetPath.LastIndexOf("/");
				string name = assetPath.Substring(n + 1);
				if (name.StartsWith("chibi")) {
					importer.animationType = ModelImporterAnimationType.Human;
				}
				importer.isReadable = true;
				importer.importNormals = ModelImporterNormals.Calculate;
				importer.normalSmoothingAngle = 180f;

				EditorUtility.SetDirty(importer);
				importer.SaveAndReimport();
			}
		}

	/*
		void OnPostprocessTexture(Texture2D texture) {
			Debug.Log ("OnPostprocessTexture");
		}

		void OnPreprocessAnimation() {
			Debug.Log ("OnPreprocessAnimation");
		}
	*/
	}
}