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
                String path = root + "/" + FilePath;//Path.Combine(root, FilePath);
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

            //throw new FileLoadException(FilePath + " NOT FOUND!");
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

            for(Int32 EntryID = 0; EntryID < IPAK.Count; EntryID++)
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
            {
                return IPAK.GetInputStream(files[FilePath]);
            }

            //throw new FileLoadException(FilePath + " NOT FOUND!");
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

            //throw new FileLoadException(FilePath + " NOT FOUND!");
            return null;
        }

        public void CloseStreams()
        {
            if(currentStream != null)
                currentStream.Close();

            if (VPK != null)
            {
                VPK.Dispose();
            }

            currentStream = null;
            VPK = null;
        }
    }

    public class uResourceManager
    {
        public static bool RefreshAssets;
        public static String ProjectPath;

        public static readonly List<IResourceProvider> _providers = new List<IResourceProvider>();

        public static readonly String MapsSubFolder = "maps/";
        public static readonly String MapsExtension = ".bsp";

        public static readonly String ModelsSubFolder = "models/";
        public static readonly String[] ModelsExtension =
        {
            ".mdl",
            ".vvd",
            ".dx90.vtx",
            ".vtx",
            ".ani",
            ".phy"
        };
        public static readonly String MaterialsSubFolder = "materials/";
        public static readonly String[] MaterialsExtension =
        {
            ".vmt",
            ".vtf"
        };

        //Cache
        public static Dictionary<String, String> DirectoryCache;
        public static Dictionary<String, Transform> ModelCache;
        public static Dictionary<String, VMTFile> MaterialCache;
        public static Dictionary<String, Texture2D[,]> TextureCache;

        public static void Init(Int32 StartIndex = 0, IResourceProvider mainProvider = null)
        {
            if (ModelCache == null)
                ModelCache = new Dictionary<String, Transform>();

            if (MaterialCache == null)
                MaterialCache = new Dictionary<String, VMTFile>();

            if (TexExportCache == null)
                TexExportCache = new List<String[,]>();

            if (TextureCache == null)
                TextureCache = new Dictionary<String, Texture2D[,]>();

            if (uLoader.SaveAssetsToUnity)
            {
#if UNITY_EDITOR
                RefreshAssets = !UnityEditor.EditorPrefs.GetBool("kAutoRefresh");
#endif

                if (ProjectPath == null)
                {
                    ProjectPath = slashesRegex.Replace(Directory.GetCurrentDirectory(), "/");
                    uSourceSavePath = slashesRegex.Replace("Assets/uSource/" + uLoader.ModFolders[0] + "/", "/");
                    TexExportType = uLoader.ExportTextureAsPNG ? ".png" : ".asset";
                    ProjectPath += "/" + uSourceSavePath;
                }
            }

            if (mainProvider != null)
            {
                _providers.Insert(0, mainProvider);
                return;
            }

            for (Int32 FolderID = StartIndex; FolderID < uLoader.ModFolders.Length; FolderID++)
                Init(uLoader.RootPath, uLoader.ModFolders[FolderID], uLoader.DirPaks[FolderID]);
        }

        public static String uSourceSavePath;
        public static String TexExportType;
        public static void Init(String RootPath, String ModFolder, String[] DirPaks)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("Init Manager");

            //Init root path with mods
            String FullPath = slashesRegex.Replace(RootPath + "/" + ModFolder + "/", "/");//Path.Combine(RootPath, ModFolder + "/");

            if (Directory.Exists(FullPath))
                _providers.Add(new DirProvider(FullPath));

            for (Int32 pakID = 0; pakID < DirPaks.Length; pakID++)
            {
                String vpkFile = DirPaks[pakID];

                String dirPath = FullPath + vpkFile + ".vpk";//Path.Combine(RootPath, ModFolder + "/" + vpkFile + ".vpk");

                if (File.Exists(dirPath))
                {
                    _providers.Add(new VPKProvider(dirPath));
                    continue;
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        public static void AddResourceProvider(IResourceProvider provider)
        {
            _providers.Add(provider);
        }

        public static void RemoveResourceProvider(IResourceProvider provider)
        {
            _providers.Remove(provider);
        }

        public static void RemoveResourceProviders()
        {
            _providers.RemoveRange(0, _providers.Count);
        }

        public static Stream OpenFile(String FilePath)
        {
            for (Int32 i = _providers.Count - 1; i >= 0; --i)
            {
                if (_providers[i].ContainsFile(FilePath))
                {
                    return _providers[i].OpenFile(FilePath);
                }
            }

            return _providers[0].OpenFile(FilePath);
        }

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

                    VBSPFile.Load(BSPStream, MapName);
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

        public static Transform LoadModel(String ModelPath, Boolean WithAnims = false, bool withHitboxes = false)
        {
            String TempPath = NormalizePath(ModelPath, ModelsSubFolder, ModelsExtension[0], false);

            if (ModelCache.ContainsKey(TempPath))
                return UnityEngine.Object.Instantiate(ModelCache[TempPath]);

            Transform Model;

            String FileName = TempPath + ModelsExtension[0];
            try
            {
                MDLFile MDLFile;
                using (Stream mdlStream = OpenFile(FileName))
                {
                    if (mdlStream == null)
                        throw new FileLoadException(FileName + " NOT FOUND!");

                    MDLFile = new MDLFile(mdlStream, WithAnims, withHitboxes);
                }

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

                if (MDLFile.meshExist)
                {
                    FileName = TempPath + ModelsExtension[2];
                    if (VVDFile != null)
                    {
                        using (Stream vtxStream = OpenFile(FileName))
                        {
                            if (vtxStream != null)
                                new VTXFile(vtxStream, MDLFile, VVDFile);
                            else
                            {
                                Debug.LogWarning(FileName + " NOT FOUND!");
                                MDLFile.meshExist = false;
                            }
                        }
                    }
                }

                Model = MDLFile.BuildModel();
                MDLFile = null;
                VVDFile = null;

                ModelCache.Add(TempPath, Model);
            }
            catch(Exception ex)
            {
                Model = new GameObject(TempPath).transform;
                Debug.LogError(String.Format("{0}: {1}", TempPath, ex));
                ModelCache.Add(TempPath, Model);
                return Model;
            }

            return Model;
        }

        public static VMTFile LoadMaterial(String MaterialPath)
        {
            String TempPath = NormalizePath(MaterialPath, MaterialsSubFolder, MaterialsExtension[0], false);

            if (MaterialCache.ContainsKey(TempPath))
            {
                VMTFile VMTCache = MaterialCache[TempPath];

                if (VMTCache.Material == null)
                    VMTCache.CreateMaterial();

                return VMTCache;
            }

            String FileName = TempPath + MaterialsExtension[0];
            VMTFile VMTFile;
            using (Stream vmtStream = OpenFile(FileName))
            {
                if (vmtStream == null)
                {
                    Debug.LogWarning(FileName + " NOT FOUND!");
                    return new VMTFile(null, FileName);
                }

                try
                {
                    VMTFile = new VMTFile(vmtStream, FileName);

                    if (VMTFile != null)
                        VMTFile.CreateMaterial();
                    else
                        VMTFile = new VMTFile(null, FileName);
                }
                catch(Exception ex)
                {
                    Debug.LogError(String.Format("{0}: {1}", TempPath, ex.Message));
                    return new VMTFile(null, FileName);
                }
            }

            MaterialCache.Add(TempPath, VMTFile);
            return VMTFile;
        }

        public static List<String[,]> TexExportCache;
        //public static Dictionary<String, String[,]> PNGCache;
        public static Texture2D[,] LoadTexture(String TexturePath, String AltTexture = null, Boolean ImmediatelyConvert = false, String[,] ExportData = null)
        {
            String TempPath;

            TempPath = NormalizePath(TexturePath, MaterialsSubFolder, MaterialsExtension[1], false);
            if (AltTexture != null)
                AltTexture = NormalizePath(AltTexture, MaterialsSubFolder, MaterialsExtension[1]);

#if UNITY_EDITOR
            if (uLoader.SaveAssetsToUnity && !ImmediatelyConvert)
                TexExportCache.Add(new String[,] { { ExportData[0, 0].Replace(MaterialsExtension[0], ""), ExportData[0, 1], TempPath } });
#endif

            if (TextureCache.ContainsKey(TempPath))
                return TextureCache[TempPath];

#if UNITY_EDITOR
            //Try load asset from project (if exist)
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
                if (vtfStream == null)
                {
                    Debug.LogWarning(FileName + " NOT FOUND!");

                    if (String.IsNullOrEmpty(AltTexture))
                        return new[,] { { Texture2D.whiteTexture } };
                    else
                        return LoadTexture(AltTexture);
                }

                try
                {
                    VTFFile = new VTFFile(vtfStream, FileName);
                }
                catch (Exception ex)
                {
                    Debug.LogError(String.Format("{0}: {1}", TempPath, ex.Message));
                    return new[,] { { Texture2D.whiteTexture } };
                }
            }

#if UNITY_EDITOR
            if (uLoader.SaveAssetsToUnity)
            {
                if (uLoader.ExportTextureAsPNG)
                {
                    //Flip
                    Texture2D TempTex = new Texture2D(VTFFile.Width, VTFFile.Height);
                    Color[] PixelsCopy = VTFFile.Frames[0, 0].GetPixels();
                    Color[] PixelsFlipped = new Color[PixelsCopy.Length];

                    for (Int32 i = 0; i < VTFFile.Height; i++)
                    {
                        Array.Copy(PixelsCopy, i * VTFFile.Width, PixelsFlipped, (VTFFile.Height - i - 1) * VTFFile.Width, VTFFile.Width);
                    }

                    TempTex.SetPixels(PixelsFlipped);
                    TempTex.Apply();
                    VTFFile.Frames[0, 0] = TempTex;
                    //Flip

                    if (ImmediatelyConvert)
                        VTFFile.Frames[0, 0] = SaveTexture(VTFFile.Frames[0, 0], FileName);
                }
                else
                {
                    if (ImmediatelyConvert)
                        SaveAsset(VTFFile.Frames[0, 0], FileName, MaterialsExtension[1], ".asset");
                }
            }
#endif

            TextureCache.Add(TempPath, VTFFile.Frames);

            return VTFFile.Frames;
        }

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

        public static Boolean ContainsFile(String FileName, String SubFolder, String FileExtension)
        {
            String FilePath = NormalizePath(FileName, SubFolder, FileExtension);
            for (Int32 i = _providers.Count - 1; i >= 0; --i)
            {
                if (_providers[i].ContainsFile(FilePath))
                    return true;
            }

            return false;
        }

        public static void ExportFromCache()
        {
#if UNITY_EDITOR
            if (uLoader.SaveAssetsToUnity)
            {
                Int32 CurrentFile = 0;
                Int32 TotalFiles = MaterialCache.Count;
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

                CurrentFile = 0;
                TotalFiles = TexExportCache.Count;
                Boolean NeedRefresh = false;
                for (Int32 TexID = 0; TexID < TotalFiles; TexID++)
                {
                    String FilePath = TexExportCache[TexID][0, 2];

                    UnityEditor.EditorUtility.DisplayProgressBar(String.Format("Convert Textures: {0}/{1}", CurrentFile, TotalFiles), FilePath, (float)CurrentFile / TotalFiles);
                    CurrentFile++;

                    //If textures exist in cache, try convert it
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

                CurrentFile = 0;
                foreach (var ItemCache in TexExportCache)
                {
                    if (ItemCache == null)
                        continue;

                    String MaterialPath = ItemCache[0, 0];
                    String PropertyPath = ItemCache[0, 1];
                    String FilePath = ItemCache[0, 2];

                    UnityEditor.EditorUtility.DisplayProgressBar("Reset textutres in materials", FilePath, (float)CurrentFile / TotalFiles);
                    CurrentFile++;

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

            Byte[] TextureData = Texture.EncodeToPNG();
            using (var File = System.IO.File.Open(ProjectPath + FilePath, FileMode.Create))
            {
                File.Write(TextureData, 0, TextureData.Length);
            }

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

        public static void CloseStreams()
        {
            for (Int32 i = _providers.Count - 1; i >= 0; --i)
            {
                _providers[i].CloseStreams();
            }
        }
    }
}