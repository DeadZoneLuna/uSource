using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace uSource.Formats.Source.MDL
{
    public class VVDFile : StudioStruct
    {
        public vertexFileHeader_t VVD_Header;
        public mstudiovertex_t[][] VVD_Vertexes;
        public vertexFileFixup_t[] VVD_Fixups;
        public Boolean HasTangents;

        //TODO:
        //Fix missed vertexes on some meshes. (on lod's & sometimes the main model)
        public VVDFile(Stream FileInput, MDLFile mdl)
        {
            using (uReader FileStream = new uReader(FileInput))
            {
                FileStream.ReadTypeFixed(ref VVD_Header, 64);

                if (VVD_Header.checksum != mdl.MDL_Header.checksum)
                    throw new FileLoadException(String.Format("{0}: Does not match the checksum in the .mdl", mdl.MDL_Header.Name));

                if (VVD_Header.numFixups > 0)
                {
                    VVD_Fixups = new vertexFileFixup_t[VVD_Header.numFixups];
                    FileStream.ReadArrayFixed(ref VVD_Fixups, 12, VVD_Header.fixupTableStart);
                }

                //TODO
                HasTangents = VVD_Header.tangentDataStart != 0;

                //"HasTagents" used to avoid non-zero length
                //Int64 TotalVerts = (HasTangents ? VVD_Header.tangentDataStart - VVD_Header.vertexDataStart : FileStream.InputStream.Length - VVD_Header.vertexDataStart) / 48;
                mstudiovertex_t[] tempVerts = new mstudiovertex_t[VVD_Header.numLODVertexes[0]];
                FileStream.ReadArrayFixed(ref tempVerts, 48, VVD_Header.vertexDataStart);

                VVD_Vertexes = new mstudiovertex_t[VVD_Header.numLODs][];
                List<mstudiovertex_t> TempVerts = new List<mstudiovertex_t>();

                for (Int32 LODID = 0; LODID < VVD_Header.numLODs; ++LODID)
                {
                    if (VVD_Header.numFixups == 0)
                    {
                        VVD_Vertexes[LODID] = tempVerts.Take(VVD_Header.numLODVertexes[LODID]).ToArray();
                        continue;
                    }

                    TempVerts.Clear();

                    foreach (vertexFileFixup_t VertexFixup in VVD_Fixups)
                    {
                        if (VertexFixup.lod >= LODID)
                        {
                            TempVerts.AddRange(tempVerts.Skip(VertexFixup.sourceVertexID).Take(VertexFixup.numVertexes));
                        }
                    }

                    VVD_Vertexes[LODID] = TempVerts.ToArray();
                }
            }
        }
    }
}