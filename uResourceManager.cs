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
                String path = Path.Combine(root, FilePath);
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

            return;
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
            if (mainProvider != null)
            {
                _providers.Insert(0, mainProvider);
                return;
            }

            for (Int32 FolderID = StartIndex; FolderID < uLoader.ModFolders.Length; FolderID++)
                Init(uLoader.RootPath, uLoader.ModFolders[FolderID], uLoader.DirPaks[FolderID]);
        }

        public static void Init(String RootPath, String ModFolder, String[] DirPaks)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("Init Manager");

            //Init root path with mods
            String FullPath = Path.Combine(RootPath, ModFolder + "/");

            if (Directory.Exists(FullPath))
                _providers.Add(new DirProvider(FullPath));

            for (Int32 pakID = 0; pakID < DirPaks.Length; pakID++)
            {
                String vpkFile = DirPaks[pakID];

                String dirPath = Path.Combine(RootPath, ModFolder + "/" + vpkFile + ".vpk");

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
            try
            {
                using (Stream BSPStream = OpenFile(FileName))
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
                CloseStreams();
                RemoveResourceProviders();
            }
        }

        public static Transform LoadModel(String ModelPath, Boolean WithAnims = false, bool withHitboxes = false)
        {
            if (ModelCache == null)
                ModelCache = new Dictionary<String, Transform>();

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
            if (MaterialCache == null)
                MaterialCache = new Dictionary<String, VMTFile>();

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
                    return new VMTFile(null);
                }
            }

            MaterialCache.Add(TempPath, VMTFile);
            return VMTFile;
        }

        public static Texture2D[,] LoadTexture(String TexturePath, String AltTexture = null)
        {
            if (TextureCache == null)
                TextureCache = new Dictionary<String, Texture2D[,]>();

            String TempPath;

            if (String.IsNullOrEmpty(AltTexture))
                TempPath = NormalizePath(TexturePath, MaterialsSubFolder, MaterialsExtension[1], false);
            else
                TempPath = NormalizePath(AltTexture, MaterialsSubFolder, MaterialsExtension[1], false);

            if (TextureCache.ContainsKey(TempPath))
                return TextureCache[TempPath];

            VTFFile VTFFile;
            String FileName = TempPath + MaterialsExtension[1];
            using (Stream vtfStream = OpenFile(FileName))
            {
                if (vtfStream == null)
                {
                    Debug.LogWarning(FileName + " NOT FOUND!");
                    return new[,] { { Texture2D.whiteTexture } };
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

            TextureCache.Add(TempPath, VTFFile.Frames);

            return VTFFile.Frames;
        }

        //private static Regex doubleDotRegex1 = new Regex("^(.*/)?([^/\\\\.]+/\\\\.\\\\./)(.+)$");
        public static String NormalizePath(String FileName, String SubFolder, String FileExtension, Boolean outputExtension = true)
        {
            //Normalize paths
            Int32 dotPath = FileName.IndexOf("./", StringComparison.Ordinal);
            if (dotPath >= 0)
                FileName = FileName.Remove(dotPath, 1);

            Int32 SubIndex = FileName.IndexOf(SubFolder, StringComparison.Ordinal);
            if (SubIndex >= 0)
                FileName = FileName.Remove(SubIndex, SubFolder.Length);

            Int32 ExtensionIndex = FileName.LastIndexOf(FileExtension, StringComparison.Ordinal);
            if (ExtensionIndex >= 0)
                FileName = FileName.Remove(ExtensionIndex, FileExtension.Length);

            if (outputExtension)
                return (SubFolder + FileName + FileExtension).ToLower().Replace('\\', '/').Replace("//", "/");
            else
                return (SubFolder + FileName).ToLower().Replace('\\', '/').Replace("//", "/");
            //Normalize paths
        }

        public static Boolean ContainsFile(String FileName, String SubFolder = "", String FileExtension = "")
        {
            String FilePath = NormalizePath(FileName, SubFolder, FileExtension);
            for (Int32 i = _providers.Count - 1; i >= 0; --i)
            {
                if (_providers[i].ContainsFile(FilePath))
                    return true;
            }

            return false;
        }

        public static void CloseStreams()
        {
            for (Int32 i = _providers.Count - 1; i >= 0; --i)
            {
                _providers[i].CloseStreams();
            }
        }
    }
}