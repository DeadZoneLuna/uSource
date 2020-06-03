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
            //GUILayout.Box(ConfigLoader.VPKPath);
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
        public static String LevelName = "test_angles"; // BSP
        public static String VpkName = "cstrike_pak_dir"; // VPK - TODO
        public static Boolean VpkUse = false; // Use VPK (not fully implemented)
        public static Boolean LoadMDL = false; //Load Only MDL file
        public static Boolean LoadLightmapsAsTextureShader = false;
        public static Boolean use3DSkybox = true;
        public static Boolean LoadMap = true;
        public static Boolean LoadInfoDecals = false; //This is just an example, you need to implement a complete decal system.
        public static Boolean DynamicLight = false;
        //HDR ONLY
        public static Boolean useHDRLighting = true;
        //HDR ONLY
        public static Boolean DrawArmature = true;
        public static String ModelName = "characters/hostage_04"; // MDL

        public static string BSPPath = GamePath + "/" + ModFolders[0] + "/maps/" + LevelName + ".bsp";
        public static string MDLPath = GamePath + "/" + ModFolders[0] + "/models/" + ModelName + ".mdl";
        public static string VPKPath = GamePath + "/" + ModFolders[0] + "/" + VpkName + ".vpk"; //TODO
        public static string SNDPath = GamePath + "/" + ModFolders[0] + "/sounds/"; //TODO
        public static string _PakPath
        {
            get
            {
#if !UNITY_EDITOR
                return Application.persistentDataPath + "/";
#else
                return Application.dataPath + "/_PakLevel/";
#endif
            }
        }

        public const float WorldScale = 0.0254f;
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