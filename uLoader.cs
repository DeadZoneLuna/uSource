using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using uSource.Formats.Source.VBSP;
using uSource.Formats.Source.MDL;
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

            if (!uLoader.PresetLoaded)
                uLoader.LoadPreset();

            GUILayout.BeginVertical("box");
            {
                if (uLoader.GlobalSettingsFoldout = EditorGUILayout.Foldout(uLoader.GlobalSettingsFoldout, "Global Settings", true, EditorStyles.miniButtonLeft))
                {
                    uLoader.RootPath = EditorGUILayout.TextField("Root path:", uLoader.RootPath);

                    #region Mod Settings
                    GUILayout.BeginVertical("box");
                    {
                        if (uLoader.ModSettingsFoldout = EditorGUILayout.Foldout(uLoader.ModSettingsFoldout, "Mod Settings", true, EditorStyles.miniButtonLeft))
                        {
                            Int32 ModSize = uLoader.ModFolders.Length;
                            for (Int32 ModID = 0; ModID < ModSize; ModID++)
                            {
                                GUILayout.BeginVertical("helpbox");
                                {
                                    uLoader.ModFolders[ModID] = EditorGUILayout.TextField(ModID == 0 ? "Main Mod: " : "Dependent Mod: ", uLoader.ModFolders[ModID]);

                                    GUILayout.Label("VPK Archives", EditorStyles.boldLabel);
                                    Int32 DirSize = uLoader.DirPaks[ModID].Length;
                                    for (Int32 PakID = 0; PakID < DirSize; PakID++)
                                    {
                                        if (uLoader.DirPaks[ModID] != null)
                                            uLoader.DirPaks[ModID][PakID] = EditorGUILayout.TextField(uLoader.DirPaks[ModID][PakID]);
                                    }

                                    GUILayout.BeginHorizontal();
                                    {
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
                                    }
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }

                            GUILayout.BeginHorizontal();
                            {
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
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                    #endregion

                    #region LOD Settings
                    GUILayout.BeginVertical("box");
                    {
                        if (uLoader.LodSettingsFoldout = EditorGUILayout.Foldout(uLoader.LodSettingsFoldout, "Models LOD Settings", true, EditorStyles.miniButtonLeft))
                        {
                            uLoader.EnableLODParsing = EditorGUILayout.ToggleLeft("Enable LOD (Beta)", uLoader.EnableLODParsing);

                            if (uLoader.EnableLODParsing)
                            {
                                uLoader.DetailMode = (DetailMode)EditorGUILayout.EnumPopup("Model Detail: ", uLoader.DetailMode);

                                if (uLoader.DetailMode == DetailMode.None)
                                {
                                    uLoader.NegativeAddLODPrecent = EditorGUILayout.Slider("Add percent difference (Last LOD)", uLoader.NegativeAddLODPrecent, 0.01f, 1f);

                                    GUILayout.BeginVertical("helpbox");
                                    GUILayout.Label("Used to avoid errors LODGroup", EditorStyles.boldLabel);
                                    uLoader.ThresholdMaxSwitch = EditorGUILayout.Slider("Threshold max switch point", uLoader.ThresholdMaxSwitch, 0.01f, 10f);
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("helpbox");
                                    GUILayout.Label("Low percent - Less distance between each LOD.\nHigh percent - Greater distance between each LOD.");
                                    uLoader.SubstractLODPrecent = EditorGUILayout.Slider("Percent distance (Substract)", uLoader.SubstractLODPrecent, 0.01f, 1f);
                                    GUILayout.EndVertical();
                                }
                            }
                        }
                    }
                    GUILayout.EndVertical();
                    #endregion

                    uLoader.LoadAnims = EditorGUILayout.ToggleLeft("Load animations (Beta)", uLoader.LoadAnims);
                    uLoader.ClearDirectoryCache = EditorGUILayout.ToggleLeft("Clear directory cache", uLoader.ClearDirectoryCache);
                    uLoader.ClearModelCache = EditorGUILayout.ToggleLeft("Clear model cache", uLoader.ClearModelCache);
                    uLoader.ClearMaterialCache = EditorGUILayout.ToggleLeft("Clear material cache", uLoader.ClearMaterialCache);
                    uLoader.ClearTextureCache = EditorGUILayout.ToggleLeft("Clear texture cache", uLoader.ClearTextureCache);
                    uLoader.UnitScale = EditorGUILayout.FloatField("Unit scale:", uLoader.UnitScale);

                    GUILayout.BeginVertical("helpbox");
                    {
                        uLoader.SaveAssetsToUnity = EditorGUILayout.ToggleLeft("(Save / Load) assets (to / from) project (Beta)", uLoader.SaveAssetsToUnity);
                        if (uLoader.SaveAssetsToUnity)
                        {
                            uLoader.OutputAssetsFolder = EditorGUILayout.TextField("Output path: ", uLoader.OutputAssetsFolder);
                            uLoader.ExportTextureAsPNG = EditorGUILayout.ToggleLeft("Convert textures as PNG (Editable format)", uLoader.ExportTextureAsPNG);
                        }
                    }
                    GUILayout.EndVertical();
                }

                #region Presets
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginVertical("helpbox");
                    GUILayout.Label("Preset Settings", EditorStyles.largeLabel);
                    GUILayout.EndVertical();

                    #region Auto Save
                    GUILayout.BeginVertical();
                    {
                        uLoader.SaveAfterResetPreset = EditorGUILayout.ToggleLeft("Auto Save Preset (After Reset)", uLoader.SaveAfterResetPreset);
                        uLoader.AutoSavePreset = EditorGUILayout.ToggleLeft("Auto Save Presets", uLoader.AutoSavePreset);
                    }
                    GUILayout.EndVertical();
                    #endregion

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Load Preset", EditorStyles.toolbarButton))
                            uLoader.LoadPreset();

                        if (GUILayout.Button("Save Preset", EditorStyles.toolbarButton))
                            uLoader.SavePreset();

                        if (GUILayout.Button("Reset Preset", EditorStyles.toolbarButton))
                            uLoader.ResetPreset();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                #endregion

                if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                    uLoader.Clear();
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region BSP
            GUILayout.BeginVertical("box");
            {
                if (uLoader.BSPSettingsFoldout = EditorGUILayout.Foldout(uLoader.BSPSettingsFoldout, "BSP Import Settings", true, EditorStyles.miniButtonLeft))
                {
                    uLoader.ParseBSPPhysics = EditorGUILayout.ToggleLeft("Parse physics (Unstable)", uLoader.ParseBSPPhysics);
                    uLoader.ParseStaticPropScale = EditorGUILayout.ToggleLeft("Parse static props scale (only for v11 & csgo)", uLoader.ParseStaticPropScale);
                    uLoader.Use3DSkybox = EditorGUILayout.ToggleLeft("Use 3D Skybox", uLoader.Use3DSkybox);
                    uLoader.ParseDecals = EditorGUILayout.ToggleLeft("Parse decals (Beta)", uLoader.ParseDecals);
                    uLoader.DebugEntities = EditorGUILayout.ToggleLeft("Debug entities", uLoader.DebugEntities);
                    #region Lightmap settings
                    GUILayout.BeginVertical("box");
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
                    GUILayout.BeginVertical("box");
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

                    uLoader.MapName = EditorGUILayout.TextField("Map Name:", uLoader.MapName);
                    if (GUILayout.Button("Load BSP", EditorStyles.toolbarButton))
                    {
                        if (uLoader.AutoSavePreset)
                            uLoader.SavePreset();

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
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region MDL
            GUILayout.BeginVertical("box");
            {
                if (uLoader.MDLSettingsFoldout = EditorGUILayout.Foldout(uLoader.MDLSettingsFoldout, "MDL Import Settings", true, EditorStyles.miniButtonLeft))
                {
                    uLoader.UseStaticPropFlag = EditorGUILayout.ToggleLeft("Load static bones", uLoader.UseStaticPropFlag);
                    uLoader.UseHitboxesOnModel = EditorGUILayout.ToggleLeft("Load hitboxes model", uLoader.UseHitboxesOnModel);
                    uLoader.DrawArmature = EditorGUILayout.ToggleLeft("Debug skeleton / bones", uLoader.DrawArmature);
                    uLoader.ModelPath = EditorGUILayout.TextField("Model:", uLoader.ModelPath);
                    if (GUILayout.Button("Load StudioModel", EditorStyles.toolbarButton))
                    {
                        if (uLoader.AutoSavePreset)
                            uLoader.SavePreset();

                        uLoader.Clear();
                        uResourceManager.Init();
                        uResourceManager.LoadModel(uLoader.ModelPath, uLoader.LoadAnims, uLoader.UseHitboxesOnModel);
                        uResourceManager.ExportFromCache();
                        uResourceManager.CloseStreams();
                    }

                    uLoader.SubModelPath = EditorGUILayout.TextField("Sub-Model: ", uLoader.SubModelPath);
                    if (GUILayout.Button("Load StudioModel + SubModel", EditorStyles.toolbarButton))
                    {
                        if (uLoader.AutoSavePreset)
                            uLoader.SavePreset();

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
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region VMT
            GUILayout.BeginVertical("box");
            {
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
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(2);

            #region Info
            GUILayout.BeginHorizontal("textfield");

            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + uLoader.PluginVersion + "\n\nSpecial thanks:\n\n->REDxEYE and ShadelessFox (for SourceIO & some help)\n->ZeqMacaw (for Crowbar)\n->James King aka Metapyziks (for SourceUtils)\n->LogicAndTrick (for Sledge and Sledge-Formats)", EditorStyles.largeLabel);
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
        #region Editor Stuff
        public static String PluginVersion = "1.1 Beta";
#if UNITY_EDITOR
        public static Boolean GlobalSettingsFoldout = true;
        public static Boolean ModSettingsFoldout = true;
        public static Boolean LodSettingsFoldout = true;
        public static Boolean BSPSettingsFoldout = true;
        public static Boolean LightmapSettingsFoldout = true;
        public static Boolean LightingSettingsFoldout = true;
        public static Boolean MDLSettingsFoldout = true;
        public static Boolean VMTSettingsFoldout = false;
#endif
        #endregion

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

        //Export Feature
        public static Boolean SaveAssetsToUnity = false;
        public static String OutputAssetsFolder = "uSource";
        public static Boolean ExportTextureAsPNG = true;
        #region Lightmap Settings
        public static Boolean GenerateUV2StaticProps = true;
        public static Boolean ParseLightmaps = false;
        public static Single ModelsLightmapSize = 1f;
        public static Int32 UV2HardAngleProps = 88;
        public static Single UV2PackMarginTexSize = 256;
        public static Int32 UV2PackMarginProps = 1;
        public static Int32 UV2AngleErrorProps = 8;
        public static Int32 UV2AreaErrorProps = 15;
        #endregion

        #region LOD Settings
        public static Boolean EnableLODParsing = false;
        public static DetailMode DetailMode = DetailMode.None;
        public static Single NegativeAddLODPrecent = 0.2f;
        public static Single ThresholdMaxSwitch = 0.1f;
        public static Single SubstractLODPrecent = 0.25f;
        #endregion
        public static Single UnitScale = 0.0254f;
        public static Boolean LoadAnims = false;
        public static Boolean ClearDirectoryCache = false;
        public static Boolean ClearModelCache = true;
        public static Boolean ClearMaterialCache = true;
        public static Boolean ClearTextureCache = true;
        //Global settings
        #endregion

        #region BSP
        //BSP
        public static String MapName = "test_angles";
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
        public static String ModelPath = @"player/t_leet";
        public static String SubModelPath = @"weapons/ct_arms";
        public static Boolean UseStaticPropFlag = false;
        public static Boolean UseHitboxesOnModel = false;
        public static Boolean DrawArmature = false;
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

        #region Save / Load Feature
        #if UNITY_EDITOR

        #region Prefs
        public static String[] GetStringArray(String Key, String[] DefaultValue)
        {
            if (!PlayerPrefs.HasKey(Key))
                return DefaultValue;
            
            return PlayerPrefs.GetString(Key).Split('|');
        }

        public static void SetStringArray(String Key, String[] Value)
        {
            StringBuilder Builder = new StringBuilder();

            Int32 DefSize = Value.Length;
            for (Int32 i = 0; i < DefSize; i++)
            {
                Builder.Append(Value[i]);
                if (i != DefSize - 1)
                    Builder.Append("|"); // Split char
            }

            PlayerPrefs.SetString(Key, Builder.ToString());
        }

        public static String[][] GetString2DArray(String Key, String[][] DefaultValue)
        {
            if (!PlayerPrefs.HasKey(Key))
                return DefaultValue;

            String[] SplitData = PlayerPrefs.GetString(Key).Split('|');
            String[][] Array = new String[SplitData.Length][];
            for (Int32 i = 0; i < Array.Length; i++)
            {
                String[] ArrayData = SplitData[i].Split(new[] { '!' }, StringSplitOptions.RemoveEmptyEntries);

                if (ArrayData.Length <= 0)
                    Array[i] = new String[] { };
                else
                    Array[i] = ArrayData;
            }

            return Array;
        }

        public static void SetString2DArray(String Key, String[][] Value)
        {
            StringBuilder Builder = new StringBuilder();

            Int32 DefSize = Value.Length;
            for (Int32 i = 0; i < DefSize; i++)
            {
                Int32 Def2Size = Value[i].Length;
                for (Int32 j = 0; j < Def2Size; j++)
                {
                    if (Value[i][j] != null)
                    {
                        Builder.Append(Value[i][j]);
                        if (j != Def2Size - 1)
                            Builder.Append("!"); // Split char
                    }
                }

                if (i != DefSize - 1)
                    Builder.Append("|"); // Split char
            }

            PlayerPrefs.SetString(Key, Builder.ToString());
        }

        public static Boolean GetBool(String Key, Boolean DefaultValue)
        {
            return PlayerPrefs.GetInt(Key, DefaultValue == false ? 0 : 1) == 0 ? false : true;
        }

        public static void SetBool(String Key, Boolean Value)
        {
            PlayerPrefs.SetInt(Key, Value == false ? 0 : 1);
        }

        public static T GetEnum<T>(String Key, T DefaultValue)
        {
            if (!PlayerPrefs.HasKey(Key))
                return DefaultValue;

            return (T)Enum.Parse(typeof(T), PlayerPrefs.GetString(Key));
        }

        public static void SetEnum<T>(String Key, T Value)
        {
            PlayerPrefs.SetString(Key, Value.ToString());
        }
        #endregion

        public static Boolean PresetLoaded = false;
        public static Boolean AutoSavePreset = true;
        public static Boolean SaveAfterResetPreset = false;
        public static void LoadPreset()
        {
            PresetLoaded = true;

            #region Editor Stuff
            AutoSavePreset = GetBool("uAutoSavePreset", AutoSavePreset);
            SaveAfterResetPreset = GetBool("uSaveAfterResetPreset", SaveAfterResetPreset);
            GlobalSettingsFoldout = GetBool("uGlobalSettingsFoldout", GlobalSettingsFoldout);
            ModSettingsFoldout = GetBool("uModSettingsFoldout", ModSettingsFoldout);
            LodSettingsFoldout = GetBool("uLodSettingsFoldout", LodSettingsFoldout);
            BSPSettingsFoldout = GetBool("uBSPSettingsFoldout", BSPSettingsFoldout);
            LightmapSettingsFoldout = GetBool("uLightmapSettingsFoldout", LightmapSettingsFoldout);
            LightingSettingsFoldout = GetBool("uLightingSettingsFoldout", LightingSettingsFoldout);
            MDLSettingsFoldout = GetBool("uMDLSettingsFoldout", MDLSettingsFoldout);
            VMTSettingsFoldout = GetBool("uVMTSettingsFoldout", VMTSettingsFoldout);
            #endregion

            #region Global
            RootPath = PlayerPrefs.GetString("uRootPath", RootPath);
            ModFolders = GetStringArray("uModFolders", ModFolders);
            DirPaks = GetString2DArray("uDirPaks", DirPaks);
            SaveAssetsToUnity = GetBool("uSaveAssetsToUnity", SaveAssetsToUnity);
            OutputAssetsFolder = PlayerPrefs.GetString("uOutputAssetsFolder", OutputAssetsFolder);
            ExportTextureAsPNG = GetBool("uExportTextureAsPNG", ExportTextureAsPNG);

            #region Lightmaps
            GenerateUV2StaticProps = GetBool("uGenerateUV2", GenerateUV2StaticProps);
            ParseLightmaps = GetBool("uParseLightmaps", ParseLightmaps);
            ModelsLightmapSize = PlayerPrefs.GetFloat("uModelsLightmapSize", ModelsLightmapSize);
            UV2HardAngleProps = PlayerPrefs.GetInt("uUV2HardAngleProps", UV2HardAngleProps);//88;
            UV2PackMarginTexSize = PlayerPrefs.GetFloat("uUV2PackMarginTexSize", UV2PackMarginTexSize);//256;
            UV2PackMarginProps = PlayerPrefs.GetInt("uUV2PackMarginProps", UV2PackMarginProps);//1;
            UV2AngleErrorProps = PlayerPrefs.GetInt("uUV2AngleErrorProps", UV2AngleErrorProps);//8;
            UV2AreaErrorProps = PlayerPrefs.GetInt("uUV2AreaErrorProps", UV2AreaErrorProps);//15;
            #endregion

            #region LOD
            EnableLODParsing = GetBool("uEnableLODParsing", EnableLODParsing);
            DetailMode = GetEnum("uDetailMode", DetailMode);//DetailMode.None;
            NegativeAddLODPrecent = PlayerPrefs.GetFloat("uNegativeAddLODPrecent", NegativeAddLODPrecent);
            ThresholdMaxSwitch = PlayerPrefs.GetFloat("uThresholdMaxSwitch", ThresholdMaxSwitch);
            SubstractLODPrecent = PlayerPrefs.GetFloat("uSubstractLODPrecent", SubstractLODPrecent);
            #endregion
            UnitScale = PlayerPrefs.GetFloat("uUnitScale", UnitScale);//0.0254f;
            LoadAnims = GetBool("uLoadAnims", LoadAnims);
            ClearDirectoryCache = GetBool("uClearDirectoryCache", ClearDirectoryCache);
            ClearModelCache = GetBool("uClearModelCache", ClearModelCache);
            ClearMaterialCache = GetBool("uClearMaterialCache", ClearMaterialCache);
            ClearTextureCache = GetBool("uClearTextureCache", ClearTextureCache);
            #endregion

            #region BSP
            MapName = PlayerPrefs.GetString("uMapName", MapName);
            ParseBSPPhysics = GetBool("uParseBSPPhysics", ParseBSPPhysics);
            ParseStaticPropScale = GetBool("uParseStaticPropScale", ParseStaticPropScale);
            Use3DSkybox = GetBool("uUse3DSkybox", Use3DSkybox);
            ParseDecals = GetBool("uParseDecals", ParseDecals);
            UseGammaLighting = GetBool("uUseGammaLighting", UseGammaLighting);
            UseLightmapsAsTextureShader = GetBool("uUseLightmapsAsTextureShader", UseLightmapsAsTextureShader);
            ParseLights = GetBool("uParseLights", ParseLights);
            UseWorldLights = GetBool("uUseWorldLights", UseWorldLights);
            QuadraticIntensityFixer = PlayerPrefs.GetFloat("uQuadraticIntensityFixer", QuadraticIntensityFixer);
            LightEnvironmentScale = PlayerPrefs.GetFloat("uLightEnvironmentScale", LightEnvironmentScale);
            CustomCascadedShadowResolution = PlayerPrefs.GetInt("uCustomCascadedShadowResolution", CustomCascadedShadowResolution);
            IgnoreShadowControl = GetBool("uIgnoreShadowControl", IgnoreShadowControl);
            UseDynamicLight = GetBool("uUseDynamicLight", UseDynamicLight);
            DebugEntities = GetBool("uDebugEntities", DebugEntities);
            DebugMaterials = GetBool("uDebugMaterials", DebugMaterials);
            #endregion

            #region MDL
            ModelPath = PlayerPrefs.GetString("uModelPath", ModelPath);
            SubModelPath = PlayerPrefs.GetString("uSubModelPath", SubModelPath);
            UseStaticPropFlag = GetBool("uUseStaticPropFlag", UseStaticPropFlag);
            UseHitboxesOnModel = GetBool("uUseHitboxesOnModel", UseHitboxesOnModel);
            DrawArmature = GetBool("uDrawArmature", DrawArmature);
            #endregion

            #region VMT
            //Additive
            AdditiveShader = PlayerPrefs.GetString("uAdditiveShader", AdditiveShader);
            //Detail
            DetailShader = PlayerPrefs.GetString("uDetailShader", DetailShader);
            DetailUnlitShader = PlayerPrefs.GetString("uDetailUnlitShader", DetailUnlitShader);
            DetailTranslucentShader = PlayerPrefs.GetString("uDetailTranslucentShader", DetailTranslucentShader);
            //Translucent
            TranslucentShader = PlayerPrefs.GetString("uTranslucentShader", TranslucentShader);
            TranslucentUnlitShader = PlayerPrefs.GetString("uTranslucentUnlitShader", TranslucentUnlitShader);
            //AlphaTest (Cutout)
            AlphaTestShader = PlayerPrefs.GetString("uAlphaTestShader", AlphaTestShader);
            //SelfIllum
            SelfIllumShader = PlayerPrefs.GetString("uSelfIllumShader", SelfIllumShader);
            //Global
            DefaultShader = PlayerPrefs.GetString("uDefaultShader", DefaultShader);
            LightmappedGenericShader = PlayerPrefs.GetString("uLightmappedGenericShader", LightmappedGenericShader);
            VertexLitGenericShader = PlayerPrefs.GetString("uVertexLitGenericShader", VertexLitGenericShader);
            WorldVertexTransitionShader = PlayerPrefs.GetString("uWorldVertexTransitionShader", WorldVertexTransitionShader);
            WorldTwoTextureBlend = PlayerPrefs.GetString("uWorldTwoTextureBlend", WorldTwoTextureBlend);
            UnlitGeneric = PlayerPrefs.GetString("uUnlitGeneric", UnlitGeneric);
            #endregion

            Debug.Log("Preset Loaded");
        }

        public static void SavePreset()
        {
            #region Editor Stuff
            PlayerPrefs.SetString("uPluginVersion", PluginVersion);
            SetBool("uAutoSavePreset", AutoSavePreset);
            SetBool("uSaveAfterResetPreset", SaveAfterResetPreset);
            SetBool("uGlobalSettingsFoldout", GlobalSettingsFoldout);
            SetBool("uModSettingsFoldout", ModSettingsFoldout);
            SetBool("uLodSettingsFoldout", LodSettingsFoldout);
            SetBool("uBSPSettingsFoldout", BSPSettingsFoldout);
            SetBool("uLightmapSettingsFoldout", LightmapSettingsFoldout);
            SetBool("uLightingSettingsFoldout", LightingSettingsFoldout);
            SetBool("uMDLSettingsFoldout", MDLSettingsFoldout);
            SetBool("uVMTSettingsFoldout", VMTSettingsFoldout);
            #endregion

            #region Global
            PlayerPrefs.SetString("uRootPath", RootPath);
            SetStringArray("uModFolders", ModFolders);
            SetString2DArray("uDirPaks", DirPaks);
            SetBool("uSaveAssetsToUnity", SaveAssetsToUnity);
            PlayerPrefs.SetString("uOutputAssetsFolder", OutputAssetsFolder);
            SetBool("uExportTextureAsPNG", ExportTextureAsPNG);

            #region Lightmaps
            SetBool("uGenerateUV2", GenerateUV2StaticProps);
            SetBool("uParseLightmaps", ParseLightmaps);
            PlayerPrefs.SetFloat("uModelsLightmapSize", ModelsLightmapSize);
            PlayerPrefs.SetInt("uUV2HardAngleProps", UV2HardAngleProps);
            PlayerPrefs.SetFloat("uUV2PackMarginTexSize", UV2PackMarginTexSize);
            PlayerPrefs.SetInt("uUV2PackMarginProps", UV2PackMarginProps);
            PlayerPrefs.SetInt("uUV2AngleErrorProps", UV2AngleErrorProps);
            PlayerPrefs.SetInt("uUV2AreaErrorProps", UV2AreaErrorProps);
            #endregion

            #region LOD
            SetBool("uEnableLODParsing", EnableLODParsing);
            SetEnum("uDetailMode", DetailMode);
            PlayerPrefs.SetFloat("uNegativeAddLODPrecent", NegativeAddLODPrecent);
            PlayerPrefs.SetFloat("uThresholdMaxSwitch", ThresholdMaxSwitch);
            PlayerPrefs.SetFloat("uSubstractLODPrecent", SubstractLODPrecent);
            #endregion
            PlayerPrefs.SetFloat("uUnitScale", UnitScale);
            SetBool("uLoadAnims", LoadAnims);
            SetBool("uClearDirectoryCache", ClearDirectoryCache);
            SetBool("uClearModelCache", ClearModelCache);
            SetBool("uClearMaterialCache", ClearMaterialCache);
            SetBool("uClearTextureCache", ClearTextureCache);
            #endregion

            #region BSP
            PlayerPrefs.SetString("uMapName", MapName);
            SetBool("uParseBSPPhysics", ParseBSPPhysics);
            SetBool("uParseStaticPropScale", ParseStaticPropScale);
            SetBool("uUse3DSkybox", Use3DSkybox);
            SetBool("uParseDecals", ParseDecals);
            SetBool("uUseGammaLighting", UseGammaLighting);
            SetBool("uUseLightmapsAsTextureShader", UseLightmapsAsTextureShader);
            SetBool("uParseLights", ParseLights);
            SetBool("uUseWorldLights", UseWorldLights);
            PlayerPrefs.SetFloat("uQuadraticIntensityFixer", QuadraticIntensityFixer);
            PlayerPrefs.SetFloat("uLightEnvironmentScale", LightEnvironmentScale);
            PlayerPrefs.SetInt("uCustomCascadedShadowResolution", CustomCascadedShadowResolution);
            SetBool("uIgnoreShadowControl", IgnoreShadowControl);
            SetBool("uUseDynamicLight", UseDynamicLight);
            SetBool("uDebugEntities", DebugEntities);
            SetBool("uDebugMaterials", DebugMaterials);
            #endregion

            #region MDL
            PlayerPrefs.SetString("uModelPath", ModelPath);
            PlayerPrefs.SetString("uSubModelPath", SubModelPath);
            SetBool("uUseStaticPropFlag", UseStaticPropFlag);
            SetBool("uUseHitboxesOnModel", UseHitboxesOnModel);
            SetBool("uDrawArmature", DrawArmature);
            #endregion

            #region VMT
            //Additive
            PlayerPrefs.SetString("uAdditiveShader", AdditiveShader);
            //Detail
            PlayerPrefs.SetString("uDetailShader", DetailShader);
            PlayerPrefs.SetString("uDetailUnlitShader", DetailUnlitShader);
            PlayerPrefs.SetString("uDetailTranslucentShader", DetailTranslucentShader);
            //Translucent
            PlayerPrefs.SetString("uTranslucentShader", TranslucentShader);
            PlayerPrefs.SetString("uTranslucentUnlitShader", TranslucentUnlitShader);
            //AlphaTest (Cutout)
            PlayerPrefs.SetString("uAlphaTestShader", AlphaTestShader);
            //SelfIllum
            PlayerPrefs.SetString("uSelfIllumShader", SelfIllumShader);
            //Global
            PlayerPrefs.SetString("uDefaultShader", DefaultShader);
            PlayerPrefs.SetString("uLightmappedGenericShader", LightmappedGenericShader);
            PlayerPrefs.SetString("uVertexLitGenericShader", VertexLitGenericShader);
            PlayerPrefs.SetString("uWorldVertexTransitionShader", WorldVertexTransitionShader);
            PlayerPrefs.SetString("uWorldTwoTextureBlend", WorldTwoTextureBlend);
            PlayerPrefs.SetString("uUnlitGeneric", UnlitGeneric);
            #endregion

            Debug.Log("Preset Saved");
        }

        public static void ResetPreset()
        {
            #region Editor Stuff
            /*GlobalSettingsFoldout = true;
            ModSettingsFoldout = true;
            LodSettingsFoldout = true;
            BSPSettingsFoldout = true;
            LightmapSettingsFoldout = true;
            LightingSettingsFoldout = true;
            MDLSettingsFoldout = true;
            VMTSettingsFoldout = false;*/
            #endregion

            #region Global
            RootPath = @"F:\Games\Source Engine\Counter-Strike Source";
            ModFolders = new[] { "cstrike", "hl2" };
            DirPaks = new String[][]
            {
                new String[] { "cstrike_pak_dir" },
                new String[] { "hl2_misc_dir", "hl2_textures_dir" }
            };
            SaveAssetsToUnity = false;
            OutputAssetsFolder = "uSource";
            ExportTextureAsPNG = true;
            #region Lightmap Settings
            GenerateUV2StaticProps = true;
            ParseLightmaps = false;
            ModelsLightmapSize = 1f;
            UV2HardAngleProps = 88;
            UV2PackMarginTexSize = 256;
            UV2PackMarginProps = 1;
            UV2AngleErrorProps = 8;
            UV2AreaErrorProps = 15;
            #endregion

            #region LOD
            EnableLODParsing = false;
            DetailMode = DetailMode.None;
            NegativeAddLODPrecent = 0.2f;
            ThresholdMaxSwitch = 0.1f;
            SubstractLODPrecent = 0.25f;
            #endregion
            UnitScale = 0.0254f;
            LoadAnims = false;
            ClearDirectoryCache = false;
            ClearModelCache = true;
            ClearMaterialCache = true;
            ClearTextureCache = true;
            #endregion

            #region BSP
            MapName = "test_angles";
            ParseBSPPhysics = false;
            ParseStaticPropScale = false;
            Use3DSkybox = true;
            ParseDecals = false;
            UseGammaLighting = true;
            UseLightmapsAsTextureShader = false;
            ParseLights = true;
            UseWorldLights = true;
            QuadraticIntensityFixer = 1;
            LightEnvironmentScale = 4;
            CustomCascadedShadowResolution = 8192;
            IgnoreShadowControl = false;
            UseDynamicLight = true;
            DebugEntities = true;
            DebugMaterials = true;
            #endregion

            #region MDL
            ModelPath = @"player/t_leet";
            SubModelPath = @"weapons/ct_arms";
            UseStaticPropFlag = false;
            UseHitboxesOnModel = false;
            DrawArmature = false;
            #endregion

            #region VMT
            //Additive
            AdditiveShader = "USource/AdditiveGeneric";
            //Detail
            DetailShader = "USource/DetailGeneric";
            DetailUnlitShader = "USource/UnlitGeneric";
            DetailTranslucentShader = "USource/TranslucentGeneric";
            //Translucent
            TranslucentShader = "USource/TranslucentGeneric";
            TranslucentUnlitShader = "Unlit/Transparent";
            //AlphaTest (Cutout)
            AlphaTestShader = "USource/CutoutGeneric";
            //SelfIllum
            SelfIllumShader = "USource/IllumGeneric";
            //Global
            DefaultShader = "Diffuse";
            LightmappedGenericShader = "Diffuse";
            VertexLitGenericShader = "Diffuse";
            WorldVertexTransitionShader = "USource/Lightmapped/WorldVertexTransition";
            WorldTwoTextureBlend = "USource/Lightmapped/WorldTwoTextureBlend";
            UnlitGeneric = "USource/UnlitGeneric";
            #endregion

            if(SaveAfterResetPreset)
                SavePreset();

            Debug.Log("Preset Reseted");
        }
#endif
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