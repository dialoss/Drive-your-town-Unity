// ImportFBXGUIScript.h
// Description: Unity Menu GUI plugin to import Trian3DBuilder FBX terrains
//				terrain path should be have separate image and tile folder and can have external folder
//				use FBX Path Template!
// Author: Mirco Nierenz
// Copyright TrianGraphics GmbH, Germany

using UnityEngine;
using UnityEditor;

using System.IO;

public class MenuItems
{
	const string c_ExternalPath = "\\external";
	const string c_TerrainPath = "/terrain";
	
	private static string copyAsset(string filename, string terrainpath)
	{	
		string localfilename = c_TerrainPath + filename.Substring(terrainpath.Length);
		string destFilename = Application.dataPath + localfilename;
		
		Directory.CreateDirectory(Path.GetDirectoryName(destFilename));
		File.Copy(filename, destFilename, true);
		
		return localfilename;
	}
	
	private static bool hasModelExtension(string filename)
	{
		return Path.GetExtension(filename) == ".fbx" || 
			   Path.GetExtension(filename) == ".prefab";
	}
	
    [MenuItem("Trian3DBuilder/Import FBX")]
    private static void NewMenuOption()
    {
		// select terrain folder
		string fbxTerrainPath = EditorUtility.OpenFolderPanel("Load FBX Terrain", "", "");		
		if (!fbxTerrainPath.Contains(c_TerrainPath))
		{
			Debug.LogError("no terrain folder selected! " + fbxTerrainPath);
			return;
		}
		
		string terrainModelPaths = null;
		string externalModelPaths = null;
		string[] paths = Directory.GetDirectories(fbxTerrainPath, "*", SearchOption.AllDirectories);
		foreach (string path in paths)
		{
			bool isModelDir = false;
			string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
			foreach (string filename in files)
			{
				if (hasModelExtension(filename))
				{
					isModelDir = true;
					break;
				}
			}
			
			if (isModelDir)
			{
				if (path.Contains(c_ExternalPath))
					externalModelPaths = path;
				else 
					terrainModelPaths = path;
			}
			else
			{	
				// copy and import first all image dirs
				foreach (string filename in files)
					copyAsset(filename, fbxTerrainPath);					
				Debug.Log("import images " + path);				
				string localpath = c_TerrainPath + path.Substring(fbxTerrainPath.Length);
				AssetDatabase.ImportAsset("Assets" + localpath);
			}				
		}
		AssetDatabase.Refresh(); // force AssetPostprocessor
		
		// import all models from external folder		
		// should be imported before loading tiles to have external assets already in unity asset database
		if (!string.IsNullOrEmpty(externalModelPaths))
		{
			Debug.Log("import external " + externalModelPaths);
			
			string[] files = Directory.GetFiles(externalModelPaths, "*", SearchOption.TopDirectoryOnly);
			foreach (string filename in files)
				copyAsset(filename, fbxTerrainPath);					
			
			string localpath = c_TerrainPath + externalModelPaths.Substring(fbxTerrainPath.Length);
			AssetDatabase.ImportAsset("Assets" + localpath);
			AssetDatabase.Refresh();
		}
		
		// import all models from tiles folder		
		if (!string.IsNullOrEmpty(terrainModelPaths))
		{
			Debug.Log("import tiles " + terrainModelPaths);
			
			string[] files = Directory.GetFiles(terrainModelPaths, "*", SearchOption.TopDirectoryOnly);
			foreach (string filename in files)
				copyAsset(filename, fbxTerrainPath);					
			
			string localpath = c_TerrainPath + terrainModelPaths.Substring(fbxTerrainPath.Length);
			AssetDatabase.ImportAsset("Assets" + localpath);
			AssetDatabase.Refresh();
		}
    }
}
