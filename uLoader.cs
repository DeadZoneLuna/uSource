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
    //TODO: Fix Window
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
            #region Global Settings
            GUILayout.BeginVertical("minibutton");
            if (uLoader.GlobalSettingsFoldout = EditorGUILayout.Foldout(uLoader.GlobalSettingsFoldout, "Global Settings", true, EditorStyles.miniButtonLeft))
            {
                uLoader.RootPath = EditorGUILayout.TextField("Root path:", uLoader.RootPath);

                #region Mod Settings
                GUILayout.BeginVertical("minibutton");
                if (uLoader.ModSettingsFoldout = EditorGUILayout.Foldout(uLoader.ModSettingsFoldout, "Mod Settings", true, EditorStyles.miniButtonLeft))
                {
                    Int32 ModSize = uLoader.ModFolders.Length;
                    for (Int32 ModID = 0; ModID < ModSize; ModID++)
                    {
                        GUILayout.BeginVertical("helpbox");
                        uLoader.ModFolders[ModID] = EditorGUILayout.TextField(ModID == 0 ? "Main Mod: " : "Dependent Mod: ", uLoader.ModFolders[ModID]);

                        GUILayout.Label("VPK Archives", EditorStyles.boldLabel);
                        Int32 DirSize = uLoader.DirPaks[ModID].Length;
                        for (Int32 PakID = 0; PakID < DirSize; PakID++)
                        {
                            if (uLoader.DirPaks[ModID] != null)
                                uLoader.DirPaks[ModID][PakID] = EditorGUILayout.TextField(uLoader.DirPaks[ModID][PakID]);
                        }

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add VPK", EditorStyles.toolbarButton))
                        {
                            Int32 ArraySize = DirSize;
                            ArraySize++;
                            Array.Resize(ref uLoader.DirPaks[ModID], ArraySize);
                        }
                        if (GUILayout.Button("Remove VPK", EditorStyles.toolbarButton))
                        {
                            Int32 ArraySize = DirSize;
                            if (ArraySize > 0)
                            {
                                ArraySize--;
                                Array.Resize(ref uLoader.DirPaks[ModID], ArraySize);
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Mod", EditorStyles.toolbarButton))
                    {
                        Int32 ArraySize = uLoader.ModFolders.Length;
                        ArraySize++;
                        Array.Resize(ref uLoader.ModFolders, ArraySize);
                        Array.Resize(ref uLoader.DirPaks, ArraySize);
                        uLoader.DirPaks[ArraySize - 1] = new String[] { };
                    }
                    if (GUILayout.Button("Remove Mod", EditorStyles.toolbarButton))
                    {
                        Int32 ArraySize = uLoader.ModFolders.Length;
                        if (ArraySize > 0)
                        {
                            ArraySize--;
                            Array.Resize(ref uLoader.ModFolders, ArraySize);
                            Array.Resize(ref uLoader.DirPaks, ArraySize);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                #endregion

                GUILayout.BeginVertical("helpbox");
                GUILayout.Label("When resources loaded, they are stored in the scene.\n\nEnable this option, loaded resources will be saved to project\n& can edit them. (Textures and Materials at the moment)", EditorStyles.boldLabel);

                uLoader.SaveAssetsToUnity = EditorGUILayout.ToggleLeft("(Save / Load) assets (to / from) project (Beta)", uLoader.SaveAssetsToUnity);
                if (uLoader.SaveAssetsToUnity)
                {
                    uLoader.OutputAssetsFolder = EditorGUILayout.TextField("Output path: ", uLoader.OutputAssetsFolder);
                    uLoader.ExportTextureAsPNG = EditorGUILayout.ToggleLeft("Convert textures as PNG (Editable format)", uLoader.ExportTextureAsPNG);
                }

                GUILayout.EndVertical();

                uLoader.UnitScale = EditorGUILayout.FloatField("Unit scale:", uLoader.UnitScale);
                uLoader.LoadAnims = EditorGUILayout.ToggleLeft("Load animations (Beta)", uLoader.LoadAnims);
                uLoader.ClearDirectoryCache = EditorGUILayout.ToggleLeft("Clear directory cache", uLoader.ClearDirectoryCache);
                uLoader.ClearModelCache = EditorGUILayout.ToggleLeft("Clear model cache", uLoader.ClearModelCache);
                uLoader.ClearMaterialCache = EditorGUILayout.ToggleLeft("Clear material cache", uLoader.ClearMaterialCache);
                uLoader.ClearTextureCache = EditorGUILayout.ToggleLeft("Clear texture cache", uLoader.ClearTextureCache);

                if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                    uLoader.Clear();
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region BSP
            GUILayout.BeginVertical("minibutton");
            if (uLoader.BSPSettingsFoldout = EditorGUILayout.Foldout(uLoader.BSPSettingsFoldout, "BSP Import Settings", true, EditorStyles.miniButtonLeft))
            {
                uLoader.ParseBSPPhysics = EditorGUILayout.ToggleLeft("Parse physics (Unstable)", uLoader.ParseBSPPhysics);
                uLoader.ParseStaticPropScale = EditorGUILayout.ToggleLeft("Parse Scale on static props lumps (only for v11 & csgo)", uLoader.ParseStaticPropScale);
                uLoader.Use3DSkybox = EditorGUILayout.ToggleLeft("Use 3D Skybox", uLoader.Use3DSkybox);
                uLoader.ParseDecals = EditorGUILayout.ToggleLeft("Parse decals (Beta)", uLoader.ParseDecals);

                #region Lightmap settings
                GUILayout.BeginVertical("minibutton");
                if (uLoader.LightmapSettingsFoldout = EditorGUILayout.Foldout(uLoader.LightmapSettingsFoldout, "Lightmap Settings", true, EditorStyles.miniButtonLeft))
                {
                    uLoader.ParseLightmaps = EditorGUILayout.ToggleLeft("Parse original lightmaps (BSP)", uLoader.ParseLightmaps);
                    if (uLoader.ParseLightmaps)
                    {
                        uLoader.UseGammaLighting = EditorGUILayout.ToggleLeft("Use gamma color space on lightmaps", uLoader.UseGammaLighting);
                        uLoader.UseLightmapsAsTextureShader = EditorGUILayout.ToggleLeft("Set lightmap texture on material", uLoader.UseLightmapsAsTextureShader);
                    }

                    GUILayout.Space(5);

                    uLoader.GenerateUV2StaticProps = EditorGUILayout.ToggleLeft("Generate UV2 (Lightmaps) for static props", uLoader.GenerateUV2StaticProps);
                    if (uLoader.GenerateUV2StaticProps)
                    {
                        GUILayout.BeginVertical("box");
                        GUILayout.Label("Static Props (Models)", EditorStyles.boldLabel);

                        GUILayout.BeginVertical("helpbox");
                        uLoader.ModelsLightmapSize = EditorGUILayout.FloatField("Lightmap scale factor", uLoader.ModelsLightmapSize);
                        GUILayout.Label("Used to scale lightmap on models (editor & rebake only)", EditorStyles.miniBoldLabel);
                        GUILayout.EndVertical();

                        uLoader.UV2HardAngleProps = EditorGUILayout.IntSlider("Hard Angle: ", uLoader.UV2HardAngleProps, 0, 180);
                        uLoader.UV2AngleErrorProps = EditorGUILayout.IntSlider("Angle Error: ", uLoader.UV2AngleErrorProps, 1, 100);
                        uLoader.UV2AreaErrorProps = EditorGUILayout.IntSlider("Area Error: ", uLoader.UV2AreaErrorProps, 1, 100);
                        uLoader.UV2PackMarginTexSize = EditorGUILayout.FloatField("Margin Size: ", uLoader.UV2PackMarginTexSize);
                        uLoader.UV2PackMarginProps = EditorGUILayout.IntSlider("Pack Margin: ", uLoader.UV2PackMarginProps, 1, 64);
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndVertical();
                #endregion

                #region Lighting Settings
                GUILayout.BeginVertical("minibutton");
                if (uLoader.LightingSettingsFoldout = EditorGUILayout.Foldout(uLoader.LightingSettingsFoldout, "Lighting Settings", true, EditorStyles.miniButtonLeft))
                {
                    uLoader.ParseLights = EditorGUILayout.ToggleLeft("Parse lights (Beta)", uLoader.ParseLights);
                    if (uLoader.ParseLights)
                    {
                        GUILayout.BeginVertical("helpbox");
                        GUILayout.Label("BSP already store lights in \"dworldlight\" structure\n!!!Recommend to use it!!!", EditorStyles.boldLabel);

                        uLoader.UseWorldLights = EditorGUILayout.ToggleLeft("Use world lights", uLoader.UseWorldLights);

                        GUILayout.EndVertical();

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

                        uLoader.CustomCascadedShadowResolution = EditorGUILayout.IntSlider("Directional Shadow Map Size:", uLoader.CustomCascadedShadowResolution, 64, 8192);
                        uLoader.IgnoreShadowControl = EditorGUILayout.ToggleLeft("Ignore shadow control", uLoader.IgnoreShadowControl);
                        uLoader.UseDynamicLight = EditorGUILayout.ToggleLeft("Dynamic shadows", uLoader.UseDynamicLight);
                    }
                }
                GUILayout.EndVertical();
                #endregion

                uLoader.DebugEntities = EditorGUILayout.ToggleLeft("Debug entities", uLoader.DebugEntities);
                uLoader.MapName = EditorGUILayout.TextField("Map Name:", uLoader.MapName);
                if (GUILayout.Button("Load BSP", EditorStyles.toolbarButton))
                {
                    uLoader.DebugTime = new System.Diagnostics.Stopwatch();
                    uLoader.DebugTimeOutput = new System.Text.StringBuilder();
                    uLoader.DebugTime.Start();

                    uLoader.Clear();
                    uResourceManager.LoadMap(uLoader.MapName);
                    uLoader.DebugTime = null;
                }

                if (GUILayout.Button("Show/Hide Brushes", EditorStyles.toolbarButton))
                {
                    for (Int32 i = 0; i < VBSPFile.BSP_Brushes.Count; i++)
                        VBSPFile.BSP_Brushes[i].GetComponent<Renderer>().enabled = !VBSPFile.BSP_Brushes[i].GetComponent<Renderer>().enabled;
                }
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region MDL
            GUILayout.BeginVertical("minibutton");
            if (uLoader.MDLSettingsFoldout = EditorGUILayout.Foldout(uLoader.MDLSettingsFoldout, "MDL Import Settings", true, EditorStyles.miniButtonLeft))
            {
                uLoader.UseStaticPropFlag = EditorGUILayout.ToggleLeft("Load static bones", uLoader.UseStaticPropFlag);
                uLoader.UseHitboxesOnModel = EditorGUILayout.ToggleLeft("Load hitboxes model", uLoader.UseHitboxesOnModel);
                uLoader.DrawArmature = EditorGUILayout.ToggleLeft("Debug skeleton / bones", uLoader.DrawArmature);
                uLoader.ModelPath = EditorGUILayout.TextField("Model:", uLoader.ModelPath);
                if (GUILayout.Button("Load StudioModel", EditorStyles.toolbarButton))
                {
                    uLoader.Clear();
                    uResourceManager.Init();
                    uResourceManager.LoadModel(uLoader.ModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                    uResourceManager.ExportFromCache();
                    uResourceManager.CloseStreams();
                }

                uLoader.SubModelPath = EditorGUILayout.TextField("Sub-Model: ", uLoader.SubModelPath);
                if (GUILayout.Button("Load StudioModel + SubModel", EditorStyles.toolbarButton))
                {
                    uLoader.Clear();
                    uResourceManager.Init();
                    Transform mainMDL = uResourceManager.LoadModel(uLoader.ModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                    Transform subMDL = uResourceManager.LoadModel(uLoader.SubModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                    uResourceManager.ExportFromCache();
                    uResourceManager.CloseStreams();

                    foreach (SkinnedMeshRenderer SkinnedMesh in subMDL.GetComponentsInChildren<SkinnedMeshRenderer>())
                        mainMDL.CreateSubModel(SkinnedMesh);

                    UnityEngine.Object.DestroyImmediate(subMDL.gameObject, false);
                }
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region VMT
            GUILayout.BeginVertical("minibutton");
            if (uLoader.VMTSettingsFoldout = EditorGUILayout.Foldout(uLoader.VMTSettingsFoldout, "VMT Settings", true, EditorStyles.miniButtonLeft))
            {
                uLoader.DebugMaterials = EditorGUILayout.ToggleLeft("Debug materials (Print VMT KeyValue data)", uLoader.DebugMaterials);

                GUILayout.BeginVertical("box");
                GUILayout.Label("Global Shaders", EditorStyles.boldLabel);
                uLoader.DefaultShader = EditorGUILayout.TextField("Default Shader: ", uLoader.DefaultShader);
                uLoader.LightmappedGenericShader = EditorGUILayout.TextField("LightmappedGeneric Shader: ", uLoader.LightmappedGenericShader);
                uLoader.VertexLitGenericShader = EditorGUILayout.TextField("VertexLitGeneric Shader: ", uLoader.VertexLitGenericShader);
                uLoader.WorldVertexTransitionShader = EditorGUILayout.TextField("WorldVertexTransition Shader: ", uLoader.WorldVertexTransitionShader);
                uLoader.WorldTwoTextureBlend = EditorGUILayout.TextField("WorldTwoTextureBlend Shader: ", uLoader.WorldTwoTextureBlend);
                uLoader.UnlitGeneric = EditorGUILayout.TextField("Unlit Shader: ", uLoader.UnlitGeneric);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("Additive Shaders", EditorStyles.boldLabel);
                uLoader.AdditiveShader = EditorGUILayout.TextField("Additive Shader: ", uLoader.AdditiveShader);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("Detail Shaders", EditorStyles.boldLabel);
                uLoader.DetailShader = EditorGUILayout.TextField("Detail Shader: ", uLoader.DetailShader);
                uLoader.DetailUnlitShader = EditorGUILayout.TextField("Unlit Shader: ", uLoader.DetailUnlitShader);
                uLoader.DetailTranslucentShader = EditorGUILayout.TextField("Translucent Shader: ", uLoader.DetailTranslucentShader);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("Translucent Shaders", EditorStyles.boldLabel);
                uLoader.TranslucentShader = EditorGUILayout.TextField("Translucent Shader: ", uLoader.TranslucentShader);
                uLoader.TranslucentUnlitShader = EditorGUILayout.TextField("Unlit Shader: ", uLoader.TranslucentUnlitShader);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("AlphaTest (Cutout) Shaders", EditorStyles.boldLabel);
                uLoader.AlphaTestShader = EditorGUILayout.TextField("AlphaTest Shader: ", uLoader.AlphaTestShader);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("SelfIllum Shaders", EditorStyles.boldLabel);
                uLoader.SelfIllumShader = EditorGUILayout.TextField("SelfIllum Shader: ", uLoader.SelfIllumShader);
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region Info
            GUILayout.BeginHorizontal("textfield");

            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: 1.1 (Beta)\n\nSpecial thanks:\n\n->REDxEYE and ShadelessFox (for SourceIO & some help)\n->ZeqMacaw (for Crowbar)\n->James King aka Metapyziks (for SourceUtils)\n->LogicAndTrick (for Sledge and Sledge-Formats)", EditorStyles.largeLabel);
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
#if UNITY_EDITOR
        public static Boolean GlobalSettingsFoldout = true;
        public static Boolean ModSettingsFoldout = true;
        public static Boolean BSPSettingsFoldout = true;
        public static Boolean LightmapSettingsFoldout = true;
        public static Boolean LightingSettingsFoldout = true;
        public static Boolean MDLSettingsFoldout = true;
        public static Boolean VMTSettingsFoldout = false;
#endif

        #region Global Settings
        //Global settings
        public static String RootPath = @"F:\Games\Source Engine\Counter-Strike Source";
        public static String[] ModFolders = { "cstrike", "hl2" };
        //"hl2_misc_dir", "hl2_textures_dir"
        //"bms_maps_dir", "bms_textures_dir", "bms_materials_dir", "bms_models_dir", "bms_misc_dir"
        //"hl2_misc_dir", "hl2_textures_dir", "hl2_materials_dir", "hl2_models_dir"
        public static String[][] DirPaks = new String[][]
        {
            new String[] { "cstrike_pak_dir" },
            new String[] { "hl2_misc_dir", "hl2_textures_dir" }
        };

        public static Single UnitScale = 0.0254f;
        public static Boolean SaveAssetsToUnity = false;
        public static String OutputAssetsFolder = "uSource";
        public static Boolean ExportTextureAsPNG = true;
        #region Lightmap Settings
        public static Boolean GenerateUV2StaticProps = true;
        public static Boolean ParseLightmaps = false;
        public static Single ModelsLightmapSize = 0.001f;
        public static Int32 UV2HardAngleProps = 88;
        public static Single UV2PackMarginTexSize = 256;
        public static Int32 UV2PackMarginProps = 1;
        public static Int32 UV2AngleErrorProps = 8;
        public static Int32 UV2AreaErrorProps = 15;
        #endregion
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
        public static Boolean ParseStaticPropScale = false;
        public static Boolean Use3DSkybox = true;
        public static Boolean ParseDecals = false;
        public static Boolean UseGammaLighting = true;
        public static Boolean UseLightmapsAsTextureShader = false;
        public static Boolean ParseLights = true;
        public static Boolean UseWorldLights = true;
        public static Single QuadraticIntensityFixer = 1;
        public static Single LightEnvironmentScale = 4;
        public static Int32 CustomCascadedShadowResolution = 8192;
        public static Boolean IgnoreShadowControl = false;
        public static Boolean UseDynamicLight = true;
        public static Boolean DebugEntities = true;
        public static Boolean DebugMaterials = true;
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

        #region VMT
        //Additive
        public static String AdditiveShader = "USource/AdditiveGeneric";
        //Detail
        public static String DetailShader = "USource/DetailGeneric";
        public static String DetailUnlitShader = "USource/UnlitGeneric";
        public static String DetailTranslucentShader = "USource/TranslucentGeneric";
        //Translucent
        public static String TranslucentShader = "USource/TranslucentGeneric";
        public static String TranslucentUnlitShader = "Unlit/Transparent";
        //AlphaTest (Cutout)
        public static String AlphaTestShader = "USource/CutoutGeneric";
        //SelfIllum
        public static String SelfIllumShader = "USource/IllumGeneric";
        //Global
        public static String DefaultShader = "Diffuse";
        public static String LightmappedGenericShader = "Diffuse";
        public static String VertexLitGenericShader = "Diffuse";
        public static String WorldVertexTransitionShader = "USource/Lightmapped/WorldVertexTransition";
        public static String WorldTwoTextureBlend = "USource/Lightmapped/WorldTwoTextureBlend";
        public static String UnlitGeneric = "USource/UnlitGeneric";
        #endregion

        public static System.Text.StringBuilder DebugTimeOutput;
        public static System.Diagnostics.Stopwatch DebugTime;
        public static void Clear()
        {
#if UNITY_EDITOR
            if (uResourceManager.ProjectPath != null)
                uResourceManager.ProjectPath = null;
#endif

            if(uResourceManager.Providers != null)
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

#if UNITY_EDITOR
            if (uResourceManager.UV2GenerateCache != null)
                uResourceManager.UV2GenerateCache.Clear();

            if (uResourceManager.TexExportCache != null)
                uResourceManager.TexExportCache.Clear();
#endif

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