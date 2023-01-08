// ImportTrianTerrainEditorScript.h
// Description: AssetPostprocessor to handle user data in fbx files from Trian3DBuilder terrains
//              can handle external files, PBR materials, LOD settings, ...
// Author: Mirco Nierenz
// Copyright TrianGraphics GmbH, Germany

using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

// class with static containers und helper functions
public static class ImporterBase
{
	// structs / enums
	public struct MaterialData
	{
		public string name;
		public string type;
		public string value;
	};

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,
		Transparent
	}	
	
	// const members
	public const string EXTERNAL_SUBPATH = "external/";
	public const string PREFAB_DESTINATION_DIRECTORY = "Assets/Prefabs/";
    public const string MATERIAL_PATH = "Assets/Materials/generated";
    public const bool DEBUG_MODE = false;
	public const bool CREATE_PREFABS = false;
	public const bool NO_LOD_CULLING = true;
	
	// static members
	public static Dictionary<string, List<MaterialData>> s_MaterialAttributes = new Dictionary<string, List<MaterialData>>(); // <material name, material attributes>
	public static Dictionary<string, Dictionary<int, float>> s_LODAttributes = new Dictionary<string, Dictionary<int, float>>(); // <LOD node name, <index, distance>>
	
	// static functions
	public static bool isDebugMode()
	{
		return DEBUG_MODE;
	}
	
	public static bool isSupportPrefab()
	{
		return CREATE_PREFABS;
	}

	public static bool isSupportLODCulling()
	{
		return !NO_LOD_CULLING;
	}	
	
	public static void EnsureDirectoryExists( string directory )
    {
        if( !Directory.Exists( directory ) )
            Directory.CreateDirectory( directory );
    }	

	public static bool getMinMaxDistance(string valueStr, out string baseNodename, out int index, out float distance)
    {
        int startValue = 0;
        int posValue = valueStr.IndexOf(";", startValue);
		
		// baseNodename
		baseNodename = valueStr.Substring(startValue, posValue - startValue);

		// index
		startValue = posValue + 1;
        posValue = valueStr.IndexOf(";", startValue);
        string subString = valueStr.Substring(startValue, posValue - startValue);
		index = Convert.ToInt32(subString);
		
		// distance
        startValue = posValue + 1;
        subString = valueStr.Substring(startValue, valueStr.Length - startValue);
		distance = float.Parse(subString, System.Globalization.CultureInfo.InvariantCulture);

        return true;
    }
	
    public static void setRenderMode(Material material, BlendMode blendmode)
    {
        if (blendmode == BlendMode.Cutout)
        {			
            material.SetFloat("_Mode", 1);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 2450;

            material.SetFloat("_Glossiness", 0.0f);
        }
        else if (blendmode == BlendMode.Opaque)
        {
            material.SetFloat("_Mode", 0);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
        else if (blendmode == BlendMode.Fade)
        {
            material.SetFloat("_Mode", 2);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else if (blendmode == BlendMode.Transparent)
        {
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }	
	
    // get material data from material string
    public static bool getMaterialData(string materialStr, out ImporterBase.MaterialData materialData, out string materialName)
    {
        materialData = new MaterialData();
        int startValue = 0;
        int posValue = materialStr.IndexOf(":", startValue);

        startValue = posValue + 1;
        posValue = materialStr.IndexOf(":", startValue);
        materialData.type = materialStr.Substring(startValue, posValue - startValue);


        startValue = posValue + 1;
        posValue = materialStr.IndexOf(":", startValue);
        materialName = materialStr.Substring(startValue, posValue - startValue);

        startValue = posValue + 1;
        materialData.name = materialStr.Substring(startValue, materialStr.Length - startValue);

        return true;
    }	
	
	public static void addMaterial( string propName, object propertyValue)
	{
		if (isDebugMode())
			Debug.Log("Propname: " + propName + " value: " + propertyValue);

		// parse attribute to get material name, type, attribute name
		MaterialData materialData;
		string materialName;
		getMaterialData(propName, out materialData, out materialName);
		materialData.value = (string)propertyValue;

		// register to s_MaterialAttributes
		List<MaterialData> materialAttributes;
		if (s_MaterialAttributes.TryGetValue(materialName, out materialAttributes))
			materialAttributes.Add(materialData);
		else
		{
			materialAttributes = new List<MaterialData>();
			materialAttributes.Add(materialData);
			s_MaterialAttributes.Add(materialName, materialAttributes);
		}
	}	
	
	public static void addLODInfo(object propertyValue, string gameObjectName)
	{
		// Unity3d create LODGroup automatically from nodenames with postfix "_LOD[x]"
		string baseNodename;
		int index = 0;
		float distance;
		getMinMaxDistance(propertyValue.ToString(), out baseNodename, out index, out distance);
		baseNodename = "Test"; // debug
		
		if (isDebugMode())
			Debug.Log("LOD: " + gameObjectName + " value: " + baseNodename + "-> " + distance);
		
		// register to s_LODAttributes
		Dictionary<int, float> lodAttributes;
		if (s_LODAttributes.TryGetValue(baseNodename, out lodAttributes))
		{
			float distanceLocal;
			if (!lodAttributes.TryGetValue(index, out distanceLocal))
				lodAttributes.Add(index, distance);
		}
		else
		{
			lodAttributes = new Dictionary<int, float>();
			lodAttributes.Add(index, distance);
			s_LODAttributes.Add(baseNodename, lodAttributes);
		}				
	}
	
	public static void addCollider(GameObject gameObject)
	{
		// add mesh collider to use raycast to clutter grass
		gameObject.AddComponent<MeshCollider>();

		MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

		foreach (MeshFilter currMeshFilter in meshFilters)
		{
			if (currMeshFilter.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh != null
				&& currMeshFilter.gameObject.GetComponentInChildren<MeshCollider>() == null)
			{
				currMeshFilter.gameObject.AddComponent<MeshCollider>();
			}
		}
	}
	
	public static void fixNormal(string assetPath, TextureImporter textureImporter)
    {
		string testPath = assetPath.ToLower();
        if (testPath.Contains("_nm.") ||
			testPath.Contains("_nrm.") ||
			testPath.Contains("_n.") ||
			testPath.Contains("_normal.")
			)
        {
			if (isDebugMode())
				Debug.Log("Fix Normal: " + assetPath);
            textureImporter.textureType = TextureImporterType.NormalMap;
        }
    }	
	
	private static void setStatic(GameObject gameObj)
	{
        var staticFlags = StaticEditorFlags.ContributeGI |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.NavigationStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.OffMeshLinkGeneration |
                        StaticEditorFlags.ReflectionProbeStatic;
        GameObjectUtility.SetStaticEditorFlags(gameObj, staticFlags);	
	}
	
	private static void setStaticRecursively(Transform trans)
	{
		 setStatic(trans.gameObject);
		 if(trans.childCount > 0)
			 foreach(Transform t in trans)
				 setStaticRecursively(t);
	}	
	
	public static void fixModel(GameObject gameObject)
	{
		// fix LOD -> from Leeds University
        //LODFix.FixLOD(gameObject.transform);
		
		// fix last LOD distance -> dont switch completly out
		var lodGroups = gameObject.GetComponentsInChildren<LODGroup>();
		foreach (var lodGroup in lodGroups)
		{
			string name = lodGroup.name;
			if (ImporterBase.isDebugMode())
				Debug.Log("lodGroup.name: " + lodGroup.name);
			
			bool hasLODData = false;
			string nodename = "Test"; // lodGroup.name;
			Dictionary<int, float> lodAttributes;			
			if (s_LODAttributes.TryGetValue(nodename, out lodAttributes))
				hasLODData = false; // true
					
			var lods = lodGroup.GetLODs();
			for (int i = 0; i < lods.Length; i++)	
			{
				float distance;
				if (hasLODData && lodAttributes.TryGetValue(i, out distance))
				{
					Debug.Log("set LOD Distance: " + lodGroup.name + "-> " + distance);
					lods[i].screenRelativeTransitionHeight = distance;
				}
					
				if (ImporterBase.isSupportLODCulling() && 
					i == lods.Length - 1) // last lods set to 0 -> always visible
					lods[i].screenRelativeTransitionHeight = 0;
			}			
			lodGroup.SetLODs(lods);
		}
		
		// set all objects recursive as static
		setStaticRecursively(gameObject.transform);	
	}
	
    public static void CreatePrefabFromModel( string path, GameObject modelAsset )
    {
        string modelFileName = Path.GetFileNameWithoutExtension( path );
 		var modelAssetInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);

		EnsureDirectoryExists( PREFAB_DESTINATION_DIRECTORY);
		
        string destinationPath = PREFAB_DESTINATION_DIRECTORY + modelFileName + ".prefab";

		Debug.Log("CreatePrefab: " + destinationPath);
		PrefabUtility.SaveAsPrefabAsset(modelAssetInstance, destinationPath);
    }	
}

// import images, external assets and and tiles
public class Trian3DTerrainImporter : AssetPostprocessor
{
	public Dictionary<string, GameObject> m_LoadedExternals = new Dictionary<string, GameObject>();
	
	private void loadAndAddExternal(object propertyValue, GameObject gameObject)
	{
		// collect external name to assign gameobj later in OnPostprocessAllAssets
		string externalPath = propertyValue.ToString();
		
		// load external
		GameObject external;
		if (!m_LoadedExternals.TryGetValue(externalPath, out external))
		{
			string assertPath = externalPath;
			if (ImporterBase.isSupportPrefab() && Path.GetExtension(externalPath) != ".prefab")
			{
				assertPath = ImporterBase.PREFAB_DESTINATION_DIRECTORY + Path.GetFileNameWithoutExtension(externalPath) + ".prefab";
			}
			
			external = AssetDatabase.LoadAssetAtPath<GameObject>(assertPath);
			if (external == null)
			{
				Debug.LogError("external model not found in AssetDatabase: " + assertPath);
				return;
			}
			m_LoadedExternals.Add(assertPath, external);
		}

		// create instance of external node
		GameObject externalInstance = UnityEngine.Object.Instantiate(external.gameObject);
		if (externalInstance == null)
			return;

		externalInstance.transform.position = gameObject.transform.position;
		externalInstance.transform.eulerAngles = externalInstance.transform.eulerAngles + gameObject.transform.eulerAngles;
		externalInstance.transform.localScale = new Vector3(gameObject.transform.lossyScale.x * external.transform.lossyScale.x,
															gameObject.transform.lossyScale.y * external.transform.lossyScale.y,
															gameObject.transform.lossyScale.z * external.transform.lossyScale.z);
		externalInstance.transform.SetParent(gameObject.transform, true);
	}		

	// 1. check if material not created yet
    // 2. then convert fbx user attributes (collected in OnPostprocessGameObjectWithUserProperties) to unity material value attributes
    public Material OnAssignMaterialModel(Material material, Renderer r)
    {
        if (ImporterBase.isDebugMode())
            Debug.Log("OnAssignMaterialModel: " + assetPath + "->" + material.name);

        ImporterBase.EnsureDirectoryExists(ImporterBase.MATERIAL_PATH);

        string materialPath = String.Format("{0}/{1}.mat", ImporterBase.MATERIAL_PATH, material.name);

		// Create a new material asset using the specular shader
        // but otherwise the default values from the model
        material.shader = Shader.Find("Standard");
		
		// enable GPU instancing only for external materials
		if (assetPath.Contains(ImporterBase.EXTERNAL_SUBPATH))
			material.enableInstancing = true;

        // Find if there is a material at the material path
        // Turn this off to always regeneration materials
        if (AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)))
        {
            Material loadedMaterial = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
            if (loadedMaterial)
                return loadedMaterial;
            else
                LogError("can not load Material: " + materialPath);
        }
		
        ImporterBase.BlendMode blendmode = ImporterBase.BlendMode.Opaque;
        List<ImporterBase.MaterialData> materialAttributes;
        if (ImporterBase.s_MaterialAttributes.TryGetValue(material.name, out materialAttributes))
        {
            foreach (ImporterBase.MaterialData data in materialAttributes)
            {
                if (ImporterBase.isDebugMode())
                    Debug.Log("mat data: " + data.name);

                if (data.type == "s") // string
                {
                    if (data.name == "Shader")
                    {
                        string shaderename = data.value.ToString();
                        Shader oNewShader = Shader.Find(shaderename);
                        if (oNewShader)
						{
                            material.shader = oNewShader;
							if (ImporterBase.isDebugMode())
								Debug.Log(" assign shader: " + shaderename + " - " + material.name);
						}
                        else
                            LogError("Shader " + shaderename + " not found!");
                    }
                    else if (data.name == "Rendermode")
                    {
                        if (data.value.ToString() == "Transparent")
                            blendmode = ImporterBase.BlendMode.Transparent;
                        else if (data.value.ToString() == "Cutout")
                            blendmode = ImporterBase.BlendMode.Cutout;
                        else if (data.value.ToString() == "Fade")
                            blendmode = ImporterBase.BlendMode.Fade;
                        else if (data.value.ToString() == "Opaque")
                            blendmode = ImporterBase.BlendMode.Opaque;
                        else
                            LogWarning("Unsupported Blendmode!");
                    }
                    else
                        LogWarning("no shader string attribute " + data.name);
                }
                else if (data.type == "t") // texture
                {
                    string texturename = data.value.ToString();
                    Texture texture = (Texture)AssetDatabase.LoadAssetAtPath(texturename, typeof(Texture));
                    if (texture)
                        material.SetTexture(data.name, texture);
                    else
                        LogError("texture not found " + texturename);

					// enable additioanl texture layer
                    if (data.name == "_BumpMap")
                        material.EnableKeyword("_NORMALMAP");
					else if (data.name == "_MetallicGlossMap")
						material.EnableKeyword("_METALLICGLOSSMAP");
					else if (data.name == "_ParallaxMap")
						material.EnableKeyword("_PARALLAXMAP");
                }
                else if (data.type == "f") // float
                {
					float value = float.Parse(data.value, System.Globalization.CultureInfo.InvariantCulture);
					material.SetFloat(data.name, value);
                }
                else if (data.type == "i") // integer
                {
					int value = Convert.ToInt32(data.value);
					material.SetInt(data.name, value);
                }
                else
                    LogWarning("not supported type of attribute " + data.name + "; type " + data.type);
            }

            ImporterBase.s_MaterialAttributes.Remove(material.name);
        }


        // add emissive color automatically
        Texture textureMain = material.GetTexture("_MainTex");
        if (textureMain)
        {
            string texName = AssetDatabase.GetAssetPath(textureMain);
            string extension = Path.GetExtension(texName);
            string emissiveName = Path.GetDirectoryName(texName) + "/" + Path.GetFileNameWithoutExtension(texName) + "_emissive" + extension;
            Texture textureEmissive = (Texture)AssetDatabase.LoadAssetAtPath(emissiveName, typeof(Texture));
            if (textureEmissive == null)
            {
                emissiveName = Path.GetDirectoryName(texName) + "/" + Path.GetFileNameWithoutExtension(texName) + "_emissive.png";
                textureEmissive = (Texture)AssetDatabase.LoadAssetAtPath(emissiveName, typeof(Texture));
            }

            if (textureEmissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", textureEmissive);
                material.SetColor("_EmissionColor", Color.white);
            }
        }

        // set rendermode and change material
        ImporterBase.setRenderMode(material, blendmode);

        AssetDatabase.CreateAsset(material, materialPath);

        return material;
    }	
	
    // 1. if attribute "Trian3D_XRefString" load and set references external (external folder should be add to unity before loading linked nodes files)
    // 2. if attribute "Trian3D_Mat" collect material attributes for later call of OnPostprocessMaterial
	// support further user attributes
    public void OnPostprocessGameObjectWithUserProperties(GameObject gameObject, string[] propNames, System.Object[] values)
    {
        if (ImporterBase.isDebugMode())
            Debug.Log("ExternalFbxImporter::OnPost: " + assetPath + "->" + gameObject.name);

        for (int i = 0; i < propNames.Length; i++)
        {
            string propName = propNames[i];
            object propertyValue = values[i];
            if (ImporterBase.isDebugMode())
                Debug.Log("Propname: " + propName + " value: " + propertyValue);

            if (propName == "Trian3D_XRefString")
				loadAndAddExternal(propertyValue, gameObject);
			if (propName.Contains("Trian3D_Mat"))
				ImporterBase.addMaterial(propName, propertyValue);
            else if (propName == "Trian3D_MinMaxDistance")
				ImporterBase.addLODInfo(propertyValue, gameObject.name);	
			else if (propName == "Trian3D_UseCollider")			
				ImporterBase.addCollider(gameObject);
            else if (propName == "Trian3D_PolygonLayerIndex") // doesn't work
            {
                // polygon offset will be place as height offset in Trian3DBuilder(MarkingZOffset or height offset)
            }				
        }
    }	
	
	// fix normal images
	public void OnPreprocessTexture()
    {	
		if (ImporterBase.isDebugMode())
			Debug.Log("OnPreprocessTexture: " + assetPath);
		
		ImporterBase.fixNormal(assetPath, (TextureImporter)assetImporter);
    }
	
	// fix LOD settings and set SetStaticEditorFlags
    public void OnPostprocessModel(GameObject gameObject)
    {
        if (ImporterBase.isDebugMode())
            Debug.Log("OnPostprocessModel: " + assetPath + "->" + gameObject.name);
				
		ImporterBase.fixModel(gameObject);
    }
	
	// fix prefab assignment
	static public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		Debug.Log("OnPostprocessAllAssets");
		
		ImporterBase.s_MaterialAttributes.Clear();
		
		// update all changed materials
		AssetDatabase.Refresh();
		
		if (ImporterBase.isSupportPrefab())
		{
			// create prefabs to have posiibility to manipulate externals one time and apply changes to all instances
			foreach( string path in importedAssets )
			{
				if( !path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) )
					continue;
				
				Debug.Log("imported fbx: " + path);
	 
				GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>( path );
	 
				ImporterBase.CreatePrefabFromModel( path, modelAsset );
			}	
		}			
	}	
}