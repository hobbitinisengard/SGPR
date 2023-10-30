using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RVP
{
	// Static class with extra functions
	public static class F
	{
		readonly public static int trackMask = 0;
		public static AnimationCurve curve2 = AnimationCurve.EaseInOut(0, 1, 1, 0);
		public static AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		public static float EasingOutQuint(float x)
		{
			return 1 - Mathf.Pow(1 - x, 5);
		}
		public static Texture2D toTexture2D(this RenderTexture rTex)
		{
			Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
			var old_rt = RenderTexture.active;
			RenderTexture.active = rTex;

			tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
			tex.Apply();

			RenderTexture.active = old_rt;
			return tex;
		}

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
		public static float MaxAbs(params float[] nums)
		{
			float result = 0;

			for (int i = 0; i < nums.Length; i++)
			{
				if (Mathf.Abs(nums[i]) > Mathf.Abs(result))
				{
					result = nums[i];
				}
			}

			return result;
		}

		// Returns the topmost parent of a Transform with a certain component
		public static T GetTopmostParentComponent<T>(this Transform tr) where T : Component
		{
			T getting = null;

			while (tr.parent != null)
			{
				if (tr.parent.GetComponent<T>() != null)
				{
					getting = tr.parent.GetComponent<T>();
				}

				tr = tr.parent;
			}
			if (getting == null)
				Debug.LogError("no component");
			return getting;
		}
		public static RectTransform FindBigCanvasParent(this Transform tr)
		{
			while (tr.parent != null)
			{
				var rt = tr.parent.GetComponent<RectTransform>();
				if (rt != null && rt.rect.width > 0.9f * Screen.width && rt.rect.height > 0.9f * Screen.height)
				{
					return rt;
				}
				tr = tr.parent;
			}
			return null;
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
		public static void PlaySlideOutOnChildren(Transform node)
		{
			var comp = node.GetComponent<SlideInOut>();
			if (comp)
			{
				if (comp.gameObject.activeSelf)
					comp.PlaySlideOut();
			}
			else
			{
				for (int i = node.transform.childCount - 1; i >= 0; --i)
				{
					if(node.transform.GetChild(i).gameObject.activeSelf)
						PlaySlideOutOnChildren(node.transform.GetChild(i));
				}
			}
		}
		/// <param name="group">search children of this transform</param>
		/// <param name="excludeChild"></param>
		public static float PosAmongstActive(this Transform group, Transform child, bool countChildAsActive = true)
		{
			float posAmongstActive = -1;
			int activeChildren = 0;
			for (int i = 0; i < group.childCount; ++i)
			{
				if (group.GetChild(i) == child)
				{
					posAmongstActive = activeChildren;
					if (!countChildAsActive)
						continue;
				}

				if (group.GetChild(i).gameObject.activeSelf)
					activeChildren++;
			}
			if (activeChildren == 0)
				return 0;

			return posAmongstActive / (activeChildren);
		}
		public static int ActiveChildren(this Transform tr)
		{
			int count = 0;
			for (int i = 0; i < tr.childCount; ++i)
			{
				if (tr.GetChild(i).gameObject.activeSelf)
					++count;
			}
			return count;
		}
		public static Material ToOpaqueMode(Material inputMat)
		{
			Material material = inputMat;

			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			material.SetInt("_ZWrite", 1);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = -1; // Set it back to the default opaque render queue

			return material;
		}

		public static Material ToFadeMode(Material inputMat)
		{
			Material material = inputMat;

			var c = material.color;
			c.a = 0.5f;
			material.color = c;

			// Set the rendering mode to transparent
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3500;

			return material;
		}
		public static async Task<Texture2D> GetRemoteTexture(string url)
		{
			using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
			{
				// begin request:
				var asyncOp = www.SendWebRequest();

				// await until it's done: 
				while (asyncOp.isDone == false)
					await Task.Delay(1000 / 30);//30 hertz

				// read results:
				if( www.result!=UnityWebRequest.Result.Success )// for Unity >= 2020.1
				{
					// log error:
					
					Debug.Log($"{www.error}, URL:{www.url}");
					// nothing to return on error:
					return null;
				}
				else
				{
					// return valid results:
					return DownloadHandlerTexture.GetContent(www);
				}
			}
		}
		public static Vector3 Vec3Flatten(in Vector3 v)
		{
			return new Vector3(v.x, 0, v.z).normalized;
		}

#if UNITY_EDITOR
		// Returns whether the given object is part of a prefab (meant to be used with selected objects in the inspector)
		public static bool IsPrefab(UnityEngine.Object componentOrGameObject)
		{
			return UnityEditor.Selection.assetGUIDs.Length > 0
				 && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.NotAPrefab
				 && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.MissingAsset;
		}

		
#endif
	}
}
