using UnityEngine;
namespace RVP
{
    // Static class with extra functions
    public static class F
    {
        readonly public static int trackMask = 0;

        static public void drawString(string text, Vector3 worldPos, Color? colour = null)
        {
            UnityEditor.Handles.BeginGUI();

            var restoreColor = GUI.color;

            if (colour.HasValue) GUI.color = colour.Value;
            var view = UnityEditor.SceneView.currentDrawingSceneView;
            Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);

            if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                GUI.color = restoreColor;
                UnityEditor.Handles.EndGUI();
                return;
            }

            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
        }
        public static T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }
            return array;
        }
        public static float Duration(this AnimationCurve curve)
        {
            return curve.keys[curve.keys.Length - 1].time;
        }
        

        /// <summary>
        /// returns magnitude of the vector ignoring Y component
        /// </summary>
        public static float flatMagnitude(this Vector3 str)
        {
            return Mathf.Sqrt(str.x * str.x + str.z * str.z);
        }
        // Returns the number with the greatest absolute value
        public static float MaxAbs(params float[] nums) {
            float result = 0;

            for (int i = 0; i < nums.Length; i++) {
                if (Mathf.Abs(nums[i]) > Mathf.Abs(result)) {
                    result = nums[i];
                }
            }

            return result;
        }

        // Returns the topmost parent of a Transform with a certain component
        public static T GetTopmostParentComponent<T>(this Transform tr) where T : Component {
            T getting = null;

            while (tr.parent != null) {
                if (tr.parent.GetComponent<T>() != null) {
                    getting = tr.parent.GetComponent<T>();
                }

                tr = tr.parent;
            }

            return getting;
        }
		public static T FindParentComponent<T>(this Transform tr) where T : Component
		{
			while (tr.parent != null)
			{
				if (tr.parent.GetComponent<T>() != null)
				{
					return tr.parent.GetComponent<T>();
				}
				tr = tr.parent;
			}
         return null;
		}

#if UNITY_EDITOR
		// Returns whether the given object is part of a prefab (meant to be used with selected objects in the inspector)
		public static bool IsPrefab(Object componentOrGameObject) {
            return UnityEditor.Selection.assetGUIDs.Length > 0
                && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.NotAPrefab
                && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.MissingAsset;
        }
#endif
    }
}
