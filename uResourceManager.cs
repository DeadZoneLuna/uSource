using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using uSource.Formats.Source.VPK;
using uSource.Formats.Source.VBSP;
using uSource.Formats.Source.VTF;
using uSource.Formats.Source.MDL;
using UnityEngine;

namespace uSource
{
    #region Resource Provider
    public interface IResourceProvider
    {
        Boolean ContainsFile(String FilePath);
        Stream OpenFile(String FilePath);

        void CloseStreams();
    }

    public class DirProvider : IResourceProvider
    {
        FileStream currentFile;
        private String root;
        public DirProvider(String directory)
        {
            if (uResourceManager.DirectoryCache == null)
                uResourceManager.DirectoryCache = new Dictionary<String, String>();

            if (!String.IsNullOrEmpty(directory))
                root = directory;
        }

        public Boolean ContainsFile(String FilePath)
        {
            if (uResourceManager.DirectoryCache.ContainsKey(FilePath))
                return true;
            else
            {
                String path = root + "/" + FilePath;
                if (File.Exists(path))
                {
                    uResourceManager.DirectoryCache.Add(FilePath, path);
                    return true;
                }

                return false;
            }
        }

        public Stream OpenFile(String FilePath)
        {
            if (ContainsFile(FilePath))
            {
                CloseStreams();
                return currentFile = File.OpenRead(uResourceManager.DirectoryCache[FilePath]);
            }

            return null;
        }

        public void CloseStreams()
        {
            if (currentFile != null)
            {
                currentFile.Dispose();
                currentFile.Close();
                return;
            }
        }
    }

    public class PAKProvider : IResourceProvider
    {
        public ZipFile IPAK;
        public Dictionary<String, Int32> files;

        public PAKProvider(Stream stream)
        {
            IPAK = new ZipFile(stream);
            files = new Dictionary<String, Int32>();

            for (Int32 EntryID = 0; EntryID < IPAK.Count; EntryID++)
            {
                ZipEntry entry = IPAK[EntryID];
                if (entry.IsFile)
                {
                    String fileName = entry.Name.ToLower().Replace("\\", "/");
                    if (ContainsFile(fileName))
                        continue;

                    files.Add(fileName, EntryID);
                }
            }
        }

        public Boolean ContainsFile(String FilePath)
        {
            return files.ContainsKey(FilePath);
        }

        public Stream OpenFile(String FilePath)
        {
            if (ContainsFile(FilePath))
                return IPAK.GetInputStream(files[FilePath]);

            return null;
        }

        public void CloseStreams()
        {
            files.Clear();
            files = null;
            IPAK.Close();
            IPAK = null;
        }
    }

    public class VPKProvider : IResourceProvider
    {
        VPKFile VPK;
        MemoryStream currentStream;

        public VPKProvider(String file)
        {
            if (VPK == null)
            {
                VPK = new VPKFile(file);
            }
        }

        public Boolean ContainsFile(String FilePath)
        {
            return VPK.Entries.ContainsKey(FilePath);
        }

        public Stream OpenFile(String FilePath)
        {
            if (ContainsFile(FilePath))
            {
                return VPK.Entries[FilePath].ReadAnyDataStream();
            }

            return null;
        }

        public void CloseStreams()
        {
            if (currentStream != null)
                currentStream.Close();

            if (VPK != null)
                VPK.Dispose();

            currentStream = null;
            VPK = null;
        }
    }
    #endregion

    public class uResourceManager
    {
        #region Sub Folders & Extensions
        public static readonly String MapsSubFolder = "maps/";
        public static readonly String MapsExtension = ".bsp";

        public static readonly String ModelsSubFolder = "models/";
        public static readonly String[] ModelsExtension =
        {
            ".mdl",
            ".vvd",
            ".dx90.vtx",
            ".vtx",
            ".dx80.vtx",
            ".sw.vtx",
            ".ani",
            ".phy"
        };
        public static readonly String MaterialsSubFolder = "materials/";
        public static readonly String[] MaterialsExtension =
        {
            ".vmt",
            ".vtf"
        };
        #endregion

        //Cache
        public static Dictionary<String, String> DirectoryCache;
        public static Dictionary<String, Transform> ModelCache;
        public static Dictionary<String, VMTFile> MaterialCache;
        public static Dictionary<String, Texture2D[,]> TextureCache;

#if UNITY_EDITOR
        public static Boolean RefreshAssets;
        public static String ProjectPath;
        public static String uSourceSavePath;
        public static String TexExportType;
        public static List<String[,]> TexExportCache;
        public static List<Mesh> UV2GenerateCache;
#endif

        public static readonly List<IResourceProvider> Providers = new List<IResourceProvider>();

        public static Regex slashesRegex = new Regex(@"[\\/./]+", RegexOptions.Compiled);
        public static String NormalizePath(String FileName, String SubFolder, String FileExtension, Boolean outputExtension = true)
        {
            //TODO: make sure if subfolder was found only at the beginning 
            //As there may including special names folder with subfolder name & this will be create problems with normalize
            Int32 SubIndex = FileName.IndexOf(SubFolder, StringComparison.Ordinal);
            if (SubIndex >= 0)
                FileName = FileName.Remove(SubIndex, SubFolder.Length);

            Int32 ExtensionIndex = FileName.LastIndexOf(FileExtension, StringComparison.Ordinal);
            if (ExtensionIndex >= 0)
                FileName = FileName.Remove(ExtensionIndex, FileExtension.Length);

            FileName = slashesRegex.Replace(SubFolder + FileName, "/").ToLower();
            if (outputExtension)
                return FileName + FileExtension;
            else
                return FileName;
        }

        #region Provider Manager
        public static void Init(Int32 StartIndex = 0, IResourceProvider mainProvider = null)
        {
            if (ModelCache == null)
                ModelCache = new Dictionary<String, Transform>();

            if (MaterialCache == null)
                MaterialCache = new Dictionary<String, VMTFile>();

            if (TextureCache == null)
                TextureCache = new Dictionary<String, Texture2D[,]>();

#if UNITY_EDITOR
            if (uLoader.GenerateUV2StaticProps)
            {
                if (UV2GenerateCache == null)
                    UV2GenerateCache = new List<Mesh>();
            }

            if (uLoader.SaveAssetsToUnity)
            {
                if (TexExportCache == null)
                    TexExportCache = new List<String[,]>();

                RefreshAssets = !UnityEditor.EditorPrefs.GetBool("kAutoRefresh");

                if (ProjectPath == null)
                {
                    ProjectPath = slashesRegex.Replace(Directory.GetCurrentDirectory(), "/");

                    uSourceSavePath = slashesRegex.Replace("Assets/" + uLoader.OutputAssetsFolder + "/" + uLoader.ModFolders[0] + "/", "/");
                    TexExportType = uLoader.ExportTextureAsPNG ? ".png" : ".asset";
                    ProjectPath += "/" + uSourceSavePath;
                }
            }
#endif

            if (mainProvider != null)
            {
                Providers.Insert(0, mainProvider);
                return;
            }

            for (Int32 FolderID = StartIndex; FolderID < uLoader.ModFolders.Length; FolderID++)
                Init(uLoader.RootPath, uLoader.ModFolders[FolderID], uLoader.DirPaks[FolderID]);
        }

        public static void Init(String RootPath, String ModFolder, String[] DirPaks)
        {
            //Initializing mod folder to provider cache (to use find resources from mod folder before)
            String FullPath = slashesRegex.Replace(RootPath + "/" + ModFolder + "/", "/");

            if (Directory.Exists(FullPath))
                Providers.Add(new DirProvider(FullPath));

            //Initializing additional VPK's from mod folder (to use find resources from VPK's after mod folder)
            for (Int32 pakID = 0; pakID < DirPaks.Length; pakID++)
            {
                String vpkFile = DirPaks[pakID];

                String dirPath = FullPath + vpkFile + ".vpk";

                if (File.Exists(dirPath))
                {
                    Providers.Add(new VPKProvider(dirPath));
                    continue;
                }
            }
        }

        public static void AddResourceProvider(IResourceProvider provider)
        {
            Providers.Add(provider);
        }

        public static void RemoveResourceProvider(IResourceProvider provider)
        {
            Providers.Remove(provider);
        }

        public static void RemoveResourceProviders()
        {
            Providers.RemoveRange(0, Providers.Count);
        }

        public static Boolean ContainsFile(String FileName, String SubFolder, String FileExtension)
        {
            String FilePath = NormalizePath(FileName, SubFolder, FileExtension);
            for (Int32 i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].ContainsFile(FilePath))
                    return true;
            }

            return false;
        }

        public static Stream OpenFile(String FilePath)
        {
            for (Int32 i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].ContainsFile(FilePath))
                {
                    return Providers[i].OpenFile(FilePath);
                }
            }

            return Providers[0].OpenFile(FilePath);
        }

        public static void CloseStreams()
        {
            for (Int32 i = 0; i < Providers.Count; i++)
            {
                Providers[i].CloseStreams();
            }
        }
        #endregion

        public static void LoadMap(String MapName)
        {
            Init(uLoader.RootPath, uLoader.ModFolders[0], uLoader.DirPaks[0]);

            String FileName = MapsSubFolder + MapName.ToLower() + MapsExtension;
            String FilePath = slashesRegex.Replace(uLoader.RootPath + "/", "/") + uLoader.ModFolders[0] + "/" + FileName;
            Stream TempFile = null;

            try
            {
                //Make sure if map exist in folder
                if (File.Exists(FilePath))
                    TempFile = File.OpenRead(FilePath);
                else //Else try load from VPK? :D (somegames stored maps in VPK, why..? D:)
                    TempFile = OpenFile(FileName);

                using (Stream BSPStream = TempFile)
                {
                    if (BSPStream == null)
                    {
                        CloseStreams();
                        RemoveResourceProviders();
                        throw new FileLoadException(FileName + " NOT FOUND!");
                    }

                    if (uLoader.ModFolders.Length > 1)
                        Init(1);

#if UNITY_EDITOR
                    uLoader.DebugTime.Stop();
                    uLoader.DebugTimeOutput.AppendLine("Init time: " + uLoader.DebugTime.Elapsed);
                    uLoader.DebugTime.Restart();
#endif

                    VBSPFile.Load(BSPStream, MapName);

#if UNITY_EDITOR
                    uLoader.DebugTime.Stop();
                    uLoader.DebugTimeOutput.AppendLine("Load time: " + uLoader.DebugTime.Elapsed);
#endif
                }
            }
            finally
            {
                ExportFromCache();
                CloseStreams();
                RemoveResourceProviders();
                if (TempFile != null)
                {
                    TempFile.Dispose();
                    TempFile.Close();
                    TempFile = null;
                }
            }
        }

        public static Transform LoadModel(String ModelPath, Boolean WithAnims = false, Boolean withHitboxes = false, Boolean GenerateUV2 = false)
        {
            //Normalize path before do magic here 
            //(Cuz some paths uses different separators or levels... so we normalize paths always)
            String TempPath = NormalizePath(ModelPath, ModelsSubFolder, ModelsExtension[0], false);

            //If model exist in cache, return it
            if (ModelCache.ContainsKey(TempPath))
                return UnityEngine.Object.Instantiate(ModelCache[TempPath]);
            //Else begin try load model

            Transform Model;
            String FileName = TempPath + ModelsExtension[0];
            try
            {
                //Try load model
                MDLFile MDLFile;
                using (Stream mdlStream = OpenFile(FileName))
                {
                    if (mdlStream == null)
                        throw new FileLoadException(FileName + " NOT FOUND!");

                    MDLFile = new MDLFile(mdlStream, WithAnims, withHitboxes);
                }

                //Try load vertexes
                FileName = TempPath + ModelsExtension[1];
                VVDFile VVDFile;
                using (Stream vvdStream = OpenFile(FileName))
                {
                    if (vvdStream != null)
                        VVDFile = new VVDFile(vvdStream, MDLFile);
                    else
                    {
                        Debug.LogWarning(FileName + " NOT FOUND!");
                        MDLFile.meshExist = false;
                        VVDFile = null;
                    }
                }

                VTXFile VTXFile = null;
                if (MDLFile.meshExist)
                {
                    if (VVDFile != null)
                    {
                        //Here we try find all vtx pattern, from high to low
                        for (Int32 TryVTXID = 0; TryVTXID < 4; TryVTXID++)
                        {
                            FileName = TempPath + ModelsExtension[2 + TryVTXID];
                            using (Stream vtxStream = OpenFile(FileName))
                            {
                                if (vtxStream != null)
                                {
                                    MDLFile.meshExist = true;
                                    VTXFile = new VTXFile(vtxStream, MDLFile, VVDFile);
                                    break;
                                }
                            }
                        }

                        //If at least one VTX was not found, notify about that
                        if (VTXFile == null)
                        {
                            Debug.LogWarning(FileName + " NOT FOUND!");
                            MDLFile.meshExist = false;
                        }
                    }
                }

                //Try build model
                Model = MDLFile.BuildModel(GenerateUV2);

                //Reset all
                MDLFile = null;
                VVDFile = null;
                VTXFile = null;

                //Add model to cache (to load faster than rebuild models again, again and again...)
                ModelCache.Add(TempPath, Model);
            }
            catch (Exception ex)
            {
                Model = new GameObject(TempPath).transform;
                //notify about error
                Debug.LogError(String.Format("{0}: {1}", TempPath, ex));
                ModelCache.Add(TempPath, Model);
                return Model;
            }

            return Model;
        }

        public static VMTFile LoadMaterial(String MaterialPath)
        {
            //Normalize path before do magic here 
            //(Cuz some paths uses different separators or levels... so we normalize paths always)
            String TempPath = NormalizePath(MaterialPath, MaterialsSubFolder, MaterialsExtension[0], false);

            //If material exist in cache, return it
            if (MaterialCache.ContainsKey(TempPath))
            {
                VMTFile VMTCache = MaterialCache[TempPath];

                if (VMTCache.Material == null)
                    VMTCache.CreateMaterial();

                return VMTCache;
            }
            //Else begin try load & parse material

            String FileName = TempPath + MaterialsExtension[0];
            VMTFile VMTFile;
            using (Stream vmtStream = OpenFile(FileName))
            {
                //If at least one material was not found, notify about that
                if (vmtStream == null)
                {
                    Debug.LogWarning(FileName + " NOT FOUND!");
                    return new VMTFile(null, FileName);
                }
                //Else try load & parse material

                try
                {
                    VMTFile = new VMTFile(vmtStream, FileName);

                    if (VMTFile != null)
                        VMTFile.CreateMaterial();
                    else
                        VMTFile = new VMTFile(null, FileName);
                }
                catch (Exception ex)
                {
                    //notify about error
                    Debug.LogError(String.Format("{0}: {1}", TempPath, ex.Message));
                    return new VMTFile(null, FileName);
                }
            }

            //Add material to cache (to load faster than reparse material again, again and again...)
            MaterialCache.Add(TempPath, VMTFile);
            return VMTFile;
        }

        public static Texture2D[,] LoadTexture(String TexturePath, String AltTexture = null, Boolean ImmediatelyConvert = false, String[,] ExportData = null)
        {
            String TempPath;

            //Normalize paths before do magic here 
            //(Cuz some paths uses different separators or levels... so we normalize paths always)
            TempPath = NormalizePath(TexturePath, MaterialsSubFolder, MaterialsExtension[1], false);
            if (AltTexture != null)
                AltTexture = NormalizePath(AltTexture, MaterialsSubFolder, MaterialsExtension[1]);

#if UNITY_EDITOR
            //Add texture to export process from material (if ImmediatelyConvert is false & save assets option enabled)
            if (uLoader.SaveAssetsToUnity && !ImmediatelyConvert)
            {
                if (ExportData != null)
                    TexExportCache.Add(new String[,] { { ExportData[0, 0].Replace(MaterialsExtension[0], ""), ExportData[0, 1], TempPath } });
            }
#endif

            //If texture exist in cache, return it
            if (TextureCache.ContainsKey(TempPath))
                return TextureCache[TempPath];
            //Else begin try load texture

#if UNITY_EDITOR
            //Try load texture from project (if exist & ImmediatelyConvert is false & save assets option enabled))
            if (uLoader.SaveAssetsToUnity && ImmediatelyConvert)
            {
                String FilePath = uSourceSavePath + TempPath + TexExportType;
                Texture2D[,] Frames = new Texture2D[,] { { UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(FilePath) } };
                if (Frames[0, 0] != null)
                {
                    TextureCache.Add(TempPath, Frames);
                    return Frames;
                }
            }
#endif

            VTFFile VTFFile;
            String FileName = TempPath + MaterialsExtension[1];
            using (Stream vtfStream = OpenFile(FileName))
            {
                //If at least one texture was not found, notify about that
                if (vtfStream == null)
                {
                    Debug.LogWarning(FileName + " NOT FOUND!");

                    if (String.IsNullOrEmpty(AltTexture))
                        return new[,] { { Texture2D.whiteTexture } };
                    else
                        return LoadTexture(AltTexture);
                }
                //Else try load texture

                try
                {
                    VTFFile = new VTFFile(vtfStream, FileName);
                }
                catch (Exception ex)
                {
                    //notify about error
                    Debug.LogError(String.Format("{0}: {1}", TempPath, ex.Message));
                    return new[,] { { Texture2D.whiteTexture } };
                }
            }

#if UNITY_EDITOR
            //Try save texture to project (if ImmediatelyConvert is true & save assets option enabled)
            if (uLoader.SaveAssetsToUnity && ImmediatelyConvert)
            {
                if (uLoader.ExportTextureAsPNG)
                    VTFFile.Frames[0, 0] = SaveTexture(VTFFile.Frames[0, 0], FileName);
                else
                    SaveAsset(VTFFile.Frames[0, 0], FileName, MaterialsExtension[1], ".asset");
            }
#endif

            //Add texture to cache (to load faster than rebuild texture again, again and again...)
            TextureCache.Add(TempPath, VTFFile.Frames);

            return VTFFile.Frames;
        }

        #region Export resources
        public static void ExportFromCache()
        {
#if UNITY_EDITOR
            if (uLoader.DebugTime != null)
                uLoader.DebugTime.Restart();

            Int32 CurrentFile = 0;
            Int32 TotalFiles = 0;
            if (uLoader.GenerateUV2StaticProps)
            {
                TotalFiles = UV2GenerateCache.Count;

                //Unwrap settings
                UnityEditor.UnwrapParam UnwrapProps;
                UnityEditor.UnwrapParam.SetDefaults(out UnwrapProps);
                UnwrapProps.hardAngle = uLoader.UV2HardAngleProps;
                UnwrapProps.packMargin = uLoader.UV2PackMarginProps / uLoader.UV2PackMarginTexSize;
                UnwrapProps.angleError = uLoader.UV2AngleErrorProps / 100.0f;
                UnwrapProps.areaError = uLoader.UV2AreaErrorProps / 100.0f;

                for (Int32 MeshID = 0; MeshID < TotalFiles; MeshID++)
                {
                    UnityEditor.EditorUtility.DisplayProgressBar(String.Format("Generate UV2: {0}/{1}", MeshID, TotalFiles), "In Progress: " + UV2GenerateCache[MeshID].name, (float)MeshID / TotalFiles);
                    UnityEditor.Unwrapping.GenerateSecondaryUVSet(UV2GenerateCache[MeshID], UnwrapProps);
                }

                UnityEditor.EditorUtility.ClearProgressBar();
            }

            if (uLoader.SaveAssetsToUnity)
            {
                CurrentFile = 0;
                TotalFiles = MaterialCache.Count;
                foreach (var Material in MaterialCache)
                {
                    UnityEditor.EditorUtility.DisplayProgressBar(String.Format("Save Materials: {0}/{1}", CurrentFile, TotalFiles), Material.Key, (float)CurrentFile / TotalFiles);
                    String FilePath = Material.Key + ".mat";

                    CurrentFile++;
                    //TODO: EditorUtility.CopySerialized
                    if (File.Exists(ProjectPath + FilePath))
                        continue;

                    SaveAsset(MaterialCache[Material.Key].Material, FilePath, UseReplace: false);
                }

                UnityEditor.EditorUtility.ClearProgressBar();

                TotalFiles = TexExportCache.Count;
                Boolean NeedRefresh = false;
                for (Int32 TexID = 0; TexID < TotalFiles; TexID++)
                {
                    String FilePath = TexExportCache[TexID][0, 2];

                    UnityEditor.EditorUtility.DisplayProgressBar(String.Format("Convert Textures: {0}/{1}", TexID, TotalFiles), FilePath, (float)TexID / TotalFiles);

                    //If texture exist in cache, try convert it
                    if (TextureCache.ContainsKey(FilePath))
                    {
                        String AssetPath = TexExportCache[TexID][0, 2] = FilePath + TexExportType;

                        //TODO: EditorUtility.CopySerialized
                        if (File.Exists(ProjectPath + AssetPath))
                            continue;

                        NeedRefresh = true;

                        if (uLoader.ExportTextureAsPNG)
                            SaveTexture(TextureCache[FilePath][0, 0], AssetPath, false, false);
                        else
                            SaveAsset(TextureCache[FilePath][0, 0], AssetPath, UseReplace: false);
                    }
                    else
                        TexExportCache[TexID] = null;
                }

                UnityEditor.EditorUtility.ClearProgressBar();

                //Refresh if one or more asset needs add to "Database"
                if (NeedRefresh)
                {
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                for (Int32 TexID = 0; TexID < TotalFiles; TexID++)
                {
                    if (TexExportCache[TexID] == null)
                        continue;

                    String MaterialPath = TexExportCache[TexID][0, 0];
                    String PropertyPath = TexExportCache[TexID][0, 1];
                    String FilePath = TexExportCache[TexID][0, 2];

                    UnityEditor.EditorUtility.DisplayProgressBar("Reset textutres in materials", FilePath, (float)TexID / TotalFiles);

                    Texture2D TexObj = null;
                    if (File.Exists(ProjectPath + FilePath))
                        TexObj = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(uSourceSavePath + FilePath);

                    if (MaterialCache.ContainsKey(MaterialPath))
                    {
                        MaterialCache[MaterialPath].Material.SetTexture(PropertyPath, TexObj);
                    }
                }

                UnityEditor.EditorUtility.ClearProgressBar();
            }

            if (uLoader.DebugTime != null)
            {
                uLoader.DebugTime.Stop();
                uLoader.DebugTimeOutput.AppendLine("Export / Convert total time: " + uLoader.DebugTime.Elapsed);
                Debug.Log(uLoader.DebugTimeOutput);
            }
#endif
        }

#if UNITY_EDITOR
        public static T LoadAsset<T>(String FilePath, String OriginalType, String ReplaceType) where T : UnityEngine.Object
        {
            FilePath = FilePath.Replace(OriginalType, ReplaceType);

            if (File.Exists(ProjectPath + FilePath))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(uSourceSavePath + FilePath);
            }
            else
                return null;
        }


        public static Texture2D SaveTexture(Texture2D Texture, String FilePath, Boolean UseReplace = true, Boolean RefreshAssets = true)
        {
            if (String.IsNullOrEmpty(FilePath))
                return null;

            CreateProjectDirs(FilePath);

            if (UseReplace)
                FilePath = FilePath.Replace(MaterialsExtension[1], ".png");

            //Flip
            Texture2D TempTex = new Texture2D(Texture.width, Texture.height);
            Color[] PixelsCopy = Texture.GetPixels();
            Color[] PixelsFlipped = new Color[PixelsCopy.Length];

            for (Int32 i = 0; i < Texture.height; i++)
            {
                Array.Copy(PixelsCopy, i * Texture.width, PixelsFlipped, (Texture.height - i - 1) * Texture.width, Texture.width);
            }

            TempTex.SetPixels(PixelsFlipped);
            TempTex.Apply();
            //UnityEngine.Object.DestroyImmediate(Texture);
            Texture = TempTex;
            //Flip

            Byte[] TextureData = Texture.EncodeToPNG();
            using (var File = System.IO.File.Open(ProjectPath + FilePath, FileMode.Create))
            {
                File.Write(TextureData, 0, TextureData.Length);
            }

            GC.Collect();

            if (RefreshAssets)
            {
                UnityEditor.AssetDatabase.Refresh();
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(uSourceSavePath + FilePath);
            }
            else
                return null;
        }

        public static void SaveAsset(UnityEngine.Object Object, String FilePath, String OriginalType = "", String ReplaceType = "", Boolean UseReplace = true)
        {
            if (String.IsNullOrEmpty(FilePath))
                return;

            CreateProjectDirs(FilePath);

            if (UseReplace)
                FilePath = uSourceSavePath + FilePath.Replace(OriginalType, ReplaceType);
            else
                FilePath = uSourceSavePath + FilePath;

            UnityEditor.AssetDatabase.CreateAsset(Object, FilePath);
        }

        static void CreateProjectDirs(String FilePath)
        {
            String FullPath = ProjectPath + Path.GetDirectoryName(FilePath);

            if (!Directory.Exists(FullPath))
            {
                Directory.CreateDirectory(FullPath);
                if (RefreshAssets)
                    UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
        #endregion
    }
}