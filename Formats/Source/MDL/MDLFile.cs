using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using uSource.MathLib;

namespace uSource.Formats.Source.MDL
{
    public class MDLFile : StudioStruct
    {
        public studiohdr_t MDL_Header;

        public String[] MDL_BoneNames;
        public mstudiobone_t[] MDL_StudioBones;

        //animations
        public mstudioseqdesc_t[] MDL_SeqDescriptions;
        public mstudioanimdesc_t[] MDL_AniDescriptions;

        public AniInfo[] Animations;
        public SeqInfo[] Sequences;
        //TODO
        //static mstudioevent_t[] MDL_Events;
        //animations

        //Materials
        public mstudiotexture_t[] MDL_TexturesInfo;
        public String[] MDL_TDirectories;
        public String[] MDL_Textures;
        //Materials

        mstudiohitboxset_t[] MDL_Hitboxsets;
        Hitbox[][] Hitboxes;

        public StudioBodyPart[] MDL_Bodyparts;
        public MDLFile(Stream FileInput, Boolean parseAnims = false, Boolean parseHitboxes = false)
        {
            using (uReader FileStream = new uReader(FileInput))
            {
                FileStream.ReadTypeFixed(ref MDL_Header, 392);

                if (MDL_Header.id != 0x54534449)
                    throw new FileLoadException("File signature does not match 'IDST'");

                //Bones
                MDL_StudioBones = new mstudiobone_t[MDL_Header.bone_count];
                MDL_BoneNames = new String[MDL_Header.bone_count];
                for (Int32 boneID = 0; boneID < MDL_Header.bone_count; boneID++)
                {
                    Int32 boneOffset = MDL_Header.bone_offset + (216 * boneID);
                    FileStream.ReadTypeFixed(ref MDL_StudioBones[boneID], 216, boneOffset);
                    MDL_BoneNames[boneID] = FileStream.ReadNullTerminatedString(boneOffset + MDL_StudioBones[boneID].sznameindex);
                }
                //Bones

                if (parseHitboxes)
                {
                    MDL_Hitboxsets = new mstudiohitboxset_t[MDL_Header.hitbox_count];
                    Hitboxes = new Hitbox[MDL_Header.hitbox_count][];
                    for (Int32 hitboxsetID = 0; hitboxsetID < MDL_Header.hitbox_count; hitboxsetID++)
                    {
                        Int32 hitboxsetOffset = MDL_Header.hitbox_offset + (12 * hitboxsetID);
                        FileStream.ReadTypeFixed(ref MDL_Hitboxsets[hitboxsetID], 12, hitboxsetOffset);
                        Hitboxes[hitboxsetID] = new Hitbox[MDL_Hitboxsets[hitboxsetID].numhitboxes];

                        for (Int32 hitboxID = 0; hitboxID < MDL_Hitboxsets[hitboxsetID].numhitboxes; hitboxID++)
                        {
                            Int32 hitboxOffset = hitboxsetOffset + (68 * hitboxID) + MDL_Hitboxsets[hitboxsetID].hitboxindex;
                            Hitboxes[hitboxsetID][hitboxID].BBox = new mstudiobbox_t();

                            FileStream.ReadTypeFixed(ref Hitboxes[hitboxsetID][hitboxID].BBox, 68, hitboxOffset);
                        }
                    }
                }

                if (parseAnims)
                {
                    try
                    {
                        //Animations
                        MDL_AniDescriptions = new mstudioanimdesc_t[MDL_Header.localanim_count];
                        Animations = new AniInfo[MDL_Header.localanim_count];

                        for (Int32 AnimID = 0; AnimID < MDL_Header.localanim_count; AnimID++)
                        {
                            Int32 AnimOffset = MDL_Header.localanim_offset + (100 * AnimID);
                            FileStream.ReadTypeFixed(ref MDL_AniDescriptions[AnimID], 100, AnimOffset);
                            mstudioanimdesc_t StudioAnim = MDL_AniDescriptions[AnimID];

                            String StudioAnimName = FileStream.ReadNullTerminatedString(AnimOffset + StudioAnim.sznameindex);
                            Animations[AnimID] = new AniInfo { name = StudioAnimName, studioAnim = StudioAnim };
                            Animations[AnimID].AnimationBones = new List<AnimationBone>();

                            //mstudioanim_t
                            FileStream.BaseStream.Position = AnimOffset;

                            Int64 StartOffset = FileStream.BaseStream.Position;

                            Int32 CurrentOffset = MDL_AniDescriptions[AnimID].animindex;
                            Int16 NextOffset;
                            do
                            {
                                FileStream.BaseStream.Position = StartOffset + CurrentOffset;
                                Byte BoneIndex = FileStream.ReadByte();
                                Byte BoneFlag = FileStream.ReadByte();
                                NextOffset = FileStream.ReadInt16();
                                CurrentOffset += NextOffset;

                                AnimationBone AnimatedBone = new AnimationBone(BoneIndex, BoneFlag, MDL_AniDescriptions[AnimID].numframes);
                                AnimatedBone.ReadData(FileStream);
                                Animations[AnimID].AnimationBones.Add(AnimatedBone);

                            } while (NextOffset != 0);
                            //mstudioanim_t

                            List<AnimationBone> AnimationBones = Animations[AnimID].AnimationBones;
                            Int32 NumBones = MDL_Header.bone_count;
                            Int32 NumFrames = StudioAnim.numframes;

                            //Used to avoid "Assertion failed" key count in Unity (if frames less than 2)
                            Boolean FramesLess = NumFrames < 2;
                            if (FramesLess)
                                NumFrames += 1;

                            Animations[AnimID].PosX = new Keyframe[NumFrames][];
                            Animations[AnimID].PosY = new Keyframe[NumFrames][];
                            Animations[AnimID].PosZ = new Keyframe[NumFrames][];

                            Animations[AnimID].RotX = new Keyframe[NumFrames][];
                            Animations[AnimID].RotY = new Keyframe[NumFrames][];
                            Animations[AnimID].RotZ = new Keyframe[NumFrames][];
                            Animations[AnimID].RotW = new Keyframe[NumFrames][];
                            for (Int32 FrameID = 0; FrameID < NumFrames; FrameID++)
                            {
                                Animations[AnimID].PosX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosZ[FrameID] = new Keyframe[NumBones];

                                Animations[AnimID].RotX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotZ[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotW[FrameID] = new Keyframe[NumBones];
                            }

                            for (Int32 boneID = 0; boneID < NumBones; boneID++)
                            {
                                AnimationBone AnimBone = AnimationBones.FirstOrDefault(x => x.Bone == boneID);

                                //frameIndex < 30 && studioAnimName == "@ak47_reload"
                                for (Int32 frameID = 0; frameID < NumFrames; frameID++)
                                {
                                    //get current animation time (length) by divide frame index on "fps"
                                    Single time = frameID / StudioAnim.fps;

                                    mstudiobone_t StudioBone = MDL_StudioBones[boneID];
                                    //Transform bone = Bones[boneIndex];

                                    Vector3 Position = StudioBone.pos;
                                    Vector3 Rotation = StudioBone.rot;

                                    //BINGO! All animations are corrected :p
                                    if (AnimBone != null)
                                    {
                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWROT) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat48);

                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWROT2) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat64);

                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWPOS) > 0)
                                            Position = AnimBone.pVec48;

                                        if ((AnimBone.Flags & STUDIO_ANIM_ANIMROT) > 0)
                                            Rotation += AnimBone.FrameAngles[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.rotscale);

                                        if ((AnimBone.Flags & STUDIO_ANIM_ANIMPOS) > 0)
                                            Position += AnimBone.FramePositions[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.posscale);

                                        if ((AnimBone.Flags & STUDIO_ANIM_DELTA) > 0)
                                        {
                                            Position = Vector3.zero;
                                            Rotation = Vector3.zero;
                                        }
                                    }

                                    //Invert right-handed position to left-handed
                                    if (StudioBone.parent == -1)
                                        Position = MathLibrary.SwapY(Position);
                                    else
                                        Position.x = -Position.x;

                                    //Corrects global scale and convert radians to degrees
                                    Position *= uLoader.UnitScale;
                                    Rotation *= Mathf.Rad2Deg;
                                    Quaternion quat;

                                    //Fix up bone rotations from right-handed to left-handed
                                    if (StudioBone.parent == -1)
                                        quat = Quaternion.Euler(-90, 180, -90) * MathLibrary.AngleQuaternion(Rotation);
                                    else
                                        quat = MathLibrary.AngleQuaternion(Rotation);

                                    Animations[AnimID].PosX[frameID][boneID] = new Keyframe(time, Position.x);
                                    Animations[AnimID].PosY[frameID][boneID] = new Keyframe(time, Position.y);
                                    Animations[AnimID].PosZ[frameID][boneID] = new Keyframe(time, Position.z);

                                    Animations[AnimID].RotX[frameID][boneID] = new Keyframe(time, quat.x);
                                    Animations[AnimID].RotY[frameID][boneID] = new Keyframe(time, quat.y);
                                    Animations[AnimID].RotZ[frameID][boneID] = new Keyframe(time, quat.z);
                                    Animations[AnimID].RotW[frameID][boneID] = new Keyframe(time, quat.w);
                                }
                            }
                        }
                        //Animations

                        //Sequences
                        MDL_SeqDescriptions = new mstudioseqdesc_t[MDL_Header.localseq_count];
                        Sequences = new SeqInfo[MDL_Header.localseq_count];

                        for (Int32 seqID = 0; seqID < MDL_Header.localseq_count; seqID++)
                        {
                            Int32 sequenceOffset = MDL_Header.localseq_offset + (212 * seqID);
                            FileStream.ReadTypeFixed(ref MDL_SeqDescriptions[seqID], 212, sequenceOffset);
                            mstudioseqdesc_t Sequence = MDL_SeqDescriptions[seqID];
                            Sequences[seqID] = new SeqInfo { name = FileStream.ReadNullTerminatedString(sequenceOffset + Sequence.szlabelindex), seq = Sequence };

                            FileStream.BaseStream.Position = sequenceOffset + Sequence.animindexindex;

                            var animID = FileStream.ReadShortArray(Sequence.groupsize[0] * Sequence.groupsize[1]);
                            //Debug.LogWarning(animIndices[0]);
                            // Just use the first animation for now
                            Sequences[seqID].ani = Animations[animID[0]];
                        }
                        //Sequences
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(String.Format("\"{0}\" Parse animation failed: {1}", MDL_Header.Name, ex));
                    }
                }

                //Materials
                MDL_TexturesInfo = new mstudiotexture_t[MDL_Header.texture_count];
                MDL_Textures = new String[MDL_Header.texture_count];
                for (Int32 texID = 0; texID < MDL_Header.texture_count; texID++)
                {
                    Int32 textureOffset = MDL_Header.texture_offset + (64 * texID);
                    FileStream.ReadTypeFixed(ref MDL_TexturesInfo[texID], 64, textureOffset);
                    MDL_Textures[texID] = FileStream.ReadNullTerminatedString(textureOffset + MDL_TexturesInfo[texID].sznameindex);
                }

                Int32[] TDirOffsets = new Int32[MDL_Header.texturedir_count];
                MDL_TDirectories = new String[MDL_Header.texturedir_count];
                for (Int32 dirID = 0; dirID < MDL_Header.texturedir_count; dirID++)
                {
                    FileStream.ReadTypeFixed(ref TDirOffsets[dirID], 4, MDL_Header.texturedir_offset + (4 * dirID));
                    MDL_TDirectories[dirID] = FileStream.ReadNullTerminatedString(TDirOffsets[dirID]);//.Replace("\\", "/");
                }
                //Materials

                //Bodyparts
                MDL_Bodyparts = new StudioBodyPart[MDL_Header.bodypart_count];
                for (Int32 bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
                {
                    mstudiobodyparts_t pBodypart = new mstudiobodyparts_t();
                    Int32 pBodypartOffset = MDL_Header.bodypart_offset + (16 * bodypartID);
                    FileStream.ReadTypeFixed(ref pBodypart, 16, pBodypartOffset);

                    if (pBodypart.sznameindex != 0)
                        MDL_Bodyparts[bodypartID].Name = FileStream.ReadNullTerminatedString(pBodypartOffset + pBodypart.sznameindex);
                    else
                        MDL_Bodyparts[bodypartID].Name = String.Empty;

                    MDL_Bodyparts[bodypartID].Models = new StudioModel[pBodypart.nummodels];

                    for (Int32 modelID = 0; modelID < pBodypart.nummodels; modelID++)
                    {
                        mstudiomodel_t pModel = new mstudiomodel_t();
                        Int64 pModelOffset = pBodypartOffset + (148 * modelID) + pBodypart.modelindex;
                        FileStream.ReadTypeFixed(ref pModel, 148, pModelOffset);

                        MDL_Bodyparts[bodypartID].Models[modelID].isBlank = (pModel.numvertices <= 0 || pModel.nummeshes <= 0);
                        MDL_Bodyparts[bodypartID].Models[modelID].Model = pModel;

                        //TODO: 
                        //props/de_aztec/hr_aztec/aztec_stairs/aztec_stair_02_edge_64wide_footer_03.mdl 
                        //props/de_aztec/hr_aztec/aztec_walls/aztec_wall_stone01_bridge_foundation_02.mdl - no vertexes, why? D:
                        MDL_Bodyparts[bodypartID].Models[modelID].Meshes = new mstudiomesh_t[pModel.nummeshes];
                        for (Int32 meshID = 0; meshID < pModel.nummeshes; meshID++)
                        {
                            mstudiomesh_t pMesh = new mstudiomesh_t();
                            Int64 pMeshOffset = pModelOffset + (116 * meshID) + pModel.meshindex;
                            FileStream.ReadTypeFixed(ref pMesh, 116, pMeshOffset);

                            //TODO: Temp fix, if "no vertexes" on model (but model has vertexes... it's didn't better way to fix.. but works now)
                            if (pMesh.VertexData.numlodvertices[0] == 0)
                                pMesh.VertexData.numlodvertices[0] = pMesh.numvertices;

                            MDL_Bodyparts[bodypartID].Models[modelID].Meshes[meshID] = pMesh;
                        }

                        MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod = new Dictionary<Int32, List<Int32>>[8];

                        for (Int32 i = 0; i < 8; i++)
                            MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod[i] = new Dictionary<Int32, List<Int32>>();

                        MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod = new mstudiovertex_t[8][];
                    }
                }
                //BodyParts
            }
        }

        public void SetIndices(Int32 BodypartID, Int32 ModelID, Int32 LODID, Int32 MeshID, List<Int32> Indices)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].IndicesPerLod[LODID].Add(MeshID, Indices);
        }

        public void SetVertices(Int32 BodypartID, Int32 ModelID, Int32 LODID, Int32 TotalVerts, Int32 StartIndex, mstudiovertex_t[] Vertexes)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID] = new mstudiovertex_t[TotalVerts];
            Array.Copy(Vertexes, StartIndex, MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID], 0, TotalVerts);
        }

        public Boolean meshExist = true;
        public Transform BuildModel(Boolean GenerateUV2 = false)
        {
            GameObject ModelObject = new GameObject(MDL_Header.Name);

            Transform[] Bones = new Transform[MDL_Header.bone_count];
            Dictionary<Int32, String> bonePathDict = new Dictionary<Int32, String>();
            for (Int32 boneID = 0; boneID < MDL_Header.bone_count; boneID++)
            {
                GameObject BoneObject = new GameObject(MDL_BoneNames[boneID]);

                Bones[boneID] = BoneObject.transform;//MDL_Bones.Add(BoneObject.transform);

                Vector3 pos = MDL_StudioBones[boneID].pos * uLoader.UnitScale;
                Vector3 rot = MDL_StudioBones[boneID].rot * Mathf.Rad2Deg;

                //Invert x for convert right-handed to left-handed
                pos.x = -pos.x;

                if (MDL_StudioBones[boneID].parent >= 0)
                {
                    Bones[boneID].parent = Bones[MDL_StudioBones[boneID].parent];
                }
                else
                {
                    //Swap Z & Y and invert Y (ex: X Z -Y)
                    //only for parents, cuz parents used different order vectors
                    float temp = pos.y;
                    pos.y = pos.z;
                    pos.z = -temp;

                    Bones[boneID].parent = ModelObject.transform;
                }

                bonePathDict.Add(boneID, Bones[boneID].GetTransformPath(ModelObject.transform));

                Bones[boneID].localPosition = pos;
                //Bones[i].localRotation = MDL_StudioBones[i].quat;

                if (MDL_StudioBones[boneID].parent == -1)
                {
                    //Fix up parents
                    Bones[boneID].localRotation = Quaternion.Euler(-90, 90, -90) * MathLibrary.AngleQuaternion(rot);
                }
                else
                    Bones[boneID].localRotation = MathLibrary.AngleQuaternion(rot);
            }

            if (uLoader.DrawArmature)
            {
                MDLArmatureInfo DebugArmature = ModelObject.AddComponent<MDLArmatureInfo>();
                DebugArmature.boneNodes = Bones;
            }

            if (MDL_Hitboxsets != null)
            {
                for (Int32 hitboxsetID = 0; hitboxsetID < MDL_Header.hitbox_count; hitboxsetID++)
                {
                    for (Int32 hitboxID = 0; hitboxID < MDL_Hitboxsets[hitboxsetID].numhitboxes; hitboxID++)
                    {
                        mstudiobbox_t hitbox = Hitboxes[hitboxsetID][hitboxID].BBox;
                        BoxCollider bbox = new GameObject(String.Format("Hitbox_{0}", Bones[hitbox.bone].name)).AddComponent<BoxCollider>();

                        bbox.size = MathLibrary.NegateX(hitbox.bbmax - hitbox.bbmin) * uLoader.UnitScale;
                        bbox.center = (MathLibrary.NegateX(hitbox.bbmax + hitbox.bbmin) / 2) * uLoader.UnitScale;

                        bbox.transform.parent = Bones[hitbox.bone];
                        bbox.transform.localPosition = Vector3.zero;
                        bbox.transform.localRotation = Quaternion.identity;

                        //bbox.transform.tag = HitTagType(MDL_BBoxes[i].group);
                    }
                }
            }

            if (meshExist)
            {
                for (Int32 bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
                {
                    StudioBodyPart BodyPart = MDL_Bodyparts[bodypartID];

                    for (Int32 modelID = 0; modelID < BodyPart.Models.Length; modelID++)
                    {
                        StudioModel Model = BodyPart.Models[modelID];

                        //Skip if model is blank
                        if (Model.isBlank)
                            continue;

                        for (Int32 lodID = 0; lodID < Model.NumLODs; lodID++)
                        {
                            mstudiovertex_t[] Vertexes = Model.VerticesPerLod[lodID];

                            BoneWeight[] pBoneWeight = new BoneWeight[Vertexes.Length];
                            Vector3[] pVertices = new Vector3[Vertexes.Length];
                            Vector3[] pNormals = new Vector3[Vertexes.Length];
                            Vector2[] pUvBuffer = new Vector2[Vertexes.Length];

                            for (Int32 i = 0; i < Vertexes.Length; i++)
                            {
                                pVertices[i] = MathLibrary.SwapZY(Vertexes[i].m_vecPosition * uLoader.UnitScale);
                                pNormals[i] = MathLibrary.SwapZY(Vertexes[i].m_vecNormal);

                                Vector2 UV = Vertexes[i].m_vecTexCoord;
                                if (uLoader.SaveAssetsToUnity && uLoader.ExportTextureAsPNG)
                                    UV.y = -UV.y;

                                pUvBuffer[i] = UV;
                                pBoneWeight[i] = GetBoneWeight(Vertexes[i].m_BoneWeights);
                            }

                            GameObject MeshObject = new GameObject(Model.Model.Name);
                            MeshObject.name += "_vLOD" + lodID;
                            MeshObject.transform.parent = ModelObject.transform;

                            Mesh pMesh = new Mesh();
                            pMesh.name = MeshObject.name;
                            pMesh.subMeshCount = Model.Model.nummeshes;

                            pMesh.vertices = pVertices;

                            if (pMesh.vertexCount <= 0)
                            {
                                Debug.LogWarning(String.Format("Mesh: \"{0}\" has no vertexes, skip building... (MDL Version: {1})", pMesh.name, MDL_Header.version));
                                continue;
                            }

                            pMesh.normals = pNormals;
                            pMesh.uv = pUvBuffer;

                            Renderer Renderer;
                            if (!MDL_Header.flags.HasFlag(StudioHDRFlags.STUDIOHDR_FLAGS_STATIC_PROP))
                            {
                                SkinnedMeshRenderer SkinnedRenderer = MeshObject.AddComponent<SkinnedMeshRenderer>();
                                Renderer = SkinnedRenderer;
                                Matrix4x4[] BindPoses = new Matrix4x4[Bones.Length];

                                for (Int32 i = 0; i < BindPoses.Length; i++)
                                    BindPoses[i] = Bones[i].worldToLocalMatrix * MeshObject.transform.localToWorldMatrix;

                                pMesh.boneWeights = pBoneWeight;
                                pMesh.bindposes = BindPoses;

                                SkinnedRenderer.sharedMesh = pMesh;

                                SkinnedRenderer.bones = Bones;
                                SkinnedRenderer.updateWhenOffscreen = true;
                            }
                            else
                            {
                                MeshFilter MeshFilter = MeshObject.AddComponent<MeshFilter>();
                                Renderer = MeshObject.AddComponent<MeshRenderer>();
                                MeshFilter.sharedMesh = pMesh;
                            }

                            #if UNITY_EDITOR
                            VTF.DebugMaterial DebugMat = null;
                            if (uLoader.DebugMaterials)
                                DebugMat = MeshObject.AddComponent<VTF.DebugMaterial>();
                            #endif

                            Material[] pMaterials = new Material[pMesh.subMeshCount];

                            for (Int32 meshID = 0; meshID < Model.Model.nummeshes; meshID++)
                            {
                                pMesh.SetTriangles(Model.IndicesPerLod[lodID][meshID], meshID);

                                String MaterialPath;
                                for (Int32 DirID = 0; DirID < MDL_TDirectories.Length; DirID++)
                                {
                                    MaterialPath = MDL_TDirectories[DirID] + MDL_Textures[Model.Meshes[meshID].material];

                                    if (uResourceManager.ContainsFile(MaterialPath, uResourceManager.MaterialsSubFolder, uResourceManager.MaterialsExtension[0]))
                                    {
                                        VTF.VMTFile VMT = uResourceManager.LoadMaterial(MaterialPath);

                                        #if UNITY_EDITOR
                                        if (uLoader.DebugMaterials)
                                            DebugMat.Init(VMT);
                                        #endif

                                        pMaterials[meshID] = VMT.Material;
                                        break;
                                    }
                                    //else if (j == MDL_TDirectories.Length - 1)
                                    //    pMaterials[matID] = ResourceManager.LoadMaterial(String.Empty).Material;
                                }
                            }

#if UNITY_EDITOR
                            if (GenerateUV2)
                            {
                                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(Renderer);
                                so.FindProperty("m_ScaleInLightmap").floatValue = uLoader.ModelsLightmapSize;
                                so.ApplyModifiedProperties();

                                MeshObject.isStatic = GenerateUV2;
                                uResourceManager.UV2GenerateCache.Add(pMesh);
                            }

#endif
                            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                            Renderer.sharedMaterials = pMaterials;
                        }//lod's per model
                    }//models in bodypart
                }//Bodypart
            }

            if (MDL_SeqDescriptions != null)
            {
                var AnimationComponent = ModelObject.AddComponent<Animation>();
                for (Int32 seqID = 0; seqID < MDL_SeqDescriptions.Length; seqID++)
                {
                    SeqInfo Sequence = Sequences[seqID];
                    AniInfo Animation = Sequence.ani;

                    //Creating "AnimationCurve" for animation "paths" (aka frames where stored position (XYZ) & rotation (XYZW))
                    AnimationCurve[] posX = new AnimationCurve[MDL_Header.bone_count];    //X
                    AnimationCurve[] posY = new AnimationCurve[MDL_Header.bone_count];    //Y
                    AnimationCurve[] posZ = new AnimationCurve[MDL_Header.bone_count];    //Z

                    AnimationCurve[] rotX = new AnimationCurve[MDL_Header.bone_count];    //X
                    AnimationCurve[] rotY = new AnimationCurve[MDL_Header.bone_count];    //Y
                    AnimationCurve[] rotZ = new AnimationCurve[MDL_Header.bone_count];    //Z
                    AnimationCurve[] rotW = new AnimationCurve[MDL_Header.bone_count];    //W

                    //Fill "AnimationCurve" arrays
                    for (Int32 boneIndex = 0; boneIndex < MDL_Header.bone_count; boneIndex++)
                    {
                        posX[boneIndex] = new AnimationCurve();
                        posY[boneIndex] = new AnimationCurve();
                        posZ[boneIndex] = new AnimationCurve();

                        rotX[boneIndex] = new AnimationCurve();
                        rotY[boneIndex] = new AnimationCurve();
                        rotZ[boneIndex] = new AnimationCurve();
                        rotW[boneIndex] = new AnimationCurve();
                    }

                    Int32 numFrames = Animation.studioAnim.numframes;

                    //Used to avoid "Assertion failed" key count in Unity (if frames less than 2)
                    if (numFrames < 2)
                        numFrames += 1;

                    //Create animation clip
                    AnimationClip clip = new AnimationClip();
                    //Make it for legacy animation system (for now, but it possible to rework for Mecanim)
                    clip.legacy = true;
                    //Set animation clip name
                    clip.name = Animation.name;

                    //To avoid problems with "obfuscators" / "protectors" for models, make sure if model have name in sequence
                    if (String.IsNullOrEmpty(clip.name))
                        clip.name = "(empty)" + seqID;

                    for (Int32 frameIndex = 0; frameIndex < numFrames; frameIndex++)
                    {
                        //Get current frame from blend (meaning from "Animation") by index
                        //AnimationFrame frame = Animation.Frames[frameIndex];

                        //Set keys (position / rotation) from current frame
                        for (Int32 boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                        {
                            posX[boneIndex].AddKey(Animation.PosX[frameIndex][boneIndex]);
                            posY[boneIndex].AddKey(Animation.PosY[frameIndex][boneIndex]);
                            posZ[boneIndex].AddKey(Animation.PosZ[frameIndex][boneIndex]);

                            rotX[boneIndex].AddKey(Animation.RotX[frameIndex][boneIndex]);
                            rotY[boneIndex].AddKey(Animation.RotY[frameIndex][boneIndex]);
                            rotZ[boneIndex].AddKey(Animation.RotZ[frameIndex][boneIndex]);
                            rotW[boneIndex].AddKey(Animation.RotW[frameIndex][boneIndex]);

                            //Set default pose from the first animation
                            if (seqID == 0 && frameIndex == 0)
                            {
                                Bones[boneIndex].localPosition = new Vector3
                                (
                                    Animation.PosX[0][boneIndex].value,
                                    Animation.PosY[0][boneIndex].value,
                                    Animation.PosZ[0][boneIndex].value
                                );

                                Bones[boneIndex].localRotation = new Quaternion
                                (
                                    Animation.RotX[0][boneIndex].value,
                                    Animation.RotY[0][boneIndex].value,
                                    Animation.RotZ[0][boneIndex].value,
                                    Animation.RotW[0][boneIndex].value
                                );
                            }
                        }
                    }

                    //Apply animation paths (Position / Rotation) to clip
                    for (Int32 boneIndex = 0; boneIndex < MDL_Header.bone_count; boneIndex++)
                    {
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.x", posX[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.y", posY[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.z", posZ[boneIndex]);

                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.x", rotX[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.y", rotY[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.z", rotZ[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.w", rotW[boneIndex]);
                    }

                    if (Animation.studioAnim.fps > 0.0f)
                        clip.frameRate = Animation.studioAnim.fps;

                    //This ensures a smooth interpolation (corrects the problem of "jitter" after 180~270 degrees rotation path)
                    //can be "comment" if have idea how to replace this
                    clip.EnsureQuaternionContinuity();
                    AnimationComponent.AddClip(clip, clip.name);
                }
            }

            //If model has compiled flag "$staticprop"
            //then rotate this model by 90 degrees (Y)
            //https://github.com/ValveSoftware/source-sdk-2013/blob/master/sp/src/public/studio.h#L1965
            //Big thanks for this tip: 
            //ShadelessFox
            //REDxEYE
            if (MDL_Header.flags.HasFlag(StudioHDRFlags.STUDIOHDR_FLAGS_STATIC_PROP))
                ModelObject.transform.eulerAngles = new Vector3(0, 90, 0);

            return ModelObject.transform;
        }

        static String HitTagType(Int32 typeHit)
        {
            String returnType;
            switch (typeHit)
            {
                case 1: // - Used for human NPC heads and to define where the player sits on the vehicle.mdl, appears Red in HLMV
                    returnType = "Head";
                    break;

                case 2: // - Used for human NPC midsection and chest, appears Green in HLMV
                    returnType = "Chest";
                    break;

                case 3: // - Used for human NPC stomach and pelvis, appears Yellow in HLMV
                    returnType = "Stomach";
                    break;

                case 4: // - Used for human Left Arm, appears Deep Blue in HLMV
                    returnType = "Left_Arm";
                    break;

                case 5: // - Used for human Right Arm, appears Bright Violet in HLMV
                    returnType = "Right_Arm";
                    break;

                case 6: // - Used for human Left Leg, appears Bright Cyan in HLMV
                    returnType = "Left_Leg";
                    break;

                case 7: // - Used for human Right Leg, appears White like the default group in HLMV
                    returnType = "Right_Leg";
                    break;

                case 8: // - Used for human neck (to fix penetration to head from behind), appears Orange in HLMV (in all games since Counter-Strike: Global Offensive)
                    returnType = "Neck";
                    break;

                default: // - the default group of hitboxes, appears White in HLMV
                    returnType = "Generic";
                    break;
            }
            return returnType;
        }

        BoneWeight GetBoneWeight(mstudioboneweight_t mBoneWeight)
        {
            BoneWeight boneWeight = new BoneWeight();

            boneWeight.boneIndex0 = mBoneWeight.bone[0];
            boneWeight.boneIndex1 = mBoneWeight.bone[1];
            boneWeight.boneIndex2 = mBoneWeight.bone[2];

            boneWeight.weight0 = mBoneWeight.weight[0];
            boneWeight.weight1 = mBoneWeight.weight[1];
            boneWeight.weight2 = mBoneWeight.weight[2];

            return boneWeight;
        }
    }
}