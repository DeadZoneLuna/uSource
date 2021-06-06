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
        public static Int32 TransparentQueue = 3001;

        public KeyValues.Entry this[String shader] => KeyValues[shader];
        public KeyValues KeyValues;
        public String ShaderType;
        public Material Material;
        public VMTFile Include;
        public Material DefaultMaterial;

        //TODO
        public Boolean HasAnimation;

        public void SetupAnimations(ref AnimatedTexture ControlScript)
        {
            ControlScript.AnimatedTextureFramerate = GetSingle("animatedtextureframerate");
            //ControlScript.Frames = VTFFile.Frames;
        }
        //TODO

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
                KeyValues = KeyValues.FromStream(stream);
            }
            catch (Exception ex)
            {
                MakeDefaultMaterial();
                Debug.LogError(ex);
                Material = DefaultMaterial;
            }

            ShaderType = KeyValues.Keys.First();

            //If shader is null, return
            if (string.IsNullOrEmpty(ShaderType))
            {
                MakeDefaultMaterial();
                return;
            }
        }

        public void CreateMaterial()
        {
            if (KeyValues == null)
            {
                MakeDefaultMaterial();
                return;
            }

            #region Patch "Shader"
            if (ContainsParma("replace"))
                this[ShaderType].MergeFrom(this[ShaderType]["replace"], true);

            if (ContainsParma("include"))
            {
                Include = uResourceManager.LoadMaterial(GetParma("include"));
                this[ShaderType].MergeFrom(Include[Include.ShaderType], false);
            }

            if (ContainsParma("insert"))
                this[ShaderType].MergeFrom(this[ShaderType]["insert"], false);
            #endregion

            if (ContainsParma("$fallbackmaterial"))
            {
                Include = uResourceManager.LoadMaterial(GetParma("$fallbackmaterial"));
                this[ShaderType].MergeFrom(Include[Include.ShaderType], true);
            }

            //TODO
            //HasAnimation = ContainsParma("animatedtexture") && GetParma("animatedtexturevar") == "$basetexture";

#if UNITY_EDITOR
            //Try load asset from project (if exist)
            if (uLoader.SaveAssetsToUnity)
            {
                Material = uResourceManager.LoadAsset<Material>(FileName, uResourceManager.MaterialsExtension[0], ".mat");
                if (Material != null)
                    return;
            }
#endif

            String TextureName;
            String PropertyName;
            Texture2D BaseTexture = null;
            Boolean HasAlpha = false;
            if (ContainsParma("$basetexture") || ContainsParma("$envmapmask"))
            {
                TextureName = GetParma("$basetexture");
                if (TextureName == null || TextureName.Length == 0)
                    TextureName = GetParma("$envmapmask");

                BaseTexture = uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, "_MainTex" } })[0, 0];

                //To avoid "NullReferenceException"
                if(BaseTexture != null)
                    HasAlpha = BaseTexture.alphaIsTransparency;
            }

            Material = new Material(GetShader(Include == null ? ShaderType : Include.ShaderType, HasAlpha));
            Material.name = FileName;
            Material.color = GetColor();

            if(BaseTexture != null)
                Material.mainTexture = BaseTexture;

            if (ContainsParma("$basetexture2"))
            {
                TextureName = GetParma("$basetexture2");
                PropertyName = "_SecondTex";
                if (Material.HasProperty(PropertyName))
                    Material.SetTexture(PropertyName, uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, PropertyName } })[0, 0]);
            }

            if (ContainsParma("$envmapmask"))
            {
                PropertyName = "_AlphaMask";
                if (Material.HasProperty(PropertyName))
                {
                    if (BaseTexture != null && !HasAlpha)
                    {
                        TextureName = GetParma("$envmapmask");
                        Material.SetInt(PropertyName, 1);
                        Material.SetTexture("_MaskTex", uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, "_MaskTex" } })[0, 0]);
                    }
                }
            }

            //Transparent
            if (IsTrue("$translucent"))
                Material.renderQueue = TransparentQueue++;

            //Cutout Transparent
            if (IsTrue("$alphatest"))
                Material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

            //Cull mode state
            //0 - Off (Double Sided)
            //1 - Front (Front Sided)
            //2 - Back (Back Sided)
            if (IsTrue("$nocull"))
            {
                PropertyName = "_Cull";
                if (Material.HasProperty(PropertyName))
                    Material.SetInt(PropertyName, 0);
            }

            // Detail texture blend
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

            //TODO?
            //if (ContainsParma("$surfaceprop"))
            //    Material.name = Items["$surfaceprop"];
        }

        public Shader GetShader(String shader, Boolean HasAlpha = false)
        {
            if (!string.IsNullOrEmpty(shader))
            {
                if (IsTrue("$additive"))
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

                if (IsTrue("$translucent") && HasAlpha)
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

                if (IsTrue("$selfillum") && (HasAlpha || ContainsParma("$envmapmask")))
                {
                    return Shader.Find(uLoader.SelfIllumShader);
                }

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
            }

            //Diffuse
            return Shader.Find(uLoader.DefaultShader);
        }

        public Boolean ContainsParma(String parma)
        {
            return this[ShaderType].ContainsKey(parma);
        }

        public String GetParma(String parma)
        {
            //if (string.IsNullOrEmpty(_Shader))
            //    throw new Exception("SHADER MISSING!");

            return this[ShaderType][parma];//float.Parse(Items[Data]);
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
                MaterialColor = this[ShaderType]["$color"];//.Replace(".", "").Trim('[', ']', '{', '}').Trim().Split(' ');
            }

            if (ContainsParma("$alpha"))
                MaterialColor.a = (byte)(255 * (float)this[ShaderType]["$alpha"]);

            return MaterialColor;
        }

        public bool IsTrue(string Input)
        {
            if (ContainsParma(Input))
                return this[ShaderType][Input] == true;

            return false;
        }
    }
}