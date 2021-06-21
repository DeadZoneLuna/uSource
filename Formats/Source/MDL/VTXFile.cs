using System.Collections;
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
			using (uReader FileStream = new uReader(FileInput))
			{
				studiohdr_t MDL_Header = StudioMDL.MDL_Header;
				FileStream.ReadTypeFixed(ref VTX_Header, 36);

				if (VTX_Header.checkSum != MDL_Header.checksum)
					throw new FileLoadException(String.Format("{0}: Does not match the checksum in the .mdl", MDL_Header.Name));

                #region BodyParts
                Int32[] VertexLODOffsets = new Int32[8];
				for (Int32 BodypartID = 0; BodypartID < MDL_Header.bodypart_count; BodypartID++)
				{
					BodyPartHeader_t BodyPart = new BodyPartHeader_t();
					Int64 BodyPartOffset = VTX_Header.bodyPartOffset + (8 * BodypartID);
					FileStream.ReadTypeFixed(ref BodyPart, 8, BodyPartOffset);

					StudioBodyPart StudioBodyPart = StudioMDL.MDL_Bodyparts[BodypartID];

                    #region Models
                    for (Int32 ModelID = 0; ModelID < BodyPart.numModels; ModelID++)
					{
						StudioModel StudioModel = StudioBodyPart.Models[ModelID];

						if (StudioModel.isBlank)
						{
							Debug.Log(String.Format("Model ID - {0} in bodypart \"{1}\" is blank, skip", ModelID, StudioBodyPart.Name));
							continue;
						}

						ModelHeader_t Model = new ModelHeader_t();
						Int64 ModelOffset = BodyPartOffset + (8 * ModelID) + BodyPart.modelOffset;
						FileStream.ReadTypeFixed(ref Model, 8, ModelOffset);

						StudioBodyPart.Models[ModelID].NumLODs = Model.numLODs;
						StudioBodyPart.Models[ModelID].LODData = new ModelLODHeader_t[Model.numLODs];

                        #region LOD's
                        //TODO: Strip unused vertexes on lower lod's ("first" lod is fine)
                        for (Int32 LODID = 0; LODID < Model.numLODs; LODID++)
						{
							ModelLODHeader_t LOD = new ModelLODHeader_t();
							Int64 LODOffset = ModelOffset + (12 * LODID) + Model.lodOffset;
							FileStream.ReadTypeFixed(ref LOD, 12, LODOffset);

							StudioBodyPart.Models[ModelID].LODData[LODID] = LOD;

                            #region Mesh LOD
                            //Temp remember verts count per lod model
                            Int32 VertexOffset = 0;
							//List<mstudiovertex_t> VertexesPerLod = new List<mstudiovertex_t>();
							for (Int32 MeshID = 0; MeshID < StudioModel.Model.nummeshes; MeshID++)
							{
								mstudiomesh_t StudioMesh = StudioBodyPart.Models[ModelID].Meshes[MeshID];

								//TODO: StudioModel.Meshes[MeshID].VertexData.numlodvertices[LODID]; - we no longer need this??
								VertexOffset += StudioMesh.numvertices;
								List<Int32> IndicesPerMesh = new List<Int32>();

								MeshHeader_t Mesh = new MeshHeader_t();
								Int64 MeshOffset = LODOffset + (9 * MeshID) + LOD.meshOffset;
								FileStream.ReadTypeFixed(ref Mesh, 9, MeshOffset);

								#region StripGroups
								for (Int32 StripGroupID = 0; StripGroupID < Mesh.numStripGroups; StripGroupID++)
								{
									StripGroupHeader_t StripGroup = new StripGroupHeader_t();
									Int64 StripGroupOffset = MeshOffset + (25 * StripGroupID) + Mesh.stripGroupHeaderOffset;
									FileStream.ReadTypeFixed(ref StripGroup, 25, StripGroupOffset);

									Vertex_t[] Vertexes = new Vertex_t[StripGroup.numVerts];
									FileStream.BaseStream.Position = StripGroupOffset + StripGroup.vertOffset;
									FileStream.ReadArrayFixed(ref Vertexes, 9);

									FileStream.BaseStream.Position = StripGroupOffset + StripGroup.indexOffset;
									Int16[] Indices = FileStream.ReadShortArray(StripGroup.numIndices);

									#region Strips
									for (Int32 StripID = 0; StripID < StripGroup.numStrips; StripID++)
									{
										StripHeader_t VTXStrip = new StripHeader_t();
										Int64 VTXStripOffset = StripGroupOffset + (27 * StripID) + StripGroup.stripOffset;
										FileStream.ReadTypeFixed(ref VTXStrip, 27, VTXStripOffset);

										//TODO:
										//Strip / "Split" vertexes
										//Pseudo code:
										/*for (Int32 VertID = 0; VertID < maxVertsPerLod; VertID++)
										{
											Int32 Index = MeshID * VTXStrip.numVerts + VertID;

											if (Index < numStripVerts)
											{
												splitVerts.Add(verts[Index]);
												splitIndices.Add(j);
											}
										}*/

										//Hmmmmm... Well, it's looks what we want.... but still doesn't perfect (for lod's mesh)
										/*Int32 NumVerts = VTXStrip.indexOffset + VTXStrip.numVerts;
										for (Int32 VertID = VTXStrip.indexOffset; VertID < NumVerts; VertID++)
										{
											Int32 Index0 = VertID + StudioMesh.vertexoffset + VertexLODOffsets[LODID];
											VertexesPerLod.Add(StudioVVD.tempVerts[Index0]);
										}*/

										if ((VTXStrip.flags & VTXStripGroupTriStripFlag) > 0)
										{
											for (Int32 TempIdx = VTXStrip.indexOffset; TempIdx < VTXStrip.indexOffset + VTXStrip.numIndices - 2; TempIdx++)
											{
												Int32[] add = TempIdx % 2 == 1 ?
													new[] { TempIdx + 1, TempIdx, TempIdx + 2 } :
													new[] { TempIdx, TempIdx + 1, TempIdx + 2 };

												foreach (Int32 Index in add)
												{
													IndicesPerMesh.Add(Vertexes[Indices[Index]].origMeshVertId + StudioMesh.vertexoffset);
												}
											}
										}
										else
										{
											for (Int32 Index = VTXStrip.indexOffset; Index < VTXStrip.indexOffset + VTXStrip.numIndices; Index++)
											{
												IndicesPerMesh.Add(Vertexes[Indices[Index]].origMeshVertId + StudioMesh.vertexoffset);
											}
										}
									}
                                    #endregion
                                }
                                #endregion

                                StudioMDL.SetIndices(BodypartID, ModelID, LODID, MeshID, IndicesPerMesh);
                            }
							#endregion

							//StudioMDL.MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID] = VertexesPerLod.ToArray();
							///TODO: Strip unused vertexes in <seealso cref="VVDFile.VVD_Vertexes"/> per lod
							StudioMDL.SetVertices(BodypartID, ModelID, LODID, VertexOffset, VertexLODOffsets[LODID], StudioVVD.VVD_Vertexes[0]);

							VertexLODOffsets[LODID] += VertexOffset;
						}
                        #endregion
                    }
                    #endregion
                }
                #endregion
            }
        }
	}
}