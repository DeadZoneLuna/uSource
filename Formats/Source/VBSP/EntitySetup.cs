using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using uSource.Decals;
using uSource.Formats.Source.VTF;
using uSource.Example;

namespace uSource.Formats.Source.VBSP
{
    public static class EntitySetup
    {
        static Boolean hueta = true;

        public static void Configure(this Transform transform, List<String> Data)
        {
            //return;
            String Classname = Data[Data.FindIndex(n => n == "classname") + 1], Targetname = Data[Data.FindIndex(n => n == "targetname") + 1];
            transform.name = Classname;

            //ResourceManager.LoadModel("editor/axis_helper").SetParent(transform, false);

            if (Data.Contains("origin"))
            {
                //Old but gold
                String[] origin = Data[Data.FindIndex(n => n == "origin") + 1].Split(' ');

                while (origin.Length != 3)
                {
                    Int32 TempIndex = Data.FindIndex(n => n == "origin") + 1;
                    origin = Data[Data.FindIndex(TempIndex, n => n == "origin") + 1].Split(' ');
                }
                //Old but gold

                transform.position = new Vector3(-origin[1].ToSingle(), origin[2].ToSingle(), origin[0].ToSingle()) * uLoader.WorldScale;
            }

            if (Data.Contains("angles"))
            {
                //String[] Array = Data[Data.FindIndex(n => n == "angles") + 1].Split(' ');
                Vector3 EulerAngles = Converters.ToVector3(Data[Data.FindIndex(n => n == "angles") + 1]);//Vector3.zero;

                EulerAngles = new Vector3(EulerAngles.x, -EulerAngles.y, -EulerAngles.z);

                //Lights
                if (Data.Contains("pitch"))
                    EulerAngles.x = -Converters.ToSingle(Data[Data.FindIndex(n => n == "pitch") + 1]);//-Single.Parse(Data[Data.FindIndex(n => n == "pitch") + 1]);

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
                lensFlare.brightness = Converters.ToSingle(Data[Data.FindIndex(n => n == "scale") + 1]);
                lensFlare.fadeSpeed = Converters.ToSingle(Data[Data.FindIndex(n => n == "GlowProxySize") + 1]);
                //String[] rendercolor = Data[Data.FindIndex(n => n == "rendercolor") + 1].Split(' ');
                lensFlare.color = Converters.ToColor32(Data[Data.FindIndex(n => n == "rendercolor") + 1]);//new Color32(Byte.Parse(rendercolor[0]), Byte.Parse(rendercolor[1]), Byte.Parse(rendercolor[2]), 0);
            }
#endif

            /*if (Classname.Equals("point_viewcontrol"))
            {
                transform.gameObject.AddComponent<point_viewcontrol>().Start();
            }*/

            //3D Skybox
            if (uLoader.use3DSkybox && Classname.Equals("sky_camera"))
            {
                //Setup 3DSkybox
                Camera playerCamera = new GameObject("CameraPlayer").AddComponent<Camera>();
                Camera skyCamera = transform.gameObject.AddComponent<Camera>();

                CameraFly camFly = playerCamera.gameObject.AddComponent<CameraFly>();
                camFly.skyScale = Converters.ToSingle(Data[Data.FindIndex(n => n == "scale") + 1]);
                camFly.offset3DSky = transform.position;
                camFly.skyCamera = skyCamera.transform;

                playerCamera.depth = -1;
                playerCamera.clearFlags = CameraClearFlags.Depth;

                skyCamera.depth = -2;
                skyCamera.clearFlags = CameraClearFlags.Skybox;
                //Setup 3DSkybox
            }
            //3D Skybox

            //Lights

            //shadow_control used to change parameters of dynamic shadows on the map: direction, color, length.
            if (Classname.Equals("shadow_control"))
            {
                //It can be integrated for Unity?
                //String[] color = Data[Data.FindIndex(n => n == "color") + 1].Split(' ');
                //RenderSettings.subtractiveShadowColor = new Color32(Byte.Parse(color[0]), Byte.Parse(color[1]), Byte.Parse(color[2]), 255);
                RenderSettings.ambientGroundColor = Converters.ToColor(Data[Data.FindIndex(n => n == "color") + 1]);

                //Set light direction by shadow_control
                if (RenderSettings.sun != null)
                {
                    if ((RenderSettings.sun.transform.rotation == Quaternion.identity) && !uLoader.ignoreShadowControl)
                        RenderSettings.sun.transform.rotation = transform.rotation;

                    UpdateEquatorColor();
                }
            }

            //Lights parsing
            if (Classname.Contains("light") && hueta)
            {
                Color ambientLight;
                if (Classname.Equals("light_environment"))
                {
                    if (RenderSettings.sun != null)
                        return;

                    String _ambient = Data[Data.FindIndex(n => n == "_ambient") + 1];

                    ambientLight = Converters.ToColor(_ambient);

                    RenderSettings.ambientLight = ambientLight;
                    //RenderSettings.ambientSkyColor = ambientLight;

                    //Set light direction by shadow_control
                    if (VBSPFile.ShadowControl != null)
                    {
                        if(!uLoader.ignoreShadowControl && transform.rotation == Quaternion.identity)
                            transform.rotation = VBSPFile.ShadowControl.rotation;

                        UpdateEquatorColor();
                    }
                }

                Light Light = transform.gameObject.AddComponent<Light>();

                if (Classname.Equals("light_spot"))
                    Light.type = LightType.Spot;
                else if (Classname.Equals("light_environment"))
                {
                    RenderSettings.sun = Light;
                    Light.type = LightType.Directional;
                }

                Vector4 _lightColor = Converters.ToColorVec(Data[Data.FindIndex(n => n == "_light") + 1]);
                float lumens = _lightColor.w;
                float color_max = Mathf.Max(_lightColor[0], _lightColor[1], _lightColor[2], _lightColor[3]);
                lumens *= color_max / 255;

                if (Light.type == LightType.Directional)
                {
                    lumens /= 10;
                    Light.color = new Color32((byte)_lightColor.x, (byte)_lightColor.y, (byte)_lightColor.z, 255);
                }
                else
                    Light.color = _lightColor / color_max;

                float radius = 9.25f;

                if(Light.type == LightType.Point)
                {
                    Int32 distanceIndex = Data.FindIndex(n => n == "_distance") + 1;

                    if (distanceIndex != -1)
                    {
                        float _distance = Converters.ToInt32(Data[distanceIndex]);

                        if (_distance > 0)
                            radius = _distance / 16f;
                        else
                            radius = 16f;
                    }
                }

                if (Light.type == LightType.Spot)
                {
                    //float constant_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_constant_attn") + 1]);
                    //float linear_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_linear_attn") + 1]);
                    //float quadratic_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_quadratic_attn") + 1]);
                    float inner_cone = Converters.ToSingle(Data[Data.FindIndex(n => n == "_cone2") + 1], 60);
                    float cone = Converters.ToSingle(Data[Data.FindIndex(n => n == "_cone") + 1]) * 2;
                    radius = (10 - inner_cone / cone);
                    Light.spotAngle = Mathf.Clamp(cone, 0, 179);
                }

                Light.range = radius;
                Light.intensity = (lumens * 0.01905f) / 2;

#if UNITY_EDITOR
                Light.lightmapBakeType = LightmapBakeType.Baked;//ConfigLoader.useDynamicLight ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
#endif
                if (uLoader.useDynamicLight)
                {
                    Light.shadows = LightShadows.Soft;
                    if (Light.type == LightType.Directional)
                    {
                        Light.shadowBias = 0.1f;
                        Light.shadowNormalBias = 0;
                    }
                    else
                        Light.shadowBias = 0.01f;
                }
            }

            //Lights

            #region Counter-Strike T spawn point
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
            if (Data.Contains("rendermode"))
            {
                if (Data[Data.FindIndex(n => n == "rendermode") + 1] == "10")
                {
                    for (Int32 i = 0; i < transform.childCount; i++)
                    {
                        GameObject Child = transform.GetChild(i).gameObject;
                        Child.GetComponent<Renderer>().enabled = false;
                    }
                }
            }

            if (Classname.Contains("prop_") || Classname.Contains("npc_"))
            {
                string ModelName = Data[Data.FindIndex(n => n == "model") + 1];

                if (!string.IsNullOrEmpty(ModelName))
                    uResourceManager.LoadModel(ModelName, uLoader.LoadAnims, uLoader.useHitboxesOnModel).SetParent(transform, false);
            }

            if (uLoader.useInfoDecals && Classname.Equals("infodecal"))
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
                DecalBuilder.SetDirection();
                DecalBuilder.MaxAngle = 87.5f;
                DecalBuilder.Offset = 0.001f;
                DecalBuilder.Sprite = DecalTexture;
                DecalBuilder.Material = DecalMaterial.Material;
                DecalBuilder.Material.SetTextureScale("_MainTex", new Vector2(-1, 1));

                Single ScaleX = (DecalWidth * DecalScale) * uLoader.WorldScale;
                Single ScaleY = (DecalHeight * DecalScale) * uLoader.WorldScale;

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

        static void UpdateEquatorColor()
        {
            Color a = RenderSettings.ambientSkyColor;
            Color b = RenderSettings.ambientGroundColor;

            RenderSettings.ambientEquatorColor = new Color(CaclAvg(a.r, b.r), CaclAvg(a.g, b.g), CaclAvg(a.b, b.b), CaclAvg(a.a, b.a));
        }

        static float CaclAvg(float first, float second)
        {
            return (first + second) / 2;
        }
    }
}