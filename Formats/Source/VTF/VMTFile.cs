using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace uSource.Formats.Source.VTF
{
    public class VMTFile
    {
        public String FileName = "";
        public static int TransparentQueue = 3001;

        public KeyValues.Entry this[string shader] => _keyValues[shader];
        public KeyValues _keyValues;//static Dictionary<String, String> Items;
        public String _Shader;
        public Material Material;
        public VMTFile includeVmt;
        public Material DefaultMaterial;

        public Boolean HasAnimation;

        public void SetupAnimations(ref AnimatedTexture ControlScript)
        {
            ControlScript.AnimatedTextureFramerate = GetSingle("animatedtextureframerate");
            //ControlScript.Frames = VTFRead.Frames;
        }

        void MakeDefaultMaterial()
        {
            if (DefaultMaterial == null)
            {
#if UNITY_EDITOR
                //Try load asset from project (if exist)
                if (uLoader.SaveAssetsToUnity)
                {
                    Material = DefaultMaterial = uResourceManager.LoadAsset<Material>(FileName, uResourceManager.MaterialsExtension[0], ".mat");
                    if (Material != null)
                        return;
                }
#endif

                Material = DefaultMaterial = new Material(Shader.Find("Diffuse"));
                Material.name = FileName;
#if UNITY_EDITOR
                if (uLoader.SaveAssetsToUnity)
                {
                    uResourceManager.SaveAsset(Material, FileName, uResourceManager.MaterialsExtension[0], ".mat");
                }
#endif
            }
        }

        public VMTFile(Stream stream, String FileName = "")
        {
            this.FileName = FileName;
            if (stream == null)
            {
                MakeDefaultMaterial();
                return;
            }

            HasAnimation = false;

            try
            {
                _keyValues = KeyValues.FromStream(stream);
            }
            catch (Exception ex)
            {
                MakeDefaultMaterial();

                Debug.LogError(ex);
                Material = DefaultMaterial;
            }

            _Shader = _keyValues.Keys.First();

            //If shader is null, return
            if (string.IsNullOrEmpty(_Shader))
            {
                MakeDefaultMaterial();
                return;
            }
        }

        public void CreateMaterial()
        {
            if (_keyValues == null)
            {
                MakeDefaultMaterial();
                return;
            }

            //VMTRead includeVmt;
            if (ContainsParma("include"))
            {
                includeVmt = uResourceManager.LoadMaterial(GetParma("include"));
                this[_Shader].MergeFrom(includeVmt[includeVmt._Shader], true);
                //_Shader = includeVmt._Shader;
            }

            if (ContainsParma("$fallbackmaterial"))
            {
                includeVmt = uResourceManager.LoadMaterial(GetParma("$fallbackmaterial"));
                this[_Shader].MergeFrom(includeVmt[includeVmt._Shader], true);
                //Material = ResourceManager.LoadMaterial(Items["$fallbackmaterial"]).Material;
            }

            HasAnimation = ContainsParma("animatedtexture") && GetParma("animatedtexturevar") == "$basetexture";

#if UNITY_EDITOR
            //Try load asset from project (if exist)
            if (uLoader.SaveAssetsToUnity)
            {
                Material = uResourceManager.LoadAsset<Material>(FileName, uResourceManager.MaterialsExtension[0], ".mat");
                if (Material != null)
                    return;
            }
#endif

            Material = new Material(GetShader(includeVmt == null ? _Shader : includeVmt._Shader));
            Material.name = FileName;
            Material.color = GetColor();

            String TextureName;
            String PropertyName;
            Texture2D BaseTexture = null;
            if (ContainsParma("$basetexture"))
            {
                TextureName = GetParma("$basetexture");
                BaseTexture = uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, "_MainTex" } })[0, 0];
                Material.mainTexture = BaseTexture;
            }

            //if (Material.mainTexture == null && ContainsParma("$bumpmap"))
            //    Material.mainTexture = ResourceManager.LoadTexture(GetParma("$bumpmap"))[0];

            if (BaseTexture == null && ContainsParma("$envmapmask"))
            {
                TextureName = GetParma("$envmapmask");
                BaseTexture = uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, "_MainTex" } })[0, 0];
                Material.mainTexture = BaseTexture;
            }

            if (ContainsParma("$basetexture2"))
            {
                TextureName = GetParma("$basetexture2");
                PropertyName = "_SecondTex";
                if (Material.HasProperty(PropertyName))
                    Material.SetTexture(PropertyName, uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, PropertyName } })[0, 0]);
            }

            Boolean HasMask = ContainsParma("$envmapmask");
            if (HasMask)
            {
                PropertyName = "_AlphaMask";
                if (Material.HasProperty(PropertyName))
                {
                    if (BaseTexture != null && !BaseTexture.alphaIsTransparency)
                    {
                        TextureName = GetParma("$envmapmask");
                        Material.SetInt(PropertyName, 1);
                        Material.SetTexture("_MaskTex", uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, "_MaskTex" } })[0, 0]);
                    }
                }
            }

            //Base props

            //_IsTranslucent
            if (IsTrue("$translucent"))
            {
                /*if (Material.HasProperty("_IsTranslucent"))
                {
                    Material.SetInt("_IsTranslucent", GetInteger("$translucent"));
                    Material.SetInt("_Cull", 0);
                    Material.SetInt("_ZState", 0);
                }*/

                Material.renderQueue = TransparentQueue++;
            }

            //_AlphaTest
            if (IsTrue("$alphatest"))
            {
                //if (Material.HasProperty("_AlphaTest"))
                //    Material.SetInt("_AlphaTest", GetInteger("$alphatest"));

                Material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            //$nocull
            if (IsTrue("$nocull"))
            {
                if(Material.HasProperty("_Cull"))
                    Material.SetInt("_Cull", IsTrue("$nocull") ? 0 : 2);
            }

            if (ContainsParma("$detail"))
            {
                PropertyName = "_Detail";
                if (Material.HasProperty(PropertyName))
                {
                    TextureName = GetParma("$detail");
                    Material.SetTexture(PropertyName, uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, PropertyName } })[0, 0]);

                    if (ContainsParma("$detailscale"))
                    {
                        float detailScale = GetSingle("$detailscale");
                        Material.SetTextureScale("_Detail", new Vector2(detailScale, detailScale));
                    }

                    if (Material.HasProperty("_DetailFactor"))
                    {
                        if (ContainsParma("$detailblendfactor"))
                        {
                            float blendFactor = GetSingle("$detailblendfactor") / 2;
                            Material.SetFloat("_DetailFactor", blendFactor);
                        }
                        else
                            Material.SetFloat("_DetailFactor", 0.5f);

                        if (ContainsParma("$detailblendmode"))
                        {
                            int blendMode = GetInteger("$detailblendmode");
                            Material.SetInt("_DetailBlendMode", blendMode);
                        }
                    }
                }
            }

            //Base props

            /*if (ContainsParma("$bumpmap"))
            {
                if (Material.HasProperty("_BumpMap"))
                    Material.SetTexture("_BumpMap", uResourceManager.LoadTexture(GetParma("$bumpmap"))[0, 0]);
            }*/

            //if (ContainsParma("$surfaceprop"))
            //    Material.name = Items["$surfaceprop"];
        }

        public Shader GetShader(String shader)
        {
            if (!string.IsNullOrEmpty(shader))
            {
                if (ContainsParma("$additive"))
                    return Shader.Find(uLoader.AdditiveShader);

                if (ContainsParma("$detail"))
                {
                    if (shader.Equals("unlitgeneric"))
                        return Shader.Find(uLoader.DetailUnlitShader);

                    if (shader.Equals("worldtwotextureblend"))
                        return Shader.Find(uLoader.WorldTwoTextureBlend);

                    if (IsTrue("$translucent"))
                        return Shader.Find(uLoader.DetailTranslucentShader);

                    return Shader.Find(uLoader.DetailShader);
                }

                if (IsTrue("$translucent"))
                {
                    if(shader.Equals("unlitgeneric"))
                        return Shader.Find(uLoader.TranslucentUnlitShader);

                    return Shader.Find(uLoader.TranslucentShader);
                }

                if (IsTrue("$alphatest"))
                {
                    //if (shader.Equals("unlitgeneric"))
                    //    return Shader.Find("Unlit/Transparent Cutout");

                    return Shader.Find(uLoader.AlphaTestShader);//"Transparent/Cutout/Diffuse"
                }

                if (ContainsParma("$selfillum"))
                    return Shader.Find(uLoader.SelfIllumShader);

                //World / Generic
                if (shader.Equals("lightmappedgeneric"))
                    return Shader.Find(uLoader.LightmappedGenericShader);//USource/Lightmapped/Generic

                if (shader.Equals("vertexlitgeneric"))
                    return Shader.Find(uLoader.VertexLitGenericShader);

                if (shader.Equals("worldvertextransition"))
                {
                    if (ContainsParma("$basetexture2"))
                        return Shader.Find(uLoader.WorldVertexTransitionShader);

                    return Shader.Find(uLoader.DefaultShader);
                }

                if (shader.Equals("WorldTwoTextureBlend"))
                    return Shader.Find(uLoader.WorldTwoTextureBlend);

                if (shader.Equals("unlitgeneric"))
                    return Shader.Find(uLoader.UnlitGeneric);

                //if(shader.Equals("worldtwotextureblend"))
                //    return Shader.Find("Diffuse");
            }

            //Diffuse
            return Shader.Find(uLoader.DefaultShader);
        }

        public Boolean ContainsParma(String parma)
        {
            return this[_Shader].ContainsKey(parma);
        }

        public String GetParma(String parma)
        {
            //if (string.IsNullOrEmpty(_Shader))
            //    throw new Exception("SHADER MISSING!");

            return this[_Shader][parma];//float.Parse(Items[Data]);
        }

        public Single GetSingle(String parma)
        {
            return Converters.ToSingle(GetParma(parma));//float.Parse(Items[Data]);
        }

        public Int32 GetInteger(String parma)
        {
            return Converters.ToInt32(GetParma(parma));//float.Parse(Items[Data]);
        }

        public Color32 GetColor()
        {
            Color32 MaterialColor = new Color32(255, 255, 255, 255);

            if (ContainsParma("$color"))
            {
                MaterialColor = this[_Shader]["$color"];//.Replace(".", "").Trim('[', ']', '{', '}').Trim().Split(' ');
            }

            if (ContainsParma("$alpha"))
                MaterialColor.a = (byte)(255 * (float)this[_Shader]["$alpha"]);

            return MaterialColor;
        }

        public bool IsTrue(string Input)
        {
            if (ContainsParma(Input))
                return this[_Shader][Input] == true;

            return false;
        }
    }
}