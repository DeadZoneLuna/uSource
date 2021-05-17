﻿using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

namespace uSource.Formats.Source.MDL
{
	public class VTXFile : StudioStruct
	{
		public FileHeader_t VTX_Header;

		public VTXFile(Stream FileInput, MDLFile StudioMDL, VVDFile StudioVVD)
		{
			using (var FileStream = new uReader(FileInput))
			{
				studiohdr_t MDL_Header = StudioMDL.MDL_Header;
				FileStream.ReadType(ref VTX_Header);

				if (VTX_Header.checkSum != MDL_Header.checksum)
					throw new FileLoadException(String.Format("{0}: Does not match the checksum in the .mdl", MDL_Header.Name));

				Int32[] vertexoffset = new Int32[8];
				for (Int32 bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
				{
					BodyPartHeader_t pBodypart = new BodyPartHeader_t();
					Int64 pBodypartOffset = VTX_Header.bodyPartOffset + (8 * bodypartID);
					FileStream.ReadType(ref pBodypart, pBodypartOffset);

					StudioBodyPart MDLPart = StudioMDL.MDL_Bodyparts[bodypartID];

					for (Int32 modelID = 0; modelID < pBodypart.numModels; modelID++)
					{
						StudioModel MDLModel = MDLPart.Models[modelID];

						if (MDLModel.isBlank)
						{
							Debug.Log(String.Format("Model ID - {0} in bodypart \"{1}\" is blank, skip", modelID, MDLPart.Name));
							continue;
						}

						ModelHeader_t pModel = new ModelHeader_t();
						Int64 pModelOffset = pBodypartOffset + (8 * modelID) + pBodypart.modelOffset;
						FileStream.ReadType(ref pModel, pModelOffset);

						//TODO: Fix all lod's per model to use other lod's than 1 (VVD / MDL)
						for (Int32 LODID = 0; LODID < 1; LODID++)
						{
							ModelLODHeader_t pLOD = new ModelLODHeader_t();
							Int64 pLODOffset = pModelOffset + (12 * LODID) + pModel.lodOffset;
							FileStream.ReadType(ref pLOD, pLODOffset);

							//Temp remember verts count per lod model
							Int32 TotalVerts = 0;
							for (Int32 MeshID = 0; MeshID < MDLModel.Model.nummeshes; MeshID++)
							{
								mstudiomesh_t MDLMesh = MDLPart.Models[modelID].Meshes[MeshID];

								TotalVerts += MDLModel.Meshes[MeshID].VertexData.numlodvertices[LODID];

								MeshHeader_t pMesh = new MeshHeader_t();
								Int64 pMeshOffset = pLODOffset + (9 * MeshID) + pLOD.meshOffset;
								FileStream.ReadType(ref pMesh, pMeshOffset);

								List<Int32> pIndices = new List<Int32>();
								for (Int32 stripgroupID = 0; stripgroupID < pMesh.numStripGroups; stripgroupID++)
								{
									StripGroupHeader_t pStripGroup = new StripGroupHeader_t();
									Int64 pStripGroupOffset = pMeshOffset + (25 * stripgroupID) + pMesh.stripGroupHeaderOffset;
									FileStream.ReadType(ref pStripGroup, pStripGroupOffset);

									Vertex_t[] Vertexes = new Vertex_t[pStripGroup.numVerts];
									FileStream.BaseStream.Position = pStripGroupOffset + pStripGroup.vertOffset;
									FileStream.ReadArray(ref Vertexes);

									FileStream.BaseStream.Position = pStripGroupOffset + pStripGroup.indexOffset;
									Int16[] Indices = FileStream.ReadShortArray(pStripGroup.numIndices);

									for (int stripID = 0; stripID < pStripGroup.numStrips; stripID++)
									{
										StripHeader_t VTXStrip = new StripHeader_t();
										Int64 VTXStripOffset = pStripGroupOffset + (27 * stripID) + pStripGroup.stripOffset;
										FileStream.ReadType(ref VTXStrip, VTXStripOffset);

										if ((VTXStrip.flags & VTXStripGroupTriListFlag) > 0)
										{
											for (var j = VTXStrip.indexOffset; j < VTXStrip.indexOffset + VTXStrip.numIndices; j++)
											{
												pIndices.Add(Vertexes[Indices[j]].origMeshVertId + MDLMesh.vertexoffset);// + vertexoffset);
											}
										}
										else if ((VTXStrip.flags & VTXStripGroupTriStripFlag) > 0)
										{
											for (var j = VTXStrip.indexOffset; j < VTXStrip.indexOffset + VTXStrip.numIndices - 2; j++)
											{
												var add = j % 2 == 1 ? new[] { j + 1, j, j + 2 } : new[] { j, j + 1, j + 2 };
												foreach (var idx in add)
												{
													pIndices.Add(Vertexes[Indices[idx]].origMeshVertId + MDLMesh.vertexoffset);// + vertexoffset);
												}
											}
										}
									}
								}

								StudioMDL.SetIndices(bodypartID, modelID, LODID, MeshID, pIndices);
								//StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod[LODID].Add(MeshID, pIndices);
							}

							//StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod[LODID] = new mstudiovertex_t[TotalVerts];
							//Array.Copy(StudioVVD.VVD_Vertexes[LODID], vertexoffset[LODID], StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod[LODID], 0, TotalVerts);

							StudioMDL.SetVertices(bodypartID, modelID, LODID, TotalVerts, vertexoffset[LODID], StudioVVD.VVD_Vertexes[LODID]);

							vertexoffset[LODID] += TotalVerts;
						}
					}
				}
			}
		}
	}
}