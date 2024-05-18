using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Networking;
public class CircularBuffer<T>
{
	T[] buffer;
	int bufferSize;

	public CircularBuffer(int bufferSize)
	{
		this.bufferSize = bufferSize;
		buffer = new T[bufferSize];
	}

	public void Add(T item, int index) => buffer[index % bufferSize] = item;
	public T Get(int index) => buffer[index % bufferSize];
	public void Clear() => buffer = new T[bufferSize];
}
public static class F
{
	readonly public static int trackMask = 0;
	public static AnimationCurve curve2 = AnimationCurve.EaseInOut(0, 1, 1, 0);
	public static AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
	public static Info I;

	/// <summary>
	/// Sets any values of the Vector3
	/// </summary>
	public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
	{
		return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
	}
	public static void CopyFilesRecursively(string sourcePath, string targetPath)
	{
		//Now Create all of the directories
		foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
		{
			Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
		}

		//Copy all the files & Replaces any files with the same name
		foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
		{
			File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
		}
	}
	public static string RandomString(int length)
	{
		var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		var stringChars = new char[length];
		for (int i = 0; i < stringChars.Length; i++)
			stringChars[i] = chars[UnityEngine.Random.Range(0, chars.Length)];

		return new string(stringChars);
	}
	public static string GetQuickMessage(int number)
	{
		number = Mathf.Clamp(number, 0, 9);
		return F.I.playerData.quickMessages[number];
	}
	public static void SetQuickMessage(int number, string message)
	{
		number = Mathf.Clamp(number, 0, 9);
		F.I.playerData.quickMessages[number] =  message;
	}
	public static Livery SponsorGet(this Player player)
	{
		return (Livery)int.Parse(player.Data[ServerC.k_Sponsor].Value);
	}
	
	public static int ScoreGet(this Player player)
	{
		return int.Parse(player.Data[ServerC.k_score].Value);
	}
	
	public static bool ReadyGet(this Player player)
	{
		return player.Data[ServerC.k_Ready].Value == "1";
	}
	
	public static string NameGet(this Player player)
	{
		return player.Data[ServerC.k_Name].Value;
	}
	
	public static string carNameGet(this Player player)
	{
		return player.Data[ServerC.k_carName].Value;
	}
	
	public static Color ReadColor(this Player player)
	{
		return ReadColor(player.SponsorGet());
	}
	public static Color ReadColor(Livery livery)
	{
		if (F.I.teams)
		{
			switch (livery)
			{
				case Livery.Special:
					return Color.yellow;
				case Livery.TGR:
					return new Color(1, 165 / 255f, 0); // orange
				case Livery.Rline:
					return new Color(165 / 255f, 90 / 255f, 189 / 255f); // purple
				case Livery.Itex:
					return Color.red;
				case Livery.Caltex:
					return Color.green;
				case Livery.Titan:
					return Color.blue;
				case Livery.Mysuko:
					return Color.gray;
				default:
					break;
			}
		}
		return Color.yellow;
	}

	public static float EasingOutQuint(float x)
	{
		return 1 - Mathf.Pow(1 - x, 5);
	}
	public static int R(int min, int max)
	{
		return UnityEngine.Random.Range(min, max);
	}
	public static int Wraparound(int value, int minInclusive, int maxInclusive)
	{
		if (value < minInclusive)
			value = maxInclusive;
		else if (value > maxInclusive)
			value = minInclusive;
		return value;
	}
	public static float Wraparound(float value, float min, float max)
	{
		if (value < min)
			value = max;
		else if (value > max)
			value = min;
		return value;
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
	public static T GetRandom<T>(this IList<T> collection)
	{
		return collection[UnityEngine.Random.Range(0, collection.Count)];
	}
	public static byte ToByte(this bool val)
	{
		return (byte)(val ? 1 : 0);
	}
	public static string ToLaptimeStr(this TimeSpan t)
	{
		string s = "";
		if (t != TimeSpan.Zero && t.TotalHours < 10)
		{
			if (t.Hours > 0)
				s += string.Format("{0:D2}.", t.Hours);
			s += string.Format("{0:D2}:{1:D2}.{2:D2}", t.Minutes, t.Seconds, Mathf.RoundToInt(t.Milliseconds / 10f));
		}
		else
			return "-";
		return s;
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
		return curve.keys[^1].time;
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
	public static Color RandomColor()
	{
		return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
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
				if (node.transform.GetChild(i).gameObject.activeSelf)
					PlaySlideOutOnChildren(node.transform.GetChild(i));
			}
		}
	}
	public static void DestroyAllChildren(this Transform tr)
	{
		for(int i=0; i<tr.childCount; ++i)
		{
			GameObject.Destroy(tr.GetChild(i).gameObject);
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

	public static async Task<Texture2D> GetRemoteTexture(string url)
	{
		using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
		var asyncOp = www.SendWebRequest();

		while (asyncOp.isDone == false)
			await Task.Delay(100);

		if (www.result != UnityWebRequest.Result.Success)// for Unity >= 2020.1
		{
			Debug.Log($"{www.error}, URL:{www.url}");
			return null;
		}
		return DownloadHandlerTexture.GetContent(www);
	}
	public static Vector2 Flat(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}
	public static float Sign(float input)
	{
		if (input >= 0)
			if (input == 0)
				return 0;
			else
				return 1;
		else
			return -1;
	}
	public static Livery RandomLivery()
	{
		return (Livery)UnityEngine.Random.Range(1, F.I.Liveries + 1);
	}
	internal static void Deselect()
	{
		F.I.eventSystem.SetSelectedGameObject(null);
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
