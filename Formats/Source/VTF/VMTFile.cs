using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace uSource.Formats.Source.VTF
{
    public class VMTFile
    {
        String FileName = "";
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
                Material = DefaultMaterial = new Material(Shader.Find("VertexLit"));
                Material.name = FileName;
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

            Material = new Material(GetShader(includeVmt == null ? _Shader : includeVmt._Shader));
            Material.name = FileName;
            Material.color = GetColor();

            if (ContainsParma("$basetexture"))
            {
                Material.mainTexture = uResourceManager.LoadTexture(GetParma("$basetexture"))[0, 0];
            }

            //if (Material.mainTexture == null && ContainsParma("$bumpmap"))
            //    Material.mainTexture = ResourceManager.LoadTexture(GetParma("$bumpmap"))[0];

            if (Material.mainTexture == null && ContainsParma("$envmapmask"))
                Material.mainTexture = uResourceManager.LoadTexture(GetParma("$envmapmask"))[0, 0];

            if (ContainsParma("$basetexture2"))
                Material.SetTexture("_BlendTex", uResourceManager.LoadTexture(GetParma("$basetexture2"))[0, 0]);

            //Base props

            //_IsTranslucent
            if (ContainsParma("$translucent"))
            {
                if (Material.HasProperty("_IsTranslucent"))
                {
                    Material.SetInt("_IsTranslucent", GetInteger("$translucent"));
                    Material.SetInt("_Cull", 0);
                    Material.SetInt("_ZState", 0);
                }

                Material.renderQueue = VMTFile.TransparentQueue++;//(int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            //_AlphaTest
            if (ContainsParma("$alphatest"))
            {
                if (Material.HasProperty("_AlphaTest"))
                    Material.SetInt("_AlphaTest", GetInteger("$alphatest"));

                Material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            //$nocull
            if (Material.HasProperty("_Cull"))
            {
                Material.SetInt("_Cull", IsTrue("$nocull") ? 0 : 2);
            }

            //Base props

            if (Material.HasProperty("_Detail"))
            {
                if (ContainsParma("$detail"))
                {
                    Material.SetTexture("_Detail", uResourceManager.LoadTexture(GetParma("$detail"))[0, 0]);

                    if (ContainsParma("$detailscale"))
                    {
                        float detailScale = GetSingle("$detailscale");
                        Material.SetTextureScale("_Detail", new Vector2(detailScale, detailScale));
                    }

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

            if (ContainsParma("$bumpmap"))
            {
                if (Material.HasProperty("_BumpMap"))
                    Material.SetTexture("_BumpMap", uResourceManager.LoadTexture(GetParma("$bumpmap"))[0, 0]);
            }

            //if (ContainsParma("$surfaceprop"))
            //    Material.name = Items["$surfaceprop"];

            //return Material;
        }

        public Shader GetShader(String shader)
        {
            if (!string.IsNullOrEmpty(shader))
            {
                if(shader.Equals("lightmappedgeneric"))
                {
                    //if (IsTrue("$translucent"))
                    //    return Shader.Find("Transparent/Diffuse");

                    //if (ContainsParma("$detail"))
                    //    return Shader.Find("USource/Lightmapped/GenericDetail");

                    return Shader.Find("Diffuse");//Shader.Find("USource/Lightmapped/Generic");
                }

                if (shader.Equals("worldvertextransition"))
                {
                    return Shader.Find("Diffuse");
                }

                if(shader.Equals("worldtwotextureblend"))
                {
                    return Shader.Find("Diffuse");
                }
            }

            //Diffuse
            return Shader.Find("Diffuse");
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
                //if (Items[Input] == "1")
                return this[_Shader][Input] == true;

            return false;
        }
    }
}