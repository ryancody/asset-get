using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetGet : EditorWindow
{
	private Editor editor;
	private const int autoWatchDelay = 1000;
	private long lastAutoWatch;
	[SerializeField]
	private bool autoWatch = true;

	[Serializable]
	public class Asset
	{
		[SerializeField]
		public string Name;
		[SerializeField]
		public string SourcePath;
		[SerializeField]
		public string FileType;
		[SerializeField]
		public string Folder;
	}

	[SerializeField]
	public List<Asset> Assets = new List<Asset>();

	// Add menu named "My Window" to the Window menu
	[MenuItem("Tools/AssetGet")]
	static void Init ()
	{
		// Get existing open window or if none, make a new one:
		AssetGet window = (AssetGet)GetWindow(typeof(AssetGet));
		window.Show();
	}

	void OnEnable ()
	{
		Load("AssetGet");
	}

	void OnDisable ()
	{
		Save("AssetGet");
	}

	private string LocalFilePath(string folder, string filename) =>
		Path.Combine(Application.dataPath, folder, filename).Replace("\\", "/");

	private void Save(string key)
	{
		var data = JsonUtility.ToJson(this, false);
		EditorPrefs.SetString(key, data);

		Debug.Log($"{key} saved data");
	}

	private void Load(string key)
	{
		var data = EditorPrefs.GetString(key, JsonUtility.ToJson(this, false));
		
		JsonUtility.FromJsonOverwrite(data, this);
		Debug.Log($"{key} loaded data");
	}

	void OnGUI ()
	{
		if (Application.isPlaying)
		{
			return;
		}

		EditorGUILayout.Space();
		autoWatch = EditorGUILayout.Toggle("Auto Watch", autoWatch);

		if (!editor)
		{
			editor = Editor.CreateEditor(this);
		}

		editor.OnInspectorGUI();


		if (autoWatch && lastAutoWatch + autoWatchDelay < DateTimeOffset.Now.ToUnixTimeSeconds())
		{
			//AutoWatch();
			lastAutoWatch = DateTimeOffset.Now.ToUnixTimeSeconds();
		}

		if (GUILayout.Button("Get Files"))
		{
			GetAssets();
		}
	}

	void OnInspectorUpdate () { Repaint(); }

	void GetAssets ()
	{
		try
		{
			foreach (Asset a in Assets)
			{
				if (!Directory.Exists(a.SourcePath))
				{
					Debug.Log("AssetGet - Source path does not exist: " + a.SourcePath);
					return;
				}
				var files = Directory.GetFiles(a.SourcePath, $"*.{a.FileType}");

				foreach (var f in files)
				{
					var filename = Path.GetFileName(f);
					var localfile = LocalFilePath(a.Folder, filename);

					if (!File.Exists(localfile) || LocalAssetIsOutDated(f, localfile))
					{
						GetAsset(a, filename);
					}
				}
			}
		}
		catch (Exception e)
		{
			Debug.Log(e.ToString());
		}
		AssetDatabase.Refresh();
	}

	void GetAsset (Asset a, string filename)
	{
		Debug.Log($"AssetGet fetching {filename}");

		var source = Path.Combine(a.SourcePath, filename);
		var dest = LocalFilePath(a.Folder, filename);
		File.Copy(source, dest, true);
	}

	bool LocalAssetIsOutDated(string source, string local)
	{
		var sourceDate = File.GetLastWriteTime(source);
		var localDate = File.GetLastWriteTime(local);

		if (DateTime.Compare(sourceDate, localDate) > 0)
		{
			return true;
		}

		return false;
	}
}

[CustomEditor(typeof(AssetGet), true)]
public class FileDrawer : Editor
{
	public override void OnInspectorGUI ()
	{
		var list = serializedObject.FindProperty("Assets");
		if (list != null)
		{
			EditorGUILayout.PropertyField(list, new GUIContent("Assets"), true);
		}
		serializedObject.ApplyModifiedProperties();
	}
}
