using UnityEngine;
using System;
using System.Collections.Generic;

namespace Engine.Source
{
#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ConfigLoader))]
    public class ConfigurationLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Box(ConfigLoader.BSPPath);

            if (GUILayout.Button("Load Source BSP"))
                BspLoader.Load(ConfigLoader.LevelName);

            GUILayout.Space(5);

            if (GUILayout.Button("Show/Hide Brushes"))
            {
                for (Int32 i = 0; i < BspLoader.BSP_Brushes.Count; i++)
                    BspLoader.BSP_Brushes[i].GetComponent<Renderer>().enabled =
                        !BspLoader.BSP_Brushes[i].GetComponent<Renderer>().enabled;
            }

            if (GUILayout.Button("Remove loaded objects in the scene"))
            {
                UnityEngine.Object.DestroyImmediate(BspLoader.BSP_WorldSpawn);
                RenderSettings.skybox = null;

                Debug.LogWarning("You need to restart the editor to free up RAM. TODO: need to fix that :D");
            }

            GUILayout.Space(10);
            GUILayout.Box(ConfigLoader.VPKPath);
            GUILayout.Box(ConfigLoader.MDLPath);
            if (GUILayout.Button("Load Studio Model"))
                StudioMDLLoader.Load(ConfigLoader.ModelName);
        }
    }
#endif

    public class ConfigLoader : MonoBehaviour
    {
        public static String GamePath = @"././SourceGames";
        public static readonly String[] ModFolders = { "cstrike", "hl2" };

        public static String LevelName = "aim_deagle7k"; // BSP
        public static String VpkName = "cstrike_pak_dir"; // VPK - TODO
        public static Boolean VpkUse = false; // Use VPK
		public static Boolean LoadMDL = true; //Load Only MDL file
		public static Boolean LoadMap = false;
		public static Boolean DrawArmature = true;
        public static String ModelName = "player/t_leet"; // MDL

        public static string BSPPath = GamePath + "/" + ModFolders[0] + "/maps/" + LevelName + ".bsp";
        public static string MDLPath = GamePath + "/" + ModFolders[0] + "/models/" + ModelName + ".mdl";
        public static string VPKPath = GamePath + "/" + ModFolders[0] + "/" + VpkName + ".vpk"; //TODO
        public static string SNDPath = GamePath + "/" + ModFolders[0] + "/sounds/"; //TODO

		public static float WorldScale = 0.0254f;
		public static List<LightmapData> lightmapsData; //Base LightmapData
		public static int CurrentLightmap = 0; //Lightmap Index Count

		void Start()
		{
			if (!LoadMDL && LoadMap)
			{
				BspLoader.Load(LevelName);
			} 
            else if (LoadMDL && !LoadMap)
			{
				StudioMDLLoader.Load(ModelName);
			}
        }
    }
}