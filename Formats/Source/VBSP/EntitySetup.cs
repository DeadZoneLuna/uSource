using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using uSource.Decals;
using uSource.Formats.Source.VTF;
using uSource.MathLib;
using uSource.Example;

namespace uSource.Formats.Source.VBSP
{
    //TODO:
    //Rework this & make universal
    public static class EntitySetup
    {
        public static void Configure(this Transform transform, List<String> Data)
        {
            //return;
            String Classname = Data[Data.FindIndex(n => n == "classname") + 1], Targetname = Data[Data.FindIndex(n => n == "targetname") + 1];
            transform.name = Classname;

            //ResourceManager.LoadModel("editor/axis_helper").SetParent(transform, false);

            Int32 OriginIndex = Data.FindIndex(n => n == "origin");
            if (OriginIndex != -1)
            {
                //Old but gold
                String[] origin = Data[OriginIndex + 1].Split(' ');

                while (origin.Length != 3)
                {
                    Int32 TempIndex = OriginIndex + 1;
                    origin = Data[Data.FindIndex(TempIndex, n => n == "origin") + 1].Split(' ');
                }
                //Old but gold

                transform.position = new Vector3(-origin[1].ToSingle(), origin[2].ToSingle(), origin[0].ToSingle()) * uLoader.UnitScale;
            }

            Int32 AnglesIndex = Data.FindIndex(n => n == "angles");
            if (AnglesIndex != -1)
            {
                Vector3 EulerAngles = Data[AnglesIndex + 1].ToVector3();

                EulerAngles = new Vector3(EulerAngles.x, -EulerAngles.y, -EulerAngles.z);

                if (Classname.StartsWith("light", StringComparison.Ordinal))
                    EulerAngles.x = -EulerAngles.x;

                Int32 PitchIndex = Data.FindIndex(n => n == "pitch");
                //Lights
                if (PitchIndex != -1)
                    EulerAngles.x = -Data[PitchIndex + 1].ToSingle();

                transform.eulerAngles = EulerAngles;
            }

            if (Classname.Contains("trigger"))
            {
                for (Int32 i = 0; i < transform.childCount; i++)
                {
                    GameObject Child = transform.GetChild(i).gameObject;
                    Child.SetActive(false);
                    Child.AddComponent<BoxCollider>().isTrigger = true;
                }
            }

#if UNITY_EDITOR
            if (Classname.Equals("env_sprite"))
            {
                //TODO: fix scale
                LensFlare lensFlare = transform.gameObject.AddComponent<LensFlare>();

                if (VBSPFile.GlowFlare == null)
                {
                    String path = UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("Glow t:Flare")[0]);
                    VBSPFile.GlowFlare = UnityEditor.AssetDatabase.LoadAssetAtPath<Flare>(path);
                }

                lensFlare.flare = VBSPFile.GlowFlare;
                lensFlare.brightness = Data[Data.FindIndex(n => n == "scale") + 1].ToSingle();
                lensFlare.fadeSpeed = Data[Data.FindIndex(n => n == "GlowProxySize") + 1].ToSingle();
                lensFlare.color = Data[Data.FindIndex(n => n == "rendercolor") + 1].ToColor32();

                return;
            }
#endif

            /*if (Classname.Equals("point_viewcontrol"))
            {
                transform.gameObject.AddComponent<point_viewcontrol>().Start();
            }*/

            //3D Skybox
            if (uLoader.Use3DSkybox && Classname.Equals("sky_camera"))
            {
                //Setup 3DSkybox
                Camera playerCamera = new GameObject("CameraPlayer").AddComponent<Camera>();
                Camera skyCamera = transform.gameObject.AddComponent<Camera>();

                CameraFly camFly = playerCamera.gameObject.AddComponent<CameraFly>();
                camFly.skyScale = Data[Data.FindIndex(n => n == "scale") + 1].ToSingle();
                camFly.offset3DSky = transform.position;
                camFly.skyCamera = skyCamera.transform;

                playerCamera.depth = -1;
                playerCamera.clearFlags = CameraClearFlags.Depth;

                skyCamera.depth = -2;
                skyCamera.clearFlags = CameraClearFlags.Skybox;
                //Setup 3DSkybox
                return;
            }
            //3D Skybox

            #region Counter-Strike entities test
            /*if (Classname.Equals("info_player_terrorist"))
            {
                //Placeholder model (can be removed if needed)
                ResourceManager.LoadModel("player/t_phoenix").SetParent(transform, false);
            }

            //Counter-Strike CT spawn point
            if (Classname.Equals("info_player_counterterrorist"))
            {
                //Placeholder model (can be removed if needed)
                ResourceManager.LoadModel("player/ct_urban").SetParent(transform, false);
            }

            //Default spawn point
            if (Classname.Equals("info_player_start"))
            {
                //Placeholder model (can be removed if needed)
                ResourceManager.LoadModel("editor/playerstart").SetParent(transform, false);
            }

            //weapon spawn point
            if (Classname.Contains("weapon_"))
            {
                //Placeholder model (can be removed if needed)
                ResourceManager.LoadModel("weapons/w_rif_ak47").SetParent(transform, false);
            }

            //hostage spawn point
            if (Classname.Equals("hostage_entity"))
            {
                String[] hostages = 
                {
                    "characters/hostage_01",
                    "characters/hostage_02",
                    "characters/hostage_03",
                    "characters/hostage_04"
                };

                ResourceManager.LoadModel(hostages[UnityEngine.Random.Range(0, hostages.Length)]).SetParent(transform, false);
            }*/
            #endregion

            Int32 RenderModeIndex = Data.FindIndex(n => n == "rendermode");
            if (RenderModeIndex != -1)
            {
                if (Data[RenderModeIndex + 1] == "10")
                {
                    for (Int32 i = 0; i < transform.childCount; i++)
                    {
                        GameObject Child = transform.GetChild(i).gameObject;
                        Child.GetComponent<Renderer>().enabled = false;
                    }
                }
            }

            if (Classname.Contains("prop_") || Classname.Contains("npc_"))// || Classname.Equals("asw_door"))
            {
                string ModelName = Data[Data.FindIndex(n => n == "model") + 1];

                if (!string.IsNullOrEmpty(ModelName))
                {
                    uResourceManager.LoadModel(ModelName, uLoader.LoadAnims, uLoader.UseHitboxesOnModel).SetParent(transform, false);
                    return;
                }

                return;
            }

            if (uLoader.ParseDecals && Classname.Equals("infodecal"))
            {
                String DecalName = Data[Data.FindIndex(n => n == "texture") + 1];
                VMTFile DecalMaterial = uResourceManager.LoadMaterial(DecalName);

                Single DecalScale = DecalMaterial.GetSingle("$decalscale");

                if (DecalScale <= 0)
                    DecalScale = 1f;

                Int32 DecalWidth = DecalMaterial.Material.mainTexture.width;   //X
                Int32 DecalHeight = DecalMaterial.Material.mainTexture.height; //Y
                Sprite DecalTexture = Sprite.Create((Texture2D)DecalMaterial.Material.mainTexture, new Rect(0, 0, DecalWidth, DecalHeight), Vector2.zero);

                Decal DecalBuilder = transform.gameObject.AddComponent<Decal>();

#if UNITY_EDITOR
                if (uLoader.DebugMaterials)
                    transform.gameObject.AddComponent<DebugMaterial>().Init(DecalMaterial);
#endif

                DecalBuilder.SetDirection();
                DecalBuilder.MaxAngle = 87.5f;
                DecalBuilder.Offset = 0.001f;
                DecalBuilder.Sprite = DecalTexture;
                DecalBuilder.Material = DecalMaterial.Material;
                DecalBuilder.Material.SetTextureScale("_MainTex", new Vector2(-1, 1));

                Single ScaleX = (DecalWidth * DecalScale) * uLoader.UnitScale;
                Single ScaleY = (DecalHeight * DecalScale) * uLoader.UnitScale;

                Single DepthSize = ScaleX;
                if (ScaleY < DepthSize)
                    DepthSize = ScaleY;

                transform.localScale = new Vector3(ScaleX, ScaleY, DepthSize);
                transform.position += new Vector3(0, 0, 0.001f);

#if !UNITY_EDITOR
                DecalBuilder.BuildAndSetDirty();
#endif
            }
        }
    }
}