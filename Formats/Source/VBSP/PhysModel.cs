using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace uSource.Formats.Source.VBSP
{
	public class PhysModel
	{
		public PhysModel(int modelIndex, int solidCount, byte[] collisionData, byte[] keyData)
		{
			ModelIndex = modelIndex;
			KeyData = System.Text.Encoding.ASCII.GetString(keyData);

			using (var ms = new MemoryStream(collisionData))
			{
				using (var br = new BinaryReader(ms))
				{
					for (int i = 0; i < solidCount; i++)
					{
						var solid = new PhysModelSolid();
						Solids.Add(solid);

						var size = br.ReadInt32();
						var maxPos = br.BaseStream.Position + size;
						solid.vphysicsID = br.ReadInt16(); // ??
						solid.version = br.ReadInt16();
						br.ReadInt16();
						solid.modelType = br.ReadInt16();

						if (solid.modelType != 0x0)
						{
							br.BaseStream.Seek(maxPos - br.BaseStream.Position, SeekOrigin.Current);
							continue;
						}

						// ???
						br.BaseStream.Seek(68, SeekOrigin.Current);

						while (true)
						{
							var cc = new PhysModelConvex();
							solid.Convexes.Add(cc);

							var pos = br.BaseStream.Position;
							var vertexOffset = (int)(pos + br.ReadUInt32());

							cc.BrushIndex = br.ReadInt32();
							cc.idk2 = br.ReadByte();
							cc.idk3 = br.ReadByte();
							cc.idk4 = br.ReadUInt16();

							var triCount = br.ReadInt16();
							cc.idk5 = br.ReadUInt16();

							for (int j = 0; j < triCount; j++)
							{
								br.BaseStream.Seek(4, SeekOrigin.Current);

								var index1 = br.ReadInt16();
								br.ReadInt16();
								var index2 = br.ReadInt16();
								br.ReadInt16();
								var index3 = br.ReadInt16();
								br.ReadInt16();

								try
								{
									var v1 = collisionData.ReadAtPosition<Vector3>(vertexOffset + index1 * 16);
									var v2 = collisionData.ReadAtPosition<Vector3>(vertexOffset + index2 * 16);
									var v3 = collisionData.ReadAtPosition<Vector3>(vertexOffset + index3 * 16);

									cc.Triangles.Add(cc.Verts.Count);
									cc.Triangles.Add(cc.Verts.Count + 1);
									cc.Triangles.Add(cc.Verts.Count + 2);
									cc.Verts.Add(v1);
									cc.Verts.Add(v2);
									cc.Verts.Add(v3);
								}
								catch (System.Exception e)
								{
									Debug.Log("Error on solid type: " + solid.modelType);
									Debug.LogError(e);
								}
							}

							if (br.BaseStream.Position >= vertexOffset)
							{
								break;
							}
						}

						var remainder = maxPos - br.BaseStream.Position;
						if (remainder > 0)
						{
							br.BaseStream.Seek(remainder, SeekOrigin.Current);
						}
					}
				}
			}

			KeyValues = KeyValues.Parse(KeyData);
		}

		public readonly int ModelIndex;
		public string KeyData;
		public List<PhysModelSolid> Solids = new List<PhysModelSolid>();
		public KeyValues KeyValues;

	}

	public class PhysModelConvex
	{
		public List<int> Triangles = new List<int>();
		public List<UnityEngine.Vector3> Verts = new List<UnityEngine.Vector3>();
		public int BrushIndex;
		public byte idk2;
		public byte idk3;
		public ushort idk4;
		public ushort idk5;

		// todo : needs research.  this presumably detects the convex that wraps an entity with multiple solids, which we won't want to actually generate
		public bool Skip => idk2 == 5;

		public override string ToString()
		{
			return $"#{BrushIndex} - {idk2} - {idk3} - {idk4} - {idk5}";
		}
	}

	public class PhysModelSolid
	{
		public int vphysicsID;
		public short version;
		public short modelType;
		public bool Fluid;
		public List<PhysModelConvex> Convexes = new List<PhysModelConvex>();
		public GameObject ConvexContainer;
	}
}