using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using uSource.Formats.Source.VTF;
using uSource.MathLib;
using UnityEngine;

namespace uSource.Formats.Source.VBSP
{
    public class VBSPFile : VBSPStruct
    {
        // ======== BSP ======= //
        public static uReader BSPFileReader;
        static dheader_t BSP_Header;

        static dface_t[] BSP_Faces;
        static dmodel_t[] BSP_Models;

        static doverlay_t[] BSP_Overlays;

        static ddispinfo_t[] BSP_DispInfo;
        static dDispVert[] BSP_DispVerts;

        static texinfo_t[] BSP_TexInfo;
        static dtexdata_t[] BSP_TexData;
        static String[] BSP_TextureStringData;

        static dedge_t[] BSP_Edges;
        static Vector3[] BSP_Vertices;
        static Int32[] BSP_Surfedges;

        // ======== OTHER ======= //

        static Face[] BSP_CFaces, BSP_CDisp;
        public static List<GameObject> BSP_Brushes;

        public static Transform ShadowControl;
        public static GameObject BSP_WorldSpawn;
        public static Flare GlowFlare;

        static System.Diagnostics.Stopwatch stopwatch;

        //TODO: Check if LUMPs has a LZMA compression (ex: updated tf maps)
        public static void Load(Stream stream, string BSPName)
        {
            //bspPath = uLoader.RootPath + "/" + uLoader.ModFolders[0] + "/maps/" + BSPName + ".bsp";
            //if (!File.Exists(bspPath))
            //    throw new FileNotFoundException(String.Format("Map file ({0}) wasn't found in the ({1}) mod-folder. Check weather a path is valid.", BSPName, uLoader.ModFolders[0]));

            //bspPath = BSPName;
            BSPFileReader = new uReader(stream);
            BSPFileReader.ReadType(ref BSP_Header);

            if (BSP_Header.Ident != 0x50534256)
                throw new FileLoadException(String.Format("{0}: File signature does not match 'VBSP'", BSPName));

            if (BSP_Header.Version < 19 || BSP_Header.Version > 21)
                throw new FileLoadException(String.Format("{0}: BSP version ({1}) isn't supported", BSPName, BSP_Header.Version));

            if (BSP_Header.Lumps[0].FileOfs == 0)
            {
                Debug.Log("Found Left 4 Dead 2 header");
                for (Int32 i = 0; i < BSP_Header.Lumps.Length; i++)
                {
                    BSP_Header.Lumps[i].FileOfs = BSP_Header.Lumps[i].FileLen;
                    BSP_Header.Lumps[i].FileLen = BSP_Header.Lumps[i].Version;
                }
            }

            BSP_WorldSpawn = new GameObject(BSPName);

            if (BSP_Header.Lumps[58].FileLen / 56 <= 0)
            {
                BSP_Faces = new dface_t[BSP_Header.Lumps[7].FileLen / 56];
                BSPFileReader.ReadArray(ref BSP_Faces, BSP_Header.Lumps[7].FileOfs);
            }
            else
            {
                BSP_Faces = new dface_t[BSP_Header.Lumps[58].FileLen / 56];
                BSPFileReader.ReadArray(ref BSP_Faces, BSP_Header.Lumps[58].FileOfs);
            }

            BSP_Models = new dmodel_t[BSP_Header.Lumps[14].FileLen / 48];
            BSPFileReader.ReadArray(ref BSP_Models, BSP_Header.Lumps[14].FileOfs);

            BSP_Overlays = new doverlay_t[BSP_Header.Lumps[45].FileLen / 352];
            BSPFileReader.ReadArray(ref BSP_Overlays, BSP_Header.Lumps[45].FileOfs);

            BSP_DispInfo = new ddispinfo_t[BSP_Header.Lumps[26].FileLen / 176];
            BSPFileReader.ReadArray(ref BSP_DispInfo, BSP_Header.Lumps[26].FileOfs);

            BSP_DispVerts = new dDispVert[BSP_Header.Lumps[33].FileLen / 20];
            BSPFileReader.ReadArray(ref BSP_DispVerts, BSP_Header.Lumps[33].FileOfs);

            BSP_TexInfo = new texinfo_t[BSP_Header.Lumps[18].FileLen / 12];
            BSPFileReader.ReadArray(ref BSP_TexInfo, BSP_Header.Lumps[18].FileOfs);

            BSP_TexInfo = new texinfo_t[BSP_Header.Lumps[6].FileLen / 72];
            BSPFileReader.ReadArray(ref BSP_TexInfo, BSP_Header.Lumps[6].FileOfs);

            BSP_TexData = new dtexdata_t[BSP_Header.Lumps[2].FileLen / 32];
            BSPFileReader.ReadArray(ref BSP_TexData, BSP_Header.Lumps[2].FileOfs);

            BSP_TextureStringData = new String[BSP_Header.Lumps[44].FileLen / 4];

            Int32[] BSP_TextureStringTable = new Int32[BSP_Header.Lumps[44].FileLen / 4];
            BSPFileReader.ReadArray(ref BSP_TextureStringTable, BSP_Header.Lumps[44].FileOfs);

            if (uLoader.ModFolders[0] == "tf")
                return;

            for (Int32 i = 0; i < BSP_TextureStringTable.Length; i++)
                BSP_TextureStringData[i] = BSPFileReader.ReadNullTerminatedString(BSP_Header.Lumps[43].FileOfs + BSP_TextureStringTable[i]);

            BSP_Edges = new dedge_t[BSP_Header.Lumps[12].FileLen / 4];
            BSPFileReader.ReadArray(ref BSP_Edges, BSP_Header.Lumps[12].FileOfs);

            BSPFileReader.BaseStream.Seek(BSP_Header.Lumps[3].FileOfs, SeekOrigin.Begin);
            BSP_Vertices = new Vector3[BSP_Header.Lumps[3].FileLen / 12];

            for (Int32 i = 0; i < BSP_Vertices.Length; i++)
                BSP_Vertices[i] = BSPFileReader.ReadVector3D(true) * uLoader.WorldScale;

            BSP_Surfedges = new Int32[BSP_Header.Lumps[13].FileLen / 4];
            BSPFileReader.ReadArray(ref BSP_Surfedges, BSP_Header.Lumps[13].FileOfs);

            BSP_Brushes = new List<GameObject>();

            //Create new lightmap list
            uLoader.lightmapsData = new List<LightmapData>();
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

            //Debug import
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            //Debug import

            LoadEntities();
            LoadStaticProps();

            //Debug import
            stopwatch.Stop();
            Debug.Log("Total Time " + stopwatch.Elapsed);
            //Debug import
            UnloadAll();
        }

        static void UnloadAll()
        {
            ShadowControl = null;
            BSP_Faces = null;
            BSP_Models = null;
            BSP_Overlays = null;
            BSP_DispInfo = null;
            BSP_DispVerts = null;
            BSP_TexInfo = null;
            BSP_TexData = null;
            BSP_TextureStringData = null;
            BSP_Edges = null;
            BSP_Vertices = null;
            BSP_Surfedges = null;
            BSP_CFaces = null;
            BSP_CDisp = null;
            BSP_Brushes.Clear();
            BSPFileReader.BaseStream.Dispose();
            BSPFileReader.BaseStream.Close();
            //IPAK.Close();
            //IPAK = null;
            GC.Collect();
            //BSP_WorldSpawn = null;
        }

        static void LoadEntities()
        {
            BSPFileReader.BaseStream.Seek(BSP_Header.Lumps[0].FileOfs, SeekOrigin.Begin);

            String Input = new String(BSPFileReader.ReadChars(BSP_Header.Lumps[0].FileLen));
            MatchCollection Matches = Regex.Matches(Input, @"{[^}]*}", RegexOptions.IgnoreCase);

            for (Int32 i = 0; i < Matches.Count; i++)
            {
                List<String> Data = new List<String>();

                MatchCollection trims = Regex.Matches(Matches[i].Value, "\"[^\"]*\"", RegexOptions.IgnoreCase);

                for (int k = 0; k < trims.Count; k++)
                {
                    Data.Add(trims[k].Value.Trim('"'));
                }

                if (Data[Data.FindIndex(n => n == "classname") + 1] == "worldspawn")
                {
                    InitPAKLump();

                    CreateSkybox(Data);

                    CreateFaces();
                    CreateModels();

                    CreateDispFaces();
                    CreateDisplacements();

                    //GeneratePhysModels();

                    continue;
                }

                Transform EntityObject = null;
                if (Data[0] == "model")
                {
                    EntityObject = GameObject.Find(Data[1]).transform;
                }
                else
                {
                    EntityObject = new GameObject().transform;
                    EntityObject.parent = BSP_WorldSpawn.transform;
                }

                if (EntityObject != null)
                {
                    if (uLoader.isDebug)
                        EntityObject.gameObject.AddComponent<EntInfo>().Configure(Data);
                    else
                        EntityObject.Configure(Data);
                }
            }
        }

        static void CreateFaces()
        {
            BSP_CFaces = new Face[BSP_Faces.Length];

            for (Int32 Index = 0; Index < BSP_Faces.Length; Index++)
            {
                dface_t CFace = BSP_Faces[Index];

                Vector3[] FaceVertices = new Vector3[CFace.NumEdges];
                Vector2[] TextureUV = new Vector2[CFace.NumEdges], LightmapUV = new Vector2[CFace.NumEdges];

                Color32[] VertColors = new Color32[CFace.NumEdges];

                texinfo_t CTexinfo = BSP_TexInfo[CFace.TexInfo];
                dtexdata_t CTexdata = BSP_TexData[CTexinfo.TexData];

                for (Int32 i = CFace.FirstEdge, k = 0; i < CFace.FirstEdge + CFace.NumEdges; i++, k++)
                {
                    FaceVertices[k] = BSP_Surfedges[i] > 0 ? BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].V[0]] : BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].V[1]];
                    VertColors[k] = new Color32(0, 0, 0, 0);
                }

                Int32[] FaceIndices = new int[(FaceVertices.Length - 1) * 3];
                for (Int32 i = 1, k = 0; i < FaceVertices.Length - 1; i++, k += 3)
                {
                    FaceIndices[k] = 0;
                    FaceIndices[k + 1] = i;
                    FaceIndices[k + 2] = i + 1;
                }

                Vector3 tS = new Vector3(-CTexinfo.TextureVecs[0].y, CTexinfo.TextureVecs[0].z, CTexinfo.TextureVecs[0].x);
                Vector3 tT = new Vector3(-CTexinfo.TextureVecs[1].y, CTexinfo.TextureVecs[1].z, CTexinfo.TextureVecs[1].x);

                for (Int32 i = 0; i < FaceVertices.Length; i++)
                {
                    Single TextureUVS = (Vector3.Dot(FaceVertices[i], tS) + CTexinfo.TextureVecs[0].w * uLoader.WorldScale) / (CTexdata.View_Width * uLoader.WorldScale);
                    Single TextureUVT = (Vector3.Dot(FaceVertices[i], tT) + CTexinfo.TextureVecs[1].w * uLoader.WorldScale) / (CTexdata.View_Height * uLoader.WorldScale);
                    TextureUV[i] = new Vector2(TextureUVS, TextureUVT);
                }

                Vector3 lS = new Vector3(-CTexinfo.LightmapVecs[0].y, CTexinfo.LightmapVecs[0].z, CTexinfo.LightmapVecs[0].x);
                Vector3 lT = new Vector3(-CTexinfo.LightmapVecs[1].y, CTexinfo.LightmapVecs[1].z, CTexinfo.LightmapVecs[1].x);

                for (Int32 i = 0; i < FaceVertices.Length; i++)
                {
                    Single LightmapS = (Vector3.Dot(FaceVertices[i], lS) + (CTexinfo.LightmapVecs[0].w + 0.5f - CFace.LightmapTextureMinsInLuxels[0]) * uLoader.WorldScale) / ((CFace.LightmapTextureSizeInLuxels[0] + 1) * uLoader.WorldScale);
                    Single LightmapT = (Vector3.Dot(FaceVertices[i], lT) + (CTexinfo.LightmapVecs[1].w + 0.5f - CFace.LightmapTextureMinsInLuxels[1]) * uLoader.WorldScale) / ((CFace.LightmapTextureSizeInLuxels[1] + 1) * uLoader.WorldScale);
                    LightmapUV[i] = new Vector2(LightmapS, LightmapT);
                }

                BSP_CFaces[Index] = new Face
                {
                    TexInfo = CTexinfo,
                    TexData = CTexdata,

                    Vertices = FaceVertices,
                    Triangles = FaceIndices,
                    Colors = VertColors,

                    UV = TextureUV,
                    UV2 = LightmapUV,

                    LightOfs = CFace.LightOfs,

                    LightMapW = CFace.LightmapTextureSizeInLuxels[0] + 1,
                    LightMapH = CFace.LightmapTextureSizeInLuxels[1] + 1
                };

            }
        }

        //TODO
        static void CreateOverlays()
        {
            //TODO
        }

        static void CreateModels()
        {
            BSP_Brushes = new List<GameObject>();

            for (Int32 Index = 0; Index < BSP_Models.Length; Index++)
            {
                GameObject Model = new GameObject("*" + Index);
                Model.transform.parent = BSP_WorldSpawn.transform;

                Dictionary<Int32, List<Int32>> MeshInfo = new Dictionary<Int32, List<Int32>>();

                for (Int32 i = BSP_Models[Index].FirstFace; i < BSP_Models[Index].FirstFace + BSP_Models[Index].NumFaces; i++)
                {
                    if (!MeshInfo.ContainsKey(BSP_TexData[BSP_TexInfo[BSP_Faces[i].TexInfo].TexData].NameStringTableID))
                        MeshInfo.Add(BSP_TexData[BSP_TexInfo[BSP_Faces[i].TexInfo].TexData].NameStringTableID, new List<Int32>());

                    MeshInfo[BSP_TexData[BSP_TexInfo[BSP_Faces[i].TexInfo].TexData].NameStringTableID].Add(i);
                    MeshInfo[BSP_TexData[BSP_TexInfo[BSP_Faces[i].TexInfo].TexData].NameStringTableID].Add(i);
                }

                for (Int32 i = 0; i < BSP_TextureStringData.Length; i++)
                {
                    if (!MeshInfo.ContainsKey(i))
                        continue;

                    List<Face> Faces = new List<Face>();

                    List<Vector3> Vertices = new List<Vector3>();
                    List<Color32> Colors = new List<Color32>();
                    List<Int32> Triangles = new List<Int32>();
                    List<Vector2> UV = new List<Vector2>();

                    for (Int32 j = 0; j < MeshInfo[i].Count; j++)
                    {
                        dface_t CFace = BSP_Faces[MeshInfo[i][j]];

                        if (CFace.DispInfo == -1)
                        {
                            Faces.Add(BSP_CFaces[MeshInfo[i][j]]);

                            Int32 PointOffset = Vertices.Count;
                            for (Int32 n = 0; n < Faces[j].Triangles.Length; n++)
                                Triangles.Add(Faces[j].Triangles[n] + PointOffset);

                            Vertices.AddRange(Faces[j].Vertices);
                            Colors.AddRange(Faces[j].Colors);
                            UV.AddRange(Faces[j].UV);
                        }
                    }

                    GameObject MeshObject = new GameObject(BSP_TextureStringData[i]);
                    MeshObject.transform.parent = Model.transform;
                    MeshObject.isStatic = true;

                    MeshRenderer MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
                    MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    VMTFile ValveMaterial = uResourceManager.LoadMaterial(BSP_TextureStringData[i]);
                    MeshRenderer.sharedMaterial = ValveMaterial.Material;
#if UNITY_EDITOR
                    MeshObject.AddComponent<DebugMaterial>().Init(ValveMaterial);
#endif

                    MeshRenderer.lightmapIndex = uLoader.CurrentLightmap;

                    /*if (ValveMaterial.HasAnimation)
                    {
                        AnimatedTexture AnimationControlScript = MeshObject.AddComponent<AnimatedTexture>();
                        ValveMaterial.SetupAnimations(ref AnimationControlScript);
                    }*/

                    Mesh Mesh = MeshObject.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                    Mesh.SetVertices(Vertices);
                    Mesh.SetTriangles(Triangles, 0);
                    Mesh.SetColors(Colors);
                    Mesh.SetUVs(0, UV);

                    if (BSP_TextureStringData[i].Contains("TOOLS/"))
                    {
                        MeshRenderer.enabled = false;
                        BSP_Brushes.Add(MeshObject);
                    }
                    else
                    {
                        MeshObject.AddComponent<MeshCollider>();

                        List<Vector2> UV2 = new List<Vector2>();
                        Texture2D Lightmap_tex = new Texture2D(1, 1);

                        CreateLightmap(Faces, ref Lightmap_tex, ref UV2);

                        if (uLoader.useLightmapsAsTextureShader)
                            if (MeshRenderer.sharedMaterial != null)
                                MeshRenderer.sharedMaterial.SetTexture("_LightMap", Lightmap_tex);

                        Mesh.SetUVs(1, UV2);
                    }

                    Mesh.RecalculateNormals();
                    //Mesh.RecalculateTangents();
                }

            }
        }

        static void CreateDisplacements()
        {
            Dictionary<Int32, List<Int32>> MeshInfo = new Dictionary<Int32, List<Int32>>();

            for (Int32 i = 0; i < BSP_DispInfo.Length; i++)
            {
                if (!MeshInfo.ContainsKey(BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].TexInfo].TexData].NameStringTableID))
                    MeshInfo.Add(BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].TexInfo].TexData].NameStringTableID, new List<Int32>());

                MeshInfo[BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].TexInfo].TexData].NameStringTableID].Add(BSP_DispInfo[i].MapFace);
            }

            for (Int32 i = 0; i < BSP_TextureStringData.Length; i++)
            {
                if (!MeshInfo.ContainsKey(i))
                    continue;

                List<Face> Faces = new List<Face>();

                List<Vector3> Vertices = new List<Vector3>();
                List<Color32> Colors = new List<Color32>();
                List<Int32> Triangles = new List<Int32>();
                List<Vector2> UV = new List<Vector2>();

                for (Int32 j = 0; j < MeshInfo[i].Count; j++)
                {
                    if (BSP_Faces[MeshInfo[i][j]].DispInfo != -1)
                    {
                        Faces.Add(BSP_CDisp[BSP_Faces[MeshInfo[i][j]].DispInfo]);

                        Int32 PointOffset = Vertices.Count;
                        for (Int32 n = Faces[j].Triangles.Length; n-- > 0;)//for (Int32 n = 0; n < Faces[j].Triangles.Length; n++)
                            Triangles.Add(Faces[j].Triangles[n] + PointOffset);

                        Vertices.AddRange(Faces[j].Vertices);
                        Colors.AddRange(Faces[j].Colors);
                        UV.AddRange(Faces[j].UV);
                    }
                }

                GameObject MeshObject = new GameObject(BSP_TextureStringData[i]);
                //MeshObject.transform.localScale = new Vector3(1, 1, -1);
                MeshObject.transform.parent = BSP_WorldSpawn.transform;
                MeshObject.isStatic = true;

                MeshRenderer MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
                MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                VMTFile ValveMaterial = uResourceManager.LoadMaterial(BSP_TextureStringData[i]);
                MeshRenderer.sharedMaterial = ValveMaterial.Material;
#if UNITY_EDITOR
                MeshObject.AddComponent<DebugMaterial>().Init(ValveMaterial);
#endif

                MeshRenderer.lightmapIndex = uLoader.CurrentLightmap;

                /*if (ValveMaterial.HasAnimation)
                {
                    AnimatedTexture AnimationControlScript = MeshObject.AddComponent<AnimatedTexture>();
                    ValveMaterial.SetupAnimations(ref AnimationControlScript);
                }*/

                Mesh Mesh = MeshObject.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                MeshObject.AddComponent<MeshCollider>();

                Mesh.SetVertices(Vertices);
                Mesh.SetTriangles(Triangles, 0);
                Mesh.SetColors(Colors);
                Mesh.SetUVs(0, UV);

                List<Vector2> UV2 = new List<Vector2>();
                Texture2D Lightmap_tex = new Texture2D(1, 1);

                CreateLightmap(Faces, ref Lightmap_tex, ref UV2);

                if (uLoader.useLightmapsAsTextureShader)
                    if (MeshRenderer.sharedMaterial != null)
                        MeshRenderer.sharedMaterial.SetTexture("_LightMap", Lightmap_tex);

                Mesh.SetUVs(1, UV2);

                Mesh.RecalculateNormals();
                //Mesh.RecalculateTangents();
            }
        }

        static GameObject CreateGameObject(string name = null, GameObject parent = null)
        {
            var obj = new GameObject();
            obj.name = name ?? obj.name;
            obj.transform.SetParent(parent ? parent.transform : BSP_WorldSpawn.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            return obj;
        }

        static void GeneratePhysModels()
        {
            var physModels = new List<PhysModel>();

            BSPFileReader.BaseStream.Position = BSP_Header.Lumps[29].FileOfs;
            while (true)
            {
                var modelIndex = BSPFileReader.ReadInt32();

                if (modelIndex == -1)
                    break;

                var dataSize = BSPFileReader.ReadInt32();
                var keyDataSize = BSPFileReader.ReadInt32();
                var solidCount = BSPFileReader.ReadInt32();
                var collisionData = BSPFileReader.ReadBytes(dataSize);
                var keyData = BSPFileReader.ReadBytes(keyDataSize);
                var physModel = new PhysModel(modelIndex, solidCount, collisionData, keyData);
                physModels.Add(physModel);
            }

            foreach (var pm in physModels)
            {
                if (BSP_Brushes.Count < pm.ModelIndex || pm.ModelIndex < 0)
                {
                    Debug.LogWarning("Attempt to load a strat PhysModel");
                    continue;
                }

                Int32 Index = pm.ModelIndex;
                if (pm.ModelIndex > 0)
                    Index = pm.ModelIndex - 1;

                var modelParent = BSP_Brushes[Index];
                var solidData = new Dictionary<int, string>();

                // this is wrong
                foreach (var kvp in pm.KeyValues)
                {
                    int indx;
                    if (int.TryParse(kvp.Value["index"], out indx))
                    {
                        solidData.Add(indx, kvp.Key);
                    }
                }

                var physModelContainer = CreateGameObject($"PhysModel #{pm.ModelIndex}", modelParent);
                var solidIdx = -1;

                foreach (var solid in pm.Solids)
                {
                    solidIdx++;

                    if (solidIdx > 0)
                        continue;

                    // i don't think this was right
                    //var contents = GetBrushContents(pm.ModelIndex, solidIdx);
                    //var isWater = contents.HasFlag(BrushContents.WATER) || solidData.ContainsKey(solidIdx) && solidData[solidIdx] == "fluid";
                    solid.ConvexContainer = CreateGameObject($"Solid #{solidIdx}", physModelContainer);

                    var convexIdx = -1;
                    foreach (var cc in solid.Convexes)
                    {
                        convexIdx++;
                        if (cc.Skip || cc.Verts.Count < 4)
                        {
                            if (cc.Verts.Count < 4)
                            {
                                Debug.Log("SKIPPERINO");
                            }
                            continue;
                        }

                        var solidObj = CreateGameObject($"Convex {cc}", solid.ConvexContainer);
                        var tris = cc.Triangles;
                        var verts = new Vector3[cc.Verts.Count];
                        var pivot = new Vector3(cc.Verts[0].x, -cc.Verts[0].y, cc.Verts[0].z);

                        for (int i = 0; i < cc.Verts.Count; i++)
                        {
                            verts[i] = new Vector3(cc.Verts[i].x, -cc.Verts[i].y, cc.Verts[i].z) - pivot;
                        }

                        var mf = solidObj.AddComponent<MeshFilter>();
                        mf.sharedMesh = new Mesh();
                        mf.sharedMesh.vertices = verts;
                        mf.sharedMesh.triangles = tris.ToArray();
                        mf.sharedMesh.ReverseNormals();
                        var mc = solidObj.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = true;

                        /*if (_bsp.Brushes[cc.BrushIndex].Contents.HasFlag(BrushContents.LADDER))
                        {
                            mc.gameObject.tag = "Ladder";
                        }

                        if (_bsp.Brushes[cc.BrushIndex].Contents.HasFlag(BrushContents.WATER))
                        {
                            mc.gameObject.layer = LayerMask.NameToLayer("Water");
                            mc.convex = true;
                            mc.isTrigger = true;
                        }*/

                        solidObj.transform.position = pivot;
                    }
                }

                physModelContainer.transform.eulerAngles = new Vector3(0, -90, 0);
            }
        }

        //"invert" solution
        //REDxEYE:
        //def get_index(ind):
        //return (ind + min_index) % 4
        static int MinIndex = 0;
        static int GetDFaceIndex(int Index)
        {
            return (Index + MinIndex) % 4;
        }

        static void CreateDispFaces()
        {
            BSP_CDisp = new Face[BSP_DispInfo.Length];

            for (Int32 Index = 0; Index < BSP_DispInfo.Length; Index++)
            {
                Face MapFace = BSP_CFaces[BSP_DispInfo[Index].MapFace];

                Vector3[] FaceVertices = MapFace.Vertices;
                List<Color32> VertColors = new List<Color32>();

                List<Vector3> DispVertices = new List<Vector3>();
                List<Int32> DispIndices = new List<Int32>();

                List<Vector2> TextureCoordinates = new List<Vector2>();
                List<Vector2> LightmapCoordinates = new List<Vector2>();

                for (Int32 i = 0; i < FaceVertices.Length; i++)
                    FaceVertices[i] = MathLibrary.UnSwapZY(FaceVertices[i]);

                Vector3 tS = new Vector3(MapFace.TexInfo.TextureVecs[0].x, MapFace.TexInfo.TextureVecs[0].y, MapFace.TexInfo.TextureVecs[0].z);
                Vector3 tT = new Vector3(MapFace.TexInfo.TextureVecs[1].x, MapFace.TexInfo.TextureVecs[1].y, MapFace.TexInfo.TextureVecs[1].z);

                //Int32 MinIndex = 0;
                Single MinDist = 1.0e9f;

                for (Int32 i = 0; i < 4; i++)
                {
                    Single Distance = Vector3.Distance(FaceVertices[i], BSP_DispInfo[Index].StartPosition * uLoader.WorldScale);

                    if (Distance < MinDist)
                    {
                        MinDist = Distance;
                        MinIndex = i;
                    }
                }

                ///This replaced by <seealso cref="GetDFaceIndex"/>
                /*for (Int32 i = 0; i < MinIndex; i++)
                {
                    Vector3 Temp = FaceVertices[0];
                    FaceVertices[0] = FaceVertices[1];
                    FaceVertices[1] = FaceVertices[2];
                    FaceVertices[2] = FaceVertices[3];
                    FaceVertices[3] = Temp;
                }*/

                Vector3 LeftEdge = FaceVertices[GetDFaceIndex(1)] - FaceVertices[GetDFaceIndex(0)];
                Vector3 RightEdge = FaceVertices[GetDFaceIndex(2)] - FaceVertices[GetDFaceIndex(3)];

                Int32 NumEdgeVertices = (1 << BSP_DispInfo[Index].Power) + 1;
                Single SubdivideScale = 1.0f / (NumEdgeVertices - 1);

                Single LightDeltaU = (1f) / (NumEdgeVertices - 1);
                Single LightDeltaV = (1f) / (NumEdgeVertices - 1);

                Vector3 LeftEdgeStep = LeftEdge * SubdivideScale;
                Vector3 RightEdgeStep = RightEdge * SubdivideScale;

                for (Int32 i = 0; i < NumEdgeVertices; i++)
                {
                    Vector3 LeftEnd = LeftEdgeStep * i;
                    LeftEnd += FaceVertices[GetDFaceIndex(0)];

                    Vector3 RightEnd = RightEdgeStep * i;
                    RightEnd += FaceVertices[GetDFaceIndex(3)];

                    Vector3 LeftRightSeg = RightEnd - LeftEnd;
                    Vector3 LeftRightStep = LeftRightSeg * SubdivideScale;

                    for (Int32 j = 0; j < NumEdgeVertices; j++)
                    {
                        Int32 DispVertIndex = BSP_DispInfo[Index].DispVertStart + (i * NumEdgeVertices + j);
                        dDispVert DispVertInfo = BSP_DispVerts[DispVertIndex];

                        Vector3 FlatVertex = LeftEnd + (LeftRightStep * j);
                        Vector3 DispVertex = DispVertInfo.Vec * (DispVertInfo.Dist * uLoader.WorldScale);
                        DispVertex += FlatVertex;

                        Single s = (Vector3.Dot(FlatVertex, tS) + MapFace.TexInfo.TextureVecs[0].w * uLoader.WorldScale) / (MapFace.TexData.View_Width * uLoader.WorldScale);
                        Single t = (Vector3.Dot(FlatVertex, tT) + MapFace.TexInfo.TextureVecs[1].w * uLoader.WorldScale) / (MapFace.TexData.View_Height * uLoader.WorldScale);
                        TextureCoordinates.Add(new Vector2(s, t));

                        Single l_s = (LightDeltaU * j * MapFace.LightMapW + 0.5f) / (MapFace.LightMapW + 1);
                        Single l_t = (LightDeltaV * i * MapFace.LightMapH + 0.5f) / (MapFace.LightMapH + 1);
                        LightmapCoordinates.Add(new Vector2(l_s, l_t));

                        VertColors.Add(new Color32(0, 0, 0, (Byte)(DispVertInfo.Alpha)));

                        //-DispVertex.y, DispVertex.z, DispVertex.x (Y - UP INVERTED |  LightmapDir: LEFT - BACK) -- BINGO!!! Need reverse array "DispIndices" (Triangles)
                        DispVertices.Add(new Vector3(-DispVertex.y, DispVertex.z, DispVertex.x));
                    }
                }

                for (Int32 i = 0; i < NumEdgeVertices - 1; i++)
                {
                    for (Int32 j = 0; j < NumEdgeVertices - 1; j++)
                    {
                        Int32 vIndex = i * NumEdgeVertices + j;

                        if ((vIndex % 2) == 1)
                        {
                            DispIndices.Add(vIndex);
                            DispIndices.Add(vIndex + 1);
                            DispIndices.Add(vIndex + NumEdgeVertices);
                            DispIndices.Add(vIndex + 1);
                            DispIndices.Add(vIndex + NumEdgeVertices + 1);
                            DispIndices.Add(vIndex + NumEdgeVertices);
                        }
                        else
                        {
                            DispIndices.Add(vIndex);
                            DispIndices.Add(vIndex + NumEdgeVertices + 1);
                            DispIndices.Add(vIndex + NumEdgeVertices);
                            DispIndices.Add(vIndex);
                            DispIndices.Add(vIndex + 1);
                            DispIndices.Add(vIndex + NumEdgeVertices + 1);
                        }
                    }
                }

                //DispIndices.Reverse();

                BSP_CDisp[Index] = new Face
                {
                    Vertices = DispVertices.ToArray(),
                    Triangles = DispIndices.ToArray(),

                    Colors = VertColors.ToArray(),

                    UV = TextureCoordinates.ToArray(),
                    UV2 = LightmapCoordinates.ToArray(),

                    LightOfs = MapFace.LightOfs,

                    LightMapW = MapFace.LightMapW,
                    LightMapH = MapFace.LightMapH
                };
            }
        }

        //TODO: Optimize lightmaps!
        static void CreateLightmap(IList<Face> InpFaces, ref Texture2D Lightmap_tex, ref List<Vector2> UV2)
        {
            Texture2D[] LMs = new Texture2D[InpFaces.Count];

            for (Int32 i = 0; i < InpFaces.Count; i++)
            {
                if (InpFaces[i].LightOfs == -1)
                    continue;

                LMs[i] = new Texture2D(InpFaces[i].LightMapW, InpFaces[i].LightMapH, TextureFormat.RGB24, false, true);
                Color32[] TexPixels = new Color32[LMs[i].width * LMs[i].height];

                for (Int32 j = 0; j < TexPixels.Length; j++)
                {
                    ColorRGBExp32 ColorRGBExp32 = uLoader.useGammaLighting ? TexLightToGamma(InpFaces[i].LightOfs + (j * 4)) : TexLightToLinear(InpFaces[i].LightOfs + (j * 4));
                    TexPixels[j] = new Color32(ColorRGBExp32.r, ColorRGBExp32.g, ColorRGBExp32.b, 255);
                }

                LMs[i].SetPixels32(TexPixels);
            }

            Rect[] UVs2 = Lightmap_tex.PackTextures(LMs, 1);
            for (Int32 i = 0; i < InpFaces.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(LMs[i]);

                for (Int32 j = 0; j < InpFaces[i].UV2.Length; j++)
                    UV2.Add(new Vector2((InpFaces[i].UV2[j].x * UVs2[i].width) + UVs2[i].x, (InpFaces[i].UV2[j].y * UVs2[i].height) + UVs2[i].y));
            }

            //Add lightmap in array
            if (!uLoader.useLightmapsAsTextureShader)
            {
                uLoader.lightmapsData.Add(new LightmapData() { lightmapColor = Lightmap_tex });
                LightmapSettings.lightmaps = uLoader.lightmapsData.ToArray();
                uLoader.CurrentLightmap++;
            }
            //Add lightmap in array
        }

        static ColorRGBExp32 TexLightToLinear(long Offset)
        {

            Offset += BSP_Header.Lumps[58].FileLen / 56 > 0 ? BSP_Header.Lumps[53].FileOfs : BSP_Header.Lumps[8].FileOfs;

            ColorRGBExp32 ColorRGBExp32 = new ColorRGBExp32();
            BSPFileReader.ReadType(ref ColorRGBExp32, Offset);

            float Pow = Mathf.Pow(2, ColorRGBExp32.exponent);

            Color32 col = new Color32(TexLightToLinearB(ColorRGBExp32.r, Pow), TexLightToLinearB(ColorRGBExp32.g, Pow), TexLightToLinearB(ColorRGBExp32.b, Pow), 255);
            ColorRGBExp32.r = col.r;
            ColorRGBExp32.g = col.g;
            ColorRGBExp32.b = col.b;

            return ColorRGBExp32;
        }

        static ColorRGBExp32 TexLightToGamma(long Offset)
        {

            Offset += BSP_Header.Lumps[58].FileLen / 56 > 0 ? BSP_Header.Lumps[53].FileOfs : BSP_Header.Lumps[8].FileOfs;

            ColorRGBExp32 ColorRGBExp32 = new ColorRGBExp32();
            BSPFileReader.ReadType(ref ColorRGBExp32, Offset);

            float Pow = Mathf.Pow(2, ColorRGBExp32.exponent);

            //https://github.com/lewa-j/Unity-Source-Tools/blob/834869c8ad7ad8924af62e11e9e55486e18203e8/Assets/Code/Read/BSPFile.cs#L337
            Color32 col = new Color(TexLightToLinearF(ColorRGBExp32.r, Pow), TexLightToLinearF(ColorRGBExp32.g, Pow), TexLightToLinearF(ColorRGBExp32.b, Pow), 0.25f).gamma;
            ColorRGBExp32.r = col.r;
            ColorRGBExp32.g = col.g;
            ColorRGBExp32.b = col.b;
            return ColorRGBExp32;
        }

        //https://github.com/lewa-j/Unity-Source-Tools/blob/834869c8ad7ad8924af62e11e9e55486e18203e8/Assets/Code/Read/BSPFile.cs#L350
        static byte TexLightToLinearB(byte c, float exponent)
        {
            return (byte)Mathf.Clamp(c * exponent * 0.5f, 0, 255);
        }

        //https://github.com/lewa-j/Unity-Source-Tools/blob/834869c8ad7ad8924af62e11e9e55486e18203e8/Assets/Code/Read/BSPFile.cs#L356
        static float TexLightToLinearF(byte c, float exponent)
        {
            return Mathf.Clamp((float)c * exponent * 0.5f, 0, 255) / 255.0f;
        }

        static void CreateSkybox(List<String> data)
        {
            String Base =
                "skybox/" + data[data.FindIndex(n => n == "skyname") + 1],
                LDR = Base.Replace("_hdr", "");

            String[] Sides = new[] { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex" };
            Material Material = new Material(Shader.Find("Mobile/Skybox"));

            /*foreach (String Side in new[] { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex" })
            {
                Material.SetTextureScale(Side, new Vector2(1, -1));
                Material.SetTextureOffset(Side, new Vector2(0, 1));
            }*/

            //Invert
            for (int i = 0; i < 5; i++)
            {
                Material.SetTextureScale(Sides[i], new Vector2(1, -1));
                Material.SetTextureOffset(Sides[i], new Vector2(0, 1));
            }

            Texture _FrontTex = uResourceManager.LoadTexture(LDR + "rt", Base + "rt")[0, 0];
            if (_FrontTex != null)
            {
                _FrontTex.wrapMode = TextureWrapMode.Clamp;
                Texture _BackTex = uResourceManager.LoadTexture(LDR + "lf", Base + "lf")[0, 0];
                _BackTex.wrapMode = TextureWrapMode.Clamp;
                Texture _LeftTex = uResourceManager.LoadTexture(LDR + "ft", Base + "ft")[0, 0];
                _LeftTex.wrapMode = TextureWrapMode.Clamp;
                Texture _RightTex = uResourceManager.LoadTexture(LDR + "bk", Base + "bk")[0, 0];
                _RightTex.wrapMode = TextureWrapMode.Clamp;
                Texture _UpTex = uResourceManager.LoadTexture(LDR + "up", Base + "up")[0, 0];
                _UpTex.wrapMode = TextureWrapMode.Clamp;
                Texture _DownTex = uResourceManager.LoadTexture(LDR + "dn", Base + "dn")[0, 0];
                _DownTex.wrapMode = TextureWrapMode.Clamp;

                Material.SetTexture(Sides[0], _FrontTex);
                Material.SetTexture(Sides[1], _BackTex);
                Material.SetTexture(Sides[2], _LeftTex);
                Material.SetTexture(Sides[3], _RightTex);
                Material.SetTexture(Sides[4], _UpTex);
                Material.SetTexture(Sides[5], _DownTex);
            }
            else
            {
                Material = new Material(Shader.Find("Mobile/Skybox"));//new Material(Shader.Find("Skybox/Procedural"));
            }

            RenderSettings.skybox = Material;
        }

        static void InitPAKLump()
        {
            if (BSP_Header.Lumps[40].FileLen > 0)
            {
                BSPFileReader.BaseStream.Seek(BSP_Header.Lumps[40].FileOfs, SeekOrigin.Begin);
                uResourceManager.Init(mainProvider: new PAKProvider(BSPFileReader.BaseStream));
            }
        }

        static void LoadStaticProps()
        {
            BSPFileReader.BaseStream.Seek(BSP_Header.Lumps[35].FileOfs, SeekOrigin.Begin);
            Int32 GameLumpCount = BSPFileReader.ReadInt32();

            dgamelump_t[] BSP_GameLump = new dgamelump_t[GameLumpCount];
            BSPFileReader.ReadArray(ref BSP_GameLump);

            for (Int32 i = 0; i < GameLumpCount; i++)
            {
                if (BSP_GameLump[i].Id == 1936749168)
                {
                    BSPFileReader.BaseStream.Seek(BSP_GameLump[i].FileOfs, SeekOrigin.Begin);

                    var Start = BSPFileReader.BaseStream.Position;

                    String[] ModelEntries = new String[BSPFileReader.ReadInt32()];
                    for (Int32 j = 0; j < ModelEntries.Length; j++)
                    {
                        ModelEntries[j] = new String(BSPFileReader.ReadChars(128)).Replace(".mdl", "");

                        if (ModelEntries[j].Contains('\0'))
                            ModelEntries[j] = ModelEntries[j].Split('\0')[0];
                    }

                    UInt16[] LeafEntries = new UInt16[BSPFileReader.ReadInt32()];
                    BSPFileReader.ReadArray(ref LeafEntries);

                    Int64 nStaticProps = BSPFileReader.ReadInt32();

                    //If there are no static props, stop iterating
                    if (nStaticProps <= 0)
                        return;

                    //REDxEYE "fix".
                    //Int32 StaticPropSize = 0;
                    //prop_size = (size -(reader.tell()-start))//self.prop_num
                    Int64 StaticPropSize = (BSP_GameLump[i].FileLen - (BSPFileReader.BaseStream.Position - Start)) / nStaticProps;

                    for (Int64 l = 0; l < nStaticProps; l++)
                    {
                        String StaticPropName = String.Empty;
                        Vector3 m_Origin = Vector3.zero;
                        Vector3 m_Angles = Vector3.zero;
                        Single m_UniformScale = 1f;

                        long StaticPropStart = BSPFileReader.BaseStream.Position;

                        switch (BSP_GameLump[i].Version)
                        {
                            case 11:
                                StaticPropLumpV11_t StaticPropLumpV11_t = new StaticPropLumpV11_t();
                                BSPFileReader.ReadType(ref StaticPropLumpV11_t);

                                StaticPropName = ModelEntries[StaticPropLumpV11_t.m_PropType];
                                m_Origin = MathLibrary.SwapY(StaticPropLumpV11_t.m_Origin) * uLoader.WorldScale;
                                m_Angles = new Vector3(StaticPropLumpV11_t.m_Angles.x, -StaticPropLumpV11_t.m_Angles.y, -StaticPropLumpV11_t.m_Angles.z);
                                //m_UniformScale = StaticPropLumpV11_t.m_UniformScale;
                                break;

                            default:
                                StaticPropLumpV4_t StaticPropLumpV4_t = new StaticPropLumpV4_t();
                                BSPFileReader.ReadType(ref StaticPropLumpV4_t);

                                StaticPropName = ModelEntries[StaticPropLumpV4_t.m_PropType];
                                m_Origin = MathLibrary.SwapY(StaticPropLumpV4_t.m_Origin) * uLoader.WorldScale;
                                m_Angles = new Vector3(StaticPropLumpV4_t.m_Angles.x, -StaticPropLumpV4_t.m_Angles.y, -StaticPropLumpV4_t.m_Angles.z);
                                break;
                        }

                        BSPFileReader.BaseStream.Seek(StaticPropStart + StaticPropSize, SeekOrigin.Begin);

                        Int64 CurrentPosition = BSPFileReader.BaseStream.Position;
                        Transform MdlTransform = uResourceManager.LoadModel(StaticPropName, uLoader.LoadAnims);
                        BSPFileReader.BaseStream.Position = CurrentPosition;

                        MdlTransform.position = m_Origin;
                        MdlTransform.eulerAngles = m_Angles;
                        MdlTransform.localRotation = MdlTransform.localRotation * Quaternion.Euler(0, 90, 0);
                        MdlTransform.localScale = new Vector3(m_UniformScale, m_UniformScale, m_UniformScale);
                        MdlTransform.parent = BSP_WorldSpawn.transform;
                    }
                }
            }
        }
    }
}
