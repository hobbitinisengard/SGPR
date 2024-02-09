#if UNITY_EDITOR
    
using System;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace HinputClasses.Internal {
	// Hinput class responsible for setting up and uninstalling the plugin.
	public static class Setup {
		
		
		// --------------------
		// SETUP
		// --------------------
		
		// Add Hinput's input array at the end of inputManagerDir
		[MenuItem("Tools/Hinput/Set Up Hinput")]
		public static void HinputSetup () {
			try {
				using (StreamWriter sw = File.AppendText(Utils.inputManagerPath)) sw.Write(Utils.HinputInputArray());
				AssetDatabase.Refresh();
				try {
					File.Delete("./Library/SourceAssetDB");
					Debug.Log("Hinput has been set up successfully. You can start coding!");
				} catch (IOException) { 
					Debug.LogWarning("[ACTION NEEDED] The asset database needs to be reimported in order " +
					                 "to confirm the setup of Hinput. Please click \"Reimport all\" in the Assets " +
					                 "menu."); 
				}
			} catch (Exception e) {
				Utils.SetupError();
				throw e;
			}
		}

		// Allows to set up Hinput only if it is not installed.
		[MenuItem("Tools/Hinput/Set Up Hinput", true)]
		public static bool HinputSetupValidation () {
			return !Utils.HinputIsInstalled();
		}
		
		
		// --------------------
		// UNINSTALL
		// --------------------
		
		// Remove Hinput's input array from the end of inputManagerDir
		[MenuItem("Tools/Hinput/Uninstall Hinput")]
		public static void HinputUninstall () {
			try {
				File.WriteAllText(Utils.inputManagerPath, 
					File.ReadAllText(Utils.inputManagerPath).Replace(Utils.HinputInputArray(), ""));
				AssetDatabase.Refresh();
				try {
					File.Delete("./Library/SourceAssetDB");
					Debug.Log("Hinput has been uninstalled successfully. Bye bye!");
				} catch (IOException) { 
					Debug.LogWarning("[ACTION NEEDED] The asset database needs to be reimported in order " +
					                 "to confirm the setup of Hinput. Please click \"Reimport all\" in the Assets " +
					                 "menu."); 
				}
			} catch (Exception e) {
				Utils.UninstallError();
				throw e;
			}
		}

		// Allows to uninstall Hinput only if it is installed.
		[MenuItem("Tools/Hinput/Uninstall Hinput", true)]
		public static bool HinputUninstallValidation () {
			return Utils.HinputIsInstalled();
		}
	}
}
#endif