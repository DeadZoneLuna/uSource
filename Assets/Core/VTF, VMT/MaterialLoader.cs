using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Engine.Source
{
    public class MaterialLoader
    {
        static Dictionary<String, String> Items;
        static Material Material;

        public static bool HasAnimation;
        public static float Parametr;

        public static void SetupAnimations(ref AnimatedTexture ControlScript)
        {
            ControlScript.AnimatedTextureFramerate = float.Parse(Items["animatedtextureframerate"]);
            ControlScript.Frames = TextureLoader.Frames;
        }

        public static string GetParametr(string Data)
        {
            if (Items.ContainsKey(Data))
            {
                Parametr = float.Parse(Items[Data]);
            }

            return Parametr.ToString();
        }

        public static Material Load(String MaterialName)
        {
            HasAnimation = false;
            String Path = String.Empty;

            MaterialName = MaterialName
                .Replace(".vmt", "")
                .Replace("materials/", "");

            if (File.Exists(Application.persistentDataPath + "/" + ConfigLoader.LevelName + "_pakFile/materials/" + MaterialName + ".vmt"))
                Path = Application.persistentDataPath + "/" + ConfigLoader.LevelName + "_pakFile/materials/" + MaterialName + ".vmt";
            else
            {
                for (int i = 0; i < ConfigLoader.ModFolders.Length; i++)
                {
                    if (File.Exists(ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/materials/" + MaterialName + ".vmt"))
                        Path = ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/materials/" + MaterialName + ".vmt";
                }
            }

            if (String.IsNullOrEmpty(Path))
            {
                Debug.Log(String.Format("{0}: File not found", MaterialName + ".vmt"));
				return Load("debug/debugempty");
			}

            Items = KeyValueParse.Load(File.ReadAllLines(Path));

            if (Items.ContainsKey("include"))
                Load(Items["include"]);

            if (Items.ContainsKey("$fallbackmaterial"))
                Load(Items["$fallbackmaterial"]);

            HasAnimation = Items.ContainsKey("animatedtexture") && Items["animatedtexturevar"] == "$basetexture";

            Material = new Material(GetShader());
            Material.color = GetColor();
			Material.name = MaterialName;


			if (Items.ContainsKey("$basetexture2"))
                Material.SetTexture("_BlendTex", TextureLoader.Load(Items["$basetexture2"]));

            if (Items.ContainsKey("$basetexture"))
                Material.mainTexture = TextureLoader.Load(Items["$basetexture"]);

			if (Items.ContainsKey("$bumpmap"))
			{
				Material.SetTexture("_BumpMap", TextureLoader.Load(Items["$bumpmap"]));
			}

            if (Items.ContainsKey("$surfaceprop"))
                Material.name = Items["$surfaceprop"];
            Debug.Log(MaterialName);
            return Material;
        }

        static Shader GetShader()
        {
            // if (IsTrue("$additive"))
            //    return Shader.Find("Particles/Additive");

            String[] ADictionary = { "$translucent", "$alphatest" };

            for (Int32 i = 0; i < ADictionary.Length; i++)
            {
                if (IsTrue(ADictionary[i]))
                {
                    if (Items.ContainsKey("lightmappedgeneric"))
						return Shader.Find("Transparent/Diffuse");
					//return Shader.Find("Custom/LmTransparent");

					return Shader.Find("Transparent/Diffuse");
                }
            }

			if (Items.ContainsKey("$selfillum"))
				return Shader.Find("Custom/SelfIllumiumAlpha");

			if (Items.ContainsKey("lightmappedgeneric"))
				return Shader.Find("VertexLit");//return Shader.Find("Diffuse");

			if (Items.ContainsKey("worldvertextransition"))
                return Shader.Find("Custom/WorldVertexTransition");

            if (Items.ContainsKey("unlitgeneric") || Items.ContainsKey("unlittwotexture"))
                return Shader.Find("Mobile/Unlit (Supports Lightmap)");

            return Shader.Find("Diffuse");
        }

        static Color32 GetColor()
        {
			Color32 MaterialColor = new Color32(255, 255, 255, 255);

			if (Items.ContainsKey("$color"))
            {
                String[] Color = Items["$color"].Replace(".", "").Trim('[', ']', '{', '}').Trim().Split(' ');
                MaterialColor = new Color32(byte.Parse(Color[0]), byte.Parse(Color[1]), byte.Parse(Color[2]), 255);
            }

            if (Items.ContainsKey("$alpha"))
                MaterialColor.a = (byte)(255 * float.Parse(Items["$alpha"]));

            return MaterialColor;
        }

        static bool IsTrue(string Input)
        {
            if (Items.ContainsKey(Input))
                if (Items[Input] == "1")
                    return true;

            return false;
        }
    }
}