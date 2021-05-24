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

        public VVDFile(Stream FileInput, MDLFile mdl)
        {
            using (var FileStream = new uReader(FileInput))
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
                var sizeVerts = (HasTangents ? VVD_Header.tangentDataStart - VVD_Header.vertexDataStart : FileStream.InputStream.Length - VVD_Header.vertexDataStart) / 48;
                var tempVerts = new mstudiovertex_t[sizeVerts];
                FileStream.ReadArrayFixed(ref tempVerts, 48, VVD_Header.vertexDataStart);

                VVD_Vertexes = new mstudiovertex_t[VVD_Header.numLODs][];
                var lodVerts = new List<mstudiovertex_t>();

                for (var lodID = 0; lodID < VVD_Header.numLODs; ++lodID)
                {
                    if (VVD_Header.numFixups == 0)
                    {
                        VVD_Vertexes[lodID] = tempVerts.Take(VVD_Header.numLODVertexes[lodID]).ToArray();
                        continue;
                    }

                    lodVerts.Clear();

                    foreach (var vertexFixup in VVD_Fixups)
                    {
                        if (vertexFixup.lod >= lodID)
                        {
                            lodVerts.AddRange(tempVerts.Skip(vertexFixup.sourceVertexID).Take(vertexFixup.numVertexes));
                        }
                    }

                    VVD_Vertexes[lodID] = lodVerts.ToArray();
                }
            }
        }
    }
}