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
                Vector3 EulerAngles = Converters.ToVector3(Data[AnglesIndex + 1]);

                EulerAngles = new Vector3(EulerAngles.x, -EulerAngles.y, -EulerAngles.z);

                if (Classname.StartsWith("light", StringComparison.Ordinal))
                    EulerAngles.x = -EulerAngles.x;

                Int32 PitchIndex = Data.FindIndex(n => n == "pitch");
                //Lights
                if (PitchIndex != -1)
                    EulerAngles.x = -Converters.ToSingle(Data[PitchIndex + 1]);

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
                lensFlare.color = Converters.ToColor32(Data[Data.FindIndex(n => n == "rendercolor") + 1]);

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
                camFly.skyScale = Converters.ToSingle(Data[Data.FindIndex(n => n == "scale") + 1]);
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

            //Lights

            //shadow_control used to change parameters of dynamic shadows on the map: direction, color, length.
            if (Classname.Equals("shadow_control"))
            {
                //It can be integrated for Unity?
                //String[] color = Data[Data.FindIndex(n => n == "color") + 1].Split(' ');
                //RenderSettings.subtractiveShadowColor = new Color32(Byte.Parse(color[0]), Byte.Parse(color[1]), Byte.Parse(color[2]), 255);
                RenderSettings.ambientGroundColor = Converters.ToColor(Data[Data.FindIndex(n => n == "color") + 1]);

                //Set light direction by shadow_control
                if (VBSPFile.LightEnvironment != null)
                {
                    if ((VBSPFile.LightEnvironment.rotation == Quaternion.identity) && !uLoader.IgnoreShadowControl)
                        VBSPFile.LightEnvironment.rotation = transform.rotation;

                    UpdateEquatorColor();
                }

                return;
            }

            //Lights parsing
            if (!uLoader.UseWorldLights && Classname.Contains("light") && !Classname.StartsWith("point"))
            {
                Color ambientLight;
                if (Classname.Equals("light_environment"))
                {
                    if (VBSPFile.LightEnvironment != null)
                        return;

                    VBSPFile.LightEnvironment = transform;

                    //TODO: Correct parse ambient color
                    String _ambient = Data[Data.FindIndex(n => n == "_ambient") + 1];
                    ambientLight = Converters.ToColor(_ambient);
                    RenderSettings.ambientLight = ambientLight;

                    //Set light direction by shadow_control
                    if (VBSPFile.ShadowControl != null)
                    {
                        if (!uLoader.IgnoreShadowControl && transform.rotation == Quaternion.identity)
                            transform.rotation = VBSPFile.ShadowControl.rotation;

                        UpdateEquatorColor();
                    }
                }

                if (uLoader.ParseLights)
                {
                    Light Light = transform.gameObject.AddComponent<Light>();

                    if (Classname.Equals("light_spot"))
                        Light.type = LightType.Spot;
                    else if (Classname.Equals("light_environment"))
                        Light.type = LightType.Directional;

                    Vector4 _lightColor = Converters.ToColorVec(Data[Data.FindIndex(n => n == "_light") + 1]);
                    Single intensity = _lightColor.w;
                    Single m_Attenuation0 = 0;
                    Single m_Attenuation1 = 0;
                    Single m_Attenuation2 = 0;

                    Light.color = new Color(_lightColor.x / 255, _lightColor.y / 255, _lightColor.z / 255, 255);

                    Single LightRadius = 256;

                    if (Light.type == LightType.Spot || Light.type == LightType.Point)
                    {
                        if (Light.type == LightType.Spot)
                        {
                            //Single inner_cone = Converters.ToSingle(Data[Data.FindIndex(n => n == "_cone2") + 1]);
                            Single cone = Converters.ToSingle(Data[Data.FindIndex(n => n == "_cone") + 1]) * 2;
                            //radius -= inner_cone / cone;
                            Light.spotAngle = Mathf.Clamp(cone, 0, 179);
                        }

                        Single _distance = Converters.ToInt32(Data[Data.FindIndex(n => n == "_distance") + 1]);

                        if (_distance != 0)
                        {
                            LightRadius = _distance;
                            intensity *= 1.5f;
                        }
                        else
                        {
                            Single _fifty_percent_distance = Converters.ToSingle(Data[Data.FindIndex(n => n == "_fifty_percent_distance") + 1]);
                            Boolean isFifty = _fifty_percent_distance != 0;

                            if (isFifty)
                            {
                                //New light style
                                Single _zero_percent_distance = Converters.ToSingle(Data[Data.FindIndex(n => n == "_zero_percent_distance") + 1]);

                                if (_zero_percent_distance < _fifty_percent_distance)
                                {
                                    // !!warning in lib code???!!!
                                    Debug.LogWarningFormat("light has _fifty_percent_distance of {0} but no zero_percent_distance", _fifty_percent_distance);
                                    _zero_percent_distance = 2.0f * _fifty_percent_distance;
                                }

                                Single a = 0, b = 1, c = 0;
                                if (!MathLibrary.SolveInverseQuadraticMonotonic(0, 1.0f, _fifty_percent_distance, 2.0f, _zero_percent_distance, 256.0f, ref a, ref b, ref c))
                                {
                                    Debug.LogWarningFormat("can't solve quadratic for light {0} {1}", _fifty_percent_distance, _zero_percent_distance);
                                }

                                Single v50 = c + _fifty_percent_distance * (b + _fifty_percent_distance * a);
                                Single scale = 2.0f / v50;
                                a *= scale;
                                b *= scale;
                                c *= scale;
                                m_Attenuation2 = a;
                                m_Attenuation1 = b;
                                m_Attenuation0 = c;
                            }
                            else
                            {
                                //Old light style
                                Single constant_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_constant_attn") + 1]);
                                Single linear_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_linear_attn") + 1]);
                                Single quadratic_attn = Converters.ToSingle(Data[Data.FindIndex(n => n == "_quadratic_attn") + 1]);

                                // old-style manually typed quadrtiac coefficients
                                if (quadratic_attn < 0.001)
                                    quadratic_attn = 0;

                                if (linear_attn < 0.001)
                                    linear_attn = 0;

                                if (constant_attn < 0.001)
                                    constant_attn = 0;

                                if ((constant_attn < 0.001) &&
                                     (linear_attn < 0.001) &&
                                     (quadratic_attn < 0.001))
                                    constant_attn = 1;

                                m_Attenuation2 = quadratic_attn;
                                m_Attenuation1 = linear_attn;
                                m_Attenuation0 = constant_attn;
                            }

                            // FALLBACK: older lights use this
                            if (m_Attenuation2 == 0.0f)
                            {
                                if (m_Attenuation1 == 0.0f)
                                {
                                    // Infinite, but we're not going to draw it as such
                                    LightRadius = 2000;
                                }
                                else
                                {
                                    LightRadius = (intensity / 0.03f - m_Attenuation0) / m_Attenuation1;
                                }
                            }
                            else
                            {
                                Single a = m_Attenuation2;
                                Single b = m_Attenuation1;
                                Single c = m_Attenuation0 - intensity / 0.03f;
                                Single discrim = b * b - 4 * a * c;
                                if (discrim < 0.0f)
                                    // Infinite, but we're not going to draw it as such
                                    LightRadius = 2000;
                                else
                                {
                                    LightRadius = (-b + Mathf.Sqrt(discrim)) / (2.0f * a);
                                    if (LightRadius < 0)
                                        LightRadius = 0;

                                    //DeadZoneLuna
                                    //TODO: Find the best way to fix that
                                    //DeadZoneLuna
                                    if (isFifty)
                                    {
                                        //TODO: WHY?
                                        LightRadius /= 10;
                                    }
                                    else
                                    {
                                        //TODO: Not enough intensity?
                                        LightRadius *= 10;
                                    }
                                }
                            }
                        }

                        Light.range = (LightRadius * uLoader.UnitScale);
                    }

                    Light.intensity = (intensity / 255f) * 1.75f;

#if UNITY_EDITOR
                    Light.lightmapBakeType = LightmapBakeType.Baked;
#endif
                    if (uLoader.UseDynamicLight)
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

                return;
            }

            //Lights

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

        static void UpdateEquatorColor()
        {
            Color a = RenderSettings.ambientSkyColor;
            Color b = RenderSettings.ambientGroundColor;

            RenderSettings.ambientEquatorColor = new Color(CaclAvg(a.r, b.r), CaclAvg(a.g, b.g), CaclAvg(a.b, b.b), CaclAvg(a.a, b.a));
        }

        static Single CaclAvg(Single first, Single second)
        {
            return (first + second) / 2;
        }
    }
}