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

        //Other
        public VMTFile Include;
        public String ShaderType;
        public String SurfaceProp;
        public Material Material;
        public Material DefaultMaterial;
        public static Int32 TransparentQueue = 3001;

        #region KeyValues
        public KeyValues.Entry this[String shader] => KeyValues[shader];
        public KeyValues KeyValues;

        public Boolean ContainsParam(String param)
        {
            return this[ShaderType].ContainsKey(param);
        }

        public String GetParam(String param)
        {
            return this[ShaderType][param];
        }

        public Single GetSingle(String param)
        {
            return GetParam(param).ToSingle();
        }

        public Vector3 GetVector3(String param)
        {
            param = GetParam(param);

            Int32 BracketOpenIndex = param.IndexOfAny(new Char[] { '[', '{' });
            if (BracketOpenIndex != -1)
                param = param.Remove(BracketOpenIndex, 1);

            Int32 BracketCloseIndex = param.IndexOfAny(new Char[] { ']', '}' });
            if (BracketCloseIndex != -1)
                param = param.Remove(BracketCloseIndex, 1);

            return param.ToVector3();
        }

        public Vector2 GetVector2(String param)
        {
            Vector2 Result;

            Vector3 TempVector = GetVector3(param);
            Result.x = TempVector.x;
            Result.y = TempVector.y;
            if (Result.y == 0f)
                Result.y = Result.x;

            return Result;
        }

        public Int32 GetInteger(String param)
        {
            return GetParam(param).ToInt32();
        }

        public Color32 GetColor()
        {
            Color32 MaterialColor = new Color32(255, 255, 255, 255);

            if (ContainsParam("$color"))
                MaterialColor = this[ShaderType]["$color"];

            if (ContainsParam("$alpha"))
                MaterialColor.a = (Byte)(255 * (Single)this[ShaderType]["$alpha"]);

            return MaterialColor;
        }

        public bool IsTrue(String Input, Boolean ContainsCheck = true)
        {
            if (ContainsCheck && ContainsParam(Input))
                return this[ShaderType][Input] == true;

            return false;
        }
        #endregion

        //TODO
        public Boolean HasAnimation = false;

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

            KeyValues = KeyValues.FromStream(stream);

            if (KeyValues.Keys.Count() > 0 && KeyValues != null)
            {
                ShaderType = KeyValues.Keys.First();

                //If shader is null, return
                if (string.IsNullOrEmpty(ShaderType))
                    throw new FileLoadException(String.Format("Shader type is missing in material, skip parse", FileName));
            }
            else
                throw new FileLoadException(String.Format("is missing any KeyValues data, skip parse", FileName));
        }

        public void CreateMaterial()
        {
            #region Patch "Shader"
            if (ContainsParam("replace"))
                this[ShaderType].MergeFrom(this[ShaderType]["replace"], true);

            if (ContainsParam("include"))
            {
                Include = uResourceManager.LoadMaterial(GetParam("include"));
                this[ShaderType].MergeFrom(Include[Include.ShaderType], false);
            }

            if (ContainsParam("insert"))
                this[ShaderType].MergeFrom(this[ShaderType]["insert"], false);
            #endregion

            if (ContainsParam("$fallbackmaterial"))
            {
                Include = uResourceManager.LoadMaterial(GetParam("$fallbackmaterial"));
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
            if (ContainsParam("$basetexture") || ContainsParam("$envmapmask"))
            {
                TextureName = GetParam("$basetexture");
                if (TextureName == null || TextureName.Length == 0)
                    TextureName = GetParam("$envmapmask");

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

            if (ContainsParam("$basetexture2"))
            {
                TextureName = GetParam("$basetexture2");
                PropertyName = "_SecondTex";
                if (Material.HasProperty(PropertyName))
                    Material.SetTexture(PropertyName, uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, PropertyName } })[0, 0]);
            }

            if (ContainsParam("$envmapmask"))
            {
                PropertyName = "_AlphaMask";
                if (Material.HasProperty(PropertyName))
                {
                    if (BaseTexture != null && !HasAlpha)
                    {
                        TextureName = GetParam("$envmapmask");
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
            if (ContainsParam("$detail"))
            {
                PropertyName = "_Detail";
                if (Material.HasProperty(PropertyName))
                {
                    TextureName = GetParam("$detail");
                    Material.SetTexture(PropertyName, uResourceManager.LoadTexture(TextureName, ExportData: new String[,] { { FileName, PropertyName } })[0, 0]);

                    if (ContainsParam("$detailscale"))
                        Material.SetTextureScale("_Detail", GetVector2("$detailscale"));

                    if (Material.HasProperty("_DetailFactor"))
                    {
                        if (ContainsParam("$detailblendfactor"))
                            Material.SetFloat("_DetailFactor", GetSingle("$detailblendfactor") / 2);
                        else
                            Material.SetFloat("_DetailFactor", 0.5f);

                        if (ContainsParam("$detailblendmode"))
                            Material.SetInt("_DetailBlendMode", GetInteger("$detailblendmode"));
                    }
                }
            }

            if (ContainsParam("$surfaceprop"))
                SurfaceProp = GetParam("$surfaceprop");
        }

        public Shader GetShader(String shader, Boolean HasAlpha = false)
        {
            if (!string.IsNullOrEmpty(shader))
            {
                if (IsTrue("$additive"))
                    return Shader.Find(uLoader.AdditiveShader);

                if (ContainsParam("$detail"))
                {
                    if (shader.Equals("unlitgeneric"))
                        return Shader.Find(uLoader.DetailUnlitShader);

                    if (shader.Equals("worldtwotextureblend"))
                        return Shader.Find(uLoader.WorldTwoTextureBlend);

                    if (IsTrue("$translucent"))
                        return Shader.Find(uLoader.DetailTranslucentShader);

                    return Shader.Find(uLoader.DetailShader);
                }

                if ((IsTrue("$translucent") && HasAlpha) || (ContainsParam("$alpha") && IsTrue("$alpha", false) == false))
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

                if (IsTrue("$selfillum") && (HasAlpha || ContainsParam("$envmapmask")))
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
                    if (ContainsParam("$basetexture2"))
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
    }
}