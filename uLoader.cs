using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using uSource.Formats.Source.VBSP;
#if UNITY_EDITOR
using UnityEditor;
#endif

//TODO:
//Improve memory management and GC!
//Improve ResourceManager
//Improve BSP parsing (Lightmaps build in atlas, entites & etc...)
//Add BSP collision (again, i hope can do that xd)
//Add decal projections
//Add VertexLighting support
//Parse AmbientCubes to Light Probes
//Improve VTF parsing (add more formats without VTFLib!)
//Add cubemap support
//Improve VMT parsing
//Port some shaders from Source to Unity
//Improve MDL parsing (Decompress anim sectors, virtualmodels (ani), attachments, flexes with rules)
//Improve VVD / VTX parsing (for lods and 8 bytes length on some models in strips!)
//Add PHY Support
//Add DMX support
//Add PCF support
//Add SFM session support
namespace uSource
{
#if UNITY_EDITOR
    public class uLoaderWindow : EditorWindow
    {
        [MenuItem("uSource/Loader")]
        static void Init()
        {
            uLoaderWindow window = (uLoaderWindow)EditorWindow.GetWindow(typeof(uLoaderWindow));
            window.Show();
        }

        void OnGUI()
        {
            uLoaderEditor.DrawGUI();
        }
    }

    //TODO: Rework all this to serializable object?
    [CustomEditor(typeof(uLoader))]
    public class uLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            DrawGUI();
        }

        public static void DrawGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Global Settings", EditorStyles.boldLabel);
            uLoader.RootPath = EditorGUILayout.TextField("Root path:", uLoader.RootPath);
            uLoader.ModFolders[0] = EditorGUILayout.TextField("Mod Name:", uLoader.ModFolders[0]);
            uLoader.UnitScale = EditorGUILayout.FloatField("Unit scale:", uLoader.UnitScale);
            uLoader.SaveAssetsToUnity = EditorGUILayout.Toggle("Save loaded assets to Unity", uLoader.SaveAssetsToUnity);
            uLoader.LoadAnims = EditorGUILayout.Toggle("Load animations (Beta)", uLoader.LoadAnims);
            uLoader.ClearDirectoryCache = EditorGUILayout.Toggle("Clear directory cache", uLoader.ClearDirectoryCache);
            uLoader.ClearModelCache = EditorGUILayout.Toggle("Clear model cache", uLoader.ClearModelCache);
            uLoader.ClearMaterialCache = EditorGUILayout.Toggle("Clear material cache", uLoader.ClearMaterialCache);
            uLoader.ClearTextureCache = EditorGUILayout.Toggle("Clear texture cache", uLoader.ClearTextureCache);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            #region BSP
            GUILayout.BeginVertical("box");
            GUILayout.Label("BSP Import Settings", EditorStyles.boldLabel);

            uLoader.ParseBSPPhysics = EditorGUILayout.Toggle("Parse physics (Unstable)", uLoader.ParseBSPPhysics);
            uLoader.Use3DSkybox = EditorGUILayout.Toggle("Use 3D Skybox", uLoader.Use3DSkybox);
            uLoader.ParseDecals = EditorGUILayout.Toggle("Parse decals (Beta)", uLoader.ParseDecals);
            uLoader.ParseLightmaps = EditorGUILayout.Toggle("Parse Lightmaps to Unity", uLoader.ParseLightmaps);
            uLoader.UseGammaLighting = EditorGUILayout.Toggle("Use gamma color space", uLoader.UseGammaLighting);
            uLoader.UseLightmapsAsTextureShader = EditorGUILayout.Toggle("Use shader for lightmaps", uLoader.UseLightmapsAsTextureShader);

            uLoader.ParseLights = EditorGUILayout.Toggle("Parse lights (Beta)", uLoader.ParseLights);
            if (uLoader.ParseLights)
            {
                GUILayout.BeginVertical("textfield");
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical("helpbox");
                GUILayout.Label("BSP already have converted lights from entities to structure (WorldLights)\n!!!Recommend to use it!!!", EditorStyles.boldLabel);
                GUILayout.EndVertical();

                uLoader.UseWorldLights = EditorGUILayout.Toggle("Use world lights", uLoader.UseWorldLights);

                if (uLoader.UseWorldLights)
                {
                    GUILayout.BeginVertical("helpbox");
                    uLoader.QuadraticIntensityFixer = EditorGUILayout.FloatField("Quadratic intensity fix const", uLoader.QuadraticIntensityFixer);
                    GUILayout.Label("For rebake lightmaps it used lower value (def: 1~)\n\nFor fix up brightness with dynamic lights, value can be set higher (def: 4~)", EditorStyles.miniBoldLabel);
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("helpbox");
                    uLoader.LightEnvironmentScale = EditorGUILayout.FloatField("Scale intensity light environment", uLoader.LightEnvironmentScale);
                    GUILayout.Label("Directional light looks more darkness in Unity\nThis parameter fixup that (multiply intensity)", EditorStyles.miniBoldLabel);
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                uLoader.IgnoreShadowControl = EditorGUILayout.Toggle("Ignore shadow control", uLoader.IgnoreShadowControl);
                uLoader.UseDynamicLight = EditorGUILayout.Toggle("Dynamic shadows", uLoader.UseDynamicLight);
            }

            uLoader.DebugEntities = EditorGUILayout.Toggle("Debug entities", uLoader.DebugEntities);
            uLoader.MapName = EditorGUILayout.TextField("Map Name:", uLoader.MapName);
            if (GUILayout.Button("Load BSP"))
            {
                uLoader.Clear();
                uResourceManager.LoadMap(uLoader.MapName);
            }

            if (GUILayout.Button("Clear Cache"))
            {
                uLoader.Clear();
            }

            if (GUILayout.Button("Show/Hide Brushes"))
            {
                for (Int32 i = 0; i < VBSPFile.BSP_Brushes.Count; i++)
                    VBSPFile.BSP_Brushes[i].GetComponent<Renderer>().enabled = !VBSPFile.BSP_Brushes[i].GetComponent<Renderer>().enabled;
            }
            GUILayout.EndVertical();
            #endregion

            #region MDL
            GUILayout.BeginVertical("box");
            GUILayout.Label("MDL Import Settings", EditorStyles.boldLabel);

            uLoader.UseStaticPropFlag = EditorGUILayout.Toggle("Load static bones", uLoader.UseStaticPropFlag);
            GUILayout.BeginVertical("helpbox");
            uLoader.ModelsLightmapSize = EditorGUILayout.FloatField("Lightmap scale factor", uLoader.ModelsLightmapSize);
            GUILayout.Label("Used to scale lightmap on models (editor & rebake only)", EditorStyles.miniBoldLabel);
            GUILayout.EndVertical();

            uLoader.UseHitboxesOnModel = EditorGUILayout.Toggle("Load hitboxes model", uLoader.UseHitboxesOnModel);
            uLoader.DrawArmature = EditorGUILayout.Toggle("Debug skeleton / bones", uLoader.DrawArmature);
            uLoader.ModelPath = EditorGUILayout.TextField("Model:", uLoader.ModelPath);
            if (GUILayout.Button("Load StudioModel"))
            {
                uLoader.Clear();
                uResourceManager.Init();
                uResourceManager.LoadModel(uLoader.ModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                uResourceManager.CloseStreams();
            }

            uLoader.SubModelPath = EditorGUILayout.TextField("Sub-Model: ", uLoader.SubModelPath);
            if (GUILayout.Button("Load StudioModel + SubModel"))
            {
                uLoader.Clear();
                uResourceManager.Init();
                var mainMDL = uResourceManager.LoadModel(uLoader.ModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                var subMDL = uResourceManager.LoadModel(uLoader.SubModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                uResourceManager.CloseStreams();

                foreach (SkinnedMeshRenderer SkinnedMesh in subMDL.GetComponentsInChildren<SkinnedMeshRenderer>())
                    mainMDL.CreateSubModel(SkinnedMesh);

                UnityEngine.Object.DestroyImmediate(subMDL.gameObject, false);
            }
            if (GUILayout.Button("Load Multi StudioModel"))
            {
                uLoader.Clear();
                uResourceManager.Init();
                for (int i = 0; i < uLoader.ModelsTest.Length; i++)
                {
                    uResourceManager.LoadModel(uLoader.ModelsTest[i], uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                }
                uResourceManager.CloseStreams();
            }

            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(10);

            #region Info
            GUILayout.BeginHorizontal("textfield");

            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: 1.1 (Beta)\n\nSpecial thanks:\n\n->REDxEYE and ShadelessFox\n->ZeqMacaw (for Crowbar)\n->James King aka Metapyziks (for SourceUtils)\n->LogicAndTrick (for Sledge and Sledge-Formats)", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();

            //GUILayout.EndHorizontal();

            GUILayout.Space(2);

            GUILayout.BeginVertical();//"textarea");

            GUILayout.Label("Helpful Links:");

            if (GUILayout.Button("SourceIO"))
            {
                Application.OpenURL("https://github.com/REDxEYE/SourceIO");
            }

            if (GUILayout.Button("Crowbar"))
            {
                Application.OpenURL("https://github.com/ZeqMacaw/Crowbar");
            }

            if (GUILayout.Button("SourceUtils"))
            {
                Application.OpenURL("https://github.com/Metapyziks/SourceUtils");
            }

            if (GUILayout.Button("Sledge-Formats"))
            {
                Application.OpenURL("https://github.com/LogicAndTrick/sledge-formats");
            }

            if (GUILayout.Button("Sledge"))
            {
                Application.OpenURL("https://github.com/LogicAndTrick/sledge");
            }

            if (GUILayout.Button("ValveSoftware Wiki"))
            {
                Application.OpenURL("https://developer.valvesoftware.com/wiki/Main_Page");
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            #endregion
        }
    }
#endif

    public class uLoader : MonoBehaviour
    {
        #region Global Settings
        //Global settings
        public static String RootPath = @"F:\Games\Source Engine\Counter-Strike Source";
        public static String[] ModFolders = { "cstrike", "hl2" };
        //"hl2_misc_dir", "hl2_textures_dir"
        //"bms_maps_dir", "bms_textures_dir", "bms_materials_dir", "bms_models_dir", "bms_misc_dir"
        //"hl2_misc_dir", "hl2_textures_dir", "hl2_materials_dir", "hl2_models_dir"
        public static readonly String[][] DirPaks = new String[][]
        {
            new String[] { "cstrike_pak_dir" },
            new String[] { "hl2_misc_dir", "hl2_textures_dir" }
        };

        public static Single UnitScale = 0.0254f;
        public static Boolean SaveAssetsToUnity = false; // - TODO
        public static Boolean LoadAnims = false;
        public static Boolean ClearDirectoryCache = false;
        public static Boolean ClearModelCache = true;
        public static Boolean ClearMaterialCache = true;
        public static Boolean ClearTextureCache = true;
        //Global settings
        #endregion

        #region BSP
        //BSP
        public static String MapName = "test_lights";
        public static Boolean ParseBSPPhysics = false;
        public static Boolean Use3DSkybox = false;
        public static Boolean ParseDecals = false;
        public static Boolean ParseLightmaps = false;
        public static Boolean UseGammaLighting = true;
        public static Boolean UseLightmapsAsTextureShader = false;
        public static Boolean ParseLights = true;
        public static Boolean UseWorldLights = true;
        public static Single QuadraticIntensityFixer = 1;
        public static Single LightEnvironmentScale = 4;
        public static Boolean IgnoreShadowControl = false;
        public static Boolean UseDynamicLight = true;
        public static Boolean DebugEntities = true;
        //BSP

        public static List<LightmapData> lightmapsData; //Base LightmapData
        public static int CurrentLightmap = 0; //Lightmap Index Count
        #endregion

        #region MDL
        //MDL
        //weapons/v_rif_ak47
        //deadzone/characters/counter-strike source/t_leet_face
        //player/custom_player/legacy/tm_leet_varianta
        //props_vehicles/mining_car
        //player/ak_anims_t
        //weapons/v_357
        //weapons/v_pist_p228
        //models/weapons/v_models/v_smg_sniper
        //props_c17/door01_left
        //survivors/survivor_gambler
        public static String ModelPath = @"weapons/v_rif_ak47";
        public static String SubModelPath = @"weapons/ct_arms";
        public static Boolean UseStaticPropFlag = false;
        public static Single ModelsLightmapSize = 0f;
        public static Boolean UseHitboxesOnModel = false;
        public static Boolean DrawArmature = false;

        public static readonly String[] ModelsTest =
        {
            "models/weapons/v_c4",
            "models/weapons/v_knife_t",
            "models/weapons/v_eq_fraggrenade",
            "models/weapons/v_eq_flashbang",
            "models/weapons/v_eq_smokegrenade",
            "models/weapons/v_pist_glock18",
            "models/weapons/v_pist_usp",
            "models/weapons/v_pist_p228",
            "models/weapons/v_pist_deagle",
            "models/weapons/v_pist_elite",
            "models/weapons/v_pist_fiveseven",
            "models/weapons/v_shot_m3super90",
            "models/weapons/v_shot_xm1014",
            "models/weapons/v_smg_mac10",
            "models/weapons/v_smg_tmp",
            "models/weapons/v_smg_mp5",
            "models/weapons/v_smg_ump45",
            "models/weapons/v_smg_p90",
            "models/weapons/v_rif_galil",
            "models/weapons/v_rif_famas",
            "models/weapons/v_snip_scout",
            "models/weapons/v_rif_ak47",
            "models/weapons/v_rif_m4a1",
            "models/weapons/v_rif_sg552",
            "models/weapons/v_rif_aug",
            "models/weapons/v_snip_awp",
            "models/weapons/v_snip_g3sg1",
            "models/weapons/v_snip_sg550",
            "models/weapons/v_mach_m249para"
        };
        //MDL
        #endregion

        public static void Clear()
        {
            if(uResourceManager._providers != null)
            {
                uResourceManager.CloseStreams();
                uResourceManager.RemoveResourceProviders();
                GC.Collect();
            }

            if (VBSPFile.BSP_WorldSpawn != null)
                DestroyImmediate(VBSPFile.BSP_WorldSpawn);

            RenderSettings.skybox = null;

            if (uResourceManager.DirectoryCache != null && ClearDirectoryCache)
                uResourceManager.DirectoryCache.Clear();

            if (uResourceManager.ModelCache != null && ClearModelCache)
                uResourceManager.ModelCache.Clear();

            if (uResourceManager.MaterialCache != null && ClearMaterialCache)
                uResourceManager.MaterialCache.Clear();

            if (uResourceManager.TextureCache != null && ClearTextureCache)
                uResourceManager.TextureCache.Clear();

            if (!UseLightmapsAsTextureShader)
            {
                if (lightmapsData != null)
                    lightmapsData.Clear();

                LightmapSettings.lightmaps = null;
                CurrentLightmap = 0;
            }
        }
    }
}