using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
//using Crowbar;

namespace Engine.Source
{
    public class StudioMDLLoader : MdlSpec
    {
        static MemUtils ModelFileLoader;

        static studiohdr_t MDL_Header;
		static mstudiobodyparts_t[] MDL_BodyParts;
        static mstudiomodel_t[] MDL_Models;
        static mstudiomesh_t[] MDL_Meshes;

        static String[] MDL_TDirectories;
        static String[] MDL_Textures;

        static List<Transform> MDL_Bones;

        static vertexFileHeader_t VVD_Header;
        static List<mstudiovertex_t> VVD_Vertexes;
        static vertexFileFixup_t[] VVD_Fixups;

        static FileHeader_t VTX_Header;
        static MeshHeader_t[] VTX_Meshes;

        static BodyPartHeader_t vBodypart;
        static ModelHeader_t vModel;
        static ModelLODHeader_t vLod;

        static GameObject ModelObject;
		static MDLArmatureInfo BonesInfo;
		public static Dictionary<string, Transform> ModelsInRAM;

        static void Clear()
        {
            MDL_Bones = new List<Transform>();
            VVD_Vertexes = new List<mstudiovertex_t>();
        }

		public static Transform Load(String ModelName)
        {
            Clear();

            if (ModelsInRAM == null)
                ModelsInRAM = new Dictionary<string, Transform>();

            String OpenPath = String.Empty;

            ModelName = ModelName
                .Replace(".mdl", "")
                .Replace("models/", "");

            if (ModelsInRAM.ContainsKey(ModelName))
                return UnityEngine.Object.Instantiate(ModelsInRAM[ModelName]);

            for (Int32 i = 0; i < ConfigLoader.ModFolders.Length; i++)
            {
                if (File.Exists(ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/models/" + ModelName + ".mdl") && !ConfigLoader.VpkUse)
                    OpenPath = ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/models/" + ModelName;
                else if(File.Exists(ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + ConfigLoader.VpkName + ".vpk") && ConfigLoader.VpkUse)
                    OpenPath = ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + ConfigLoader.VpkName + ".vpk";
            }

            ModelObject = new GameObject(ModelName);


			if (!File.Exists(OpenPath + ".mdl"))
            {
                Debug.Log(String.Format("{0}: File not found", ModelName + ".mdl"));
				return Load("error");
                //return ModelObject.transform;
            }

            if (!File.Exists(OpenPath + ".vvd"))
            {
                Debug.Log(String.Format("{0}: File not found", ModelName + ".vvd"));
                return ModelObject.transform;
            }

            if (!File.Exists(OpenPath + ".dx90.vtx"))
            {
                Debug.Log(String.Format("{0}: File not found", ModelName + ".dx90.vtx"));
                return ModelObject.transform;
            }

            try
            {
                ModelFileLoader = new MemUtils(File.OpenRead(OpenPath + ".mdl"));
                ModelFileLoader.ReadType(ref MDL_Header);
                ParseMdlFile();

                ModelFileLoader = new MemUtils(File.OpenRead(OpenPath + ".vvd"));
                ModelFileLoader.ReadType(ref VVD_Header);
                ParseVvdFile();

                ModelFileLoader = new MemUtils(File.OpenRead(OpenPath + ".dx90.vtx"));
                ModelFileLoader.ReadType(ref VTX_Header);
                ParseVtxFile();
            }
            catch (Exception ErrorInfo)
            {
                Debug.LogError(ErrorInfo.ToString());
                return ModelObject.transform;
            }

			if(ConfigLoader.DrawArmature)
			{
				BonesInfo = ModelObject.AddComponent<MDLArmatureInfo>();
				DrawArmature();
			}

            ModelsInRAM.Add(ModelName, ModelObject.transform);
            return ModelObject.transform;
        }

		static void ParseMdlFile()
		{
			if (MDL_Header.id != 0x54534449)
				throw new FileLoadException(String.Format("{0}: File signature does not match 'IDST'", ModelObject.name + ".mdl"));

			MDL_BodyParts = new mstudiobodyparts_t[MDL_Header.bodypart_count];
			ModelFileLoader.ReadArray(ref MDL_BodyParts, MDL_Header.bodypart_offset);

			Int32 ModelInputFilePosition = MDL_Header.bodypart_offset + MDL_BodyParts[0].modelindex;
			MDL_Models = new mstudiomodel_t[MDL_BodyParts[0].nummodels];
			ModelFileLoader.ReadArray(ref MDL_Models, ModelInputFilePosition);

			Int32 MeshInputFilePosition = ModelInputFilePosition + MDL_Models[0].meshindex;
			MDL_Meshes = new mstudiomesh_t[MDL_Models[0].nummeshes];
			ModelFileLoader.ReadArray(ref MDL_Meshes, MeshInputFilePosition);

			mstudiotexture_t[] MDL_TexturesInfo = new mstudiotexture_t[MDL_Header.texture_count];
			ModelFileLoader.ReadArray(ref MDL_TexturesInfo, MDL_Header.texture_offset);

			MDL_Textures = new String[MDL_Header.texture_count];
			for (Int32 i = 0; i < MDL_Header.texture_count; i++)
			{
				Int32 StringInputFilePosition = MDL_Header.texture_offset + (Marshal.SizeOf(typeof(mstudiotexture_t)) * i) + MDL_TexturesInfo[i].sznameindex;
				MDL_Textures[i] = ModelFileLoader.ReadNullTerminatedString(StringInputFilePosition);
			}

			Int32[] TDirOffsets = new Int32[MDL_Header.texturedir_count];
			ModelFileLoader.ReadArray(ref TDirOffsets, MDL_Header.texturedir_offset);

			MDL_TDirectories = new String[MDL_Header.texturedir_count];
			for (Int32 i = 0; i < MDL_Header.texturedir_count; i++)
				MDL_TDirectories[i] = ModelFileLoader.ReadNullTerminatedString(TDirOffsets[i]);

			mstudiobone_t[] MDL_BonesInfo = new mstudiobone_t[MDL_Header.bone_count];
			ModelFileLoader.ReadArray(ref MDL_BonesInfo, MDL_Header.bone_offset);

			for (Int32 i = 0; i < MDL_Header.bone_count; i++)
			{
				Int32 StringInputFilePosition = MDL_Header.bone_offset + (Marshal.SizeOf(typeof(mstudiobone_t)) * i) + MDL_BonesInfo[i].sznameindex;

				GameObject BoneObject = new GameObject(ModelFileLoader.ReadNullTerminatedString(StringInputFilePosition));
				BoneObject.transform.parent = ModelObject.transform;

				MDL_Bones.Add(BoneObject.transform);

                // WIP - It works incorrectly (nearly)
                if (MDL_BonesInfo[i].parent >= 0)
                {
                    MDL_Bones[i].parent = MDL_Bones[MDL_BonesInfo[i].parent];
                }
                else
                    MDL_Bones[i].transform.parent = ModelObject.transform;

                Vector3 BonePosition = Vector3.zero;
                BonePosition = new Vector3(MDL_BonesInfo[i].pos.x, MDL_BonesInfo[i].pos.z, MDL_BonesInfo[i].pos.y);
                MDL_Bones[i].transform.localPosition = BonePosition * ConfigLoader.WorldScale;

                Vector3 RotationBone = new Vector3(-MDL_BonesInfo[i].rot.x * Mathf.Rad2Deg, -MDL_BonesInfo[i].rot.z * Mathf.Rad2Deg, -MDL_BonesInfo[i].rot.y * Mathf.Rad2Deg);
                MDL_Bones[i].transform.localEulerAngles = RotationBone;
            }
		}

		static void DrawArmature()
		{
			mstudiobone_t[] MDL_BonesInfo = new mstudiobone_t[MDL_Header.bone_count];
			for (Int32 i = 0; i < MDL_Bones.Count; i++)
			{
				if (ConfigLoader.DrawArmature && MDL_Header.bone_count == MDL_Bones.Count)
				{
					BonesInfo.ModelObject = ModelObject;
					BonesInfo.rootNode = MDL_Bones[0];
					BonesInfo.PopulateArmature();
				}
			}
		}

		static void ParseVtxFile()
        {
            if (VTX_Header.checkSum != MDL_Header.checksum)
                throw new FileLoadException(String.Format("{0}: Does not match the checksum in the .mdl", ModelObject.name + ".dx90.vtx"));

            mstudiomodel_t pModel = MDL_Models[0];
            mstudiomesh_t pStudioMesh;

            BoneWeight[] pBoneWeight = new BoneWeight[pModel.numvertices];
            Vector3[] pVertices = new Vector3[pModel.numvertices];
            Vector3[] pNormals = new Vector3[pModel.numvertices];
            Vector2[] pUvBuffer = new Vector2[pModel.numvertices];

            List<Material> pMaterials = new List<Material>();

            ModelFileLoader.ReadType(ref vBodypart, VTX_Header.bodyPartOffset);

            Int32 ModelInputFilePosition = VTX_Header.bodyPartOffset + vBodypart.modelOffset;
            ModelFileLoader.ReadType(ref vModel, ModelInputFilePosition);

            Int32 ModelLODInputFilePosition = ModelInputFilePosition + vModel.lodOffset;
            ModelFileLoader.ReadType(ref vLod, ModelLODInputFilePosition);

            Int32 MeshInputFilePosition = ModelLODInputFilePosition + vLod.meshOffset;
            VTX_Meshes = new MeshHeader_t[vLod.numMeshes];
            ModelFileLoader.ReadArray(ref VTX_Meshes, MeshInputFilePosition);

            for (Int32 i = 0; i < pModel.numvertices; i++)
            {
                pVertices[i] = MathUtils.SwapZY(VVD_Vertexes[pModel.vertexindex + i].m_vecPosition * ConfigLoader.WorldScale);
                pNormals[i] = MathUtils.SwapZY(VVD_Vertexes[pModel.vertexindex + i].m_vecNormal);
                pUvBuffer[i] = VVD_Vertexes[pModel.vertexindex + i].m_vecTexCoord;
            }

            Mesh pMesh = new Mesh();
            ModelObject.AddComponent<MeshCollider>().sharedMesh = pMesh;

            pMesh.subMeshCount = vLod.numMeshes;

            pMesh.vertices = pVertices;
            pMesh.normals = pNormals;
            pMesh.uv = pUvBuffer;

            if (MDL_Bones.Count > 1)
            {
                for (Int32 i = 0; i < pModel.numvertices; i++)
                    pBoneWeight[i] = GetBoneWeight(VVD_Vertexes[pModel.vertexindex + i].m_BoneWeights);

                SkinnedMeshRenderer smr = ModelObject.AddComponent<SkinnedMeshRenderer>();
                Matrix4x4[] bindPoses = new Matrix4x4[MDL_Bones.Count];

                for (Int32 i = 0; i < bindPoses.Length; i++)
                    bindPoses[i] = MDL_Bones[i].worldToLocalMatrix * ModelObject.transform.localToWorldMatrix;

                pMesh.boneWeights = pBoneWeight;
                pMesh.bindposes = bindPoses;

                smr.sharedMesh = pMesh;

                smr.bones = MDL_Bones.ToArray();
                smr.updateWhenOffscreen = true;
            }
            else
            {
                MeshFilter MeshFilter = ModelObject.AddComponent<MeshFilter>();
                ModelObject.AddComponent<MeshRenderer>();

                MeshFilter.sharedMesh = pMesh;
            }

            for (Int32 i = 0; i < vLod.numMeshes; i++)
            {
                List<Int32> pIndices = new List<Int32>();
                pStudioMesh = MDL_Meshes[i];

                StripGroupHeader_t[] StripGroups = new StripGroupHeader_t[VTX_Meshes[i].numStripGroups];
                Int32 StripGroupFilePosition = MeshInputFilePosition + (Marshal.SizeOf(typeof(MeshHeader_t)) * i) + VTX_Meshes[i].stripGroupHeaderOffset;
                ModelFileLoader.ReadArray(ref StripGroups, StripGroupFilePosition);

                for (Int32 j = 0; j < VTX_Meshes[i].numStripGroups; j++)
                {
                    Vertex_t[] pVertexBuffer = new Vertex_t[StripGroups[j].numVerts];
                    ModelFileLoader.ReadArray(ref pVertexBuffer, StripGroupFilePosition + (Marshal.SizeOf(typeof(StripGroupHeader_t)) * j) + StripGroups[j].vertOffset);

                    UInt16[] Indices = new UInt16[StripGroups[j].numIndices];
                    ModelFileLoader.ReadArray(ref Indices, StripGroupFilePosition + (Marshal.SizeOf(typeof(StripGroupHeader_t)) * j) + StripGroups[j].indexOffset);

                    for (Int32 n = 0; n < Indices.Length; n++)
                        pIndices.Add(pVertexBuffer[Indices[n]].origMeshVertID + pStudioMesh.vertexoffset);
                }

                pMesh.SetTriangles(pIndices.ToArray(), i);
                String MaterialPath = String.Empty;

                for (Int32 j = 0; j < MDL_TDirectories.Length; j++)
                {
                    for (Int32 n = 0; n < ConfigLoader.ModFolders.Length; n++)
                    {
                        if (File.Exists(ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[n] + "/materials/" + MDL_TDirectories[j] + MDL_Textures[pStudioMesh.material] + ".vmt"))
                            MaterialPath = MDL_TDirectories[j] + MDL_Textures[pStudioMesh.material];
                    }
                }

                pMaterials.Add(MaterialLoader.Load(MaterialPath));
            }

            ;
            ModelObject.GetComponent<Renderer>().sharedMaterials = pMaterials.ToArray();
        }

        static BoneWeight GetBoneWeight(mstudioboneweight_t mBoneWeight)
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

        static void ParseVvdFile()
        {
            if (VVD_Header.checksum != MDL_Header.checksum)
                throw new FileLoadException(String.Format("{0}: Does not match the checksum in the .mdl", ModelObject.name + ".vvd"));

            VVD_Fixups = new vertexFileFixup_t[VVD_Header.numFixups];
            ModelFileLoader.ReadArray(ref VVD_Fixups, VVD_Header.fixupTableStart);

            if (VVD_Header.numFixups == 0)
            {
                mstudiovertex_t[] thisVertexes = new mstudiovertex_t[VVD_Header.numLODVertexes[0]];
                ModelFileLoader.ReadArray(ref thisVertexes, VVD_Header.vertexDataStart);

                VVD_Vertexes.AddRange(thisVertexes);
            }

            for (Int32 i = 0; i < VVD_Header.numFixups; i++)
            {
                if (VVD_Fixups[i].lod >= 0)
                {
                    mstudiovertex_t[] thisVertexes = new mstudiovertex_t[VVD_Fixups[i].numVertexes];
                    ModelFileLoader.ReadArray(ref thisVertexes, VVD_Header.vertexDataStart + (VVD_Fixups[i].sourceVertexID * 48));

                    VVD_Vertexes.AddRange(thisVertexes);
                }
            }
        }
    }
}
