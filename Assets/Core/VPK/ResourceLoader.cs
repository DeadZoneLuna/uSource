using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Source
{
    public interface IResourceProvider
    {
        bool ContainsFile(string filename);
        Stream OpenFile(string filename);
    }

    public class ResourceLoader : IResourceProvider
    {
        private readonly List<IResourceProvider> _providers = new List<IResourceProvider>();

        public void AddResourceProvider(IResourceProvider provider)
        {
            _providers.Add(provider);
        }

        public void RemoveResourceProvider(IResourceProvider provider)
        {
            _providers.Remove(provider);
        }

        public bool ContainsFile(string filename)
        {
            for (var i = _providers.Count - 1; i >= 0; --i)
            {
                if (_providers[i].ContainsFile(filename)) return true;
            }

            return false;
        }

        public Stream OpenFile(string filename)
        {
            for (var i = _providers.Count - 1; i >= 0; --i)
            {
                if (_providers[i].ContainsFile(filename)) return _providers[i].OpenFile(filename);
            }

            return _providers[0].OpenFile(filename);
        }

        /*private readonly Dictionary<string, StudioMdlLoader> _sLoadedMdls
            = new Dictionary<string, StudioMdlLoader>(StringComparer.CurrentCultureIgnoreCase);

        public StudioMdlLoader Load(string filename)
        {
            StudioMdlLoader loaded;
            if (_sLoadedMdls.TryGetValue(filename, out loaded)) return loaded;

            loaded = new StudioMdlLoader(this, filename);
            _sLoadedMdls.Add(filename, loaded);

            return loaded;
        }*/

        /*private readonly Dictionary<string, VmtFile> _sLoadedVmts
            = new Dictionary<string, VmtFile>(StringComparer.CurrentCultureIgnoreCase);

        public VmtFile LoadVmt(string filename)
        {
            VmtFile loaded;
            if (_sLoadedVmts.TryGetValue(filename, out loaded)) return loaded;

            var fullName = "materials/" + filename;
            if (!ContainsFile(fullName)) fullName = filename;

            using (var stream = OpenFile(fullName))
            {
                loaded = VmtFile.FromStream(stream);
                _sLoadedVmts.Add(filename, loaded);
            }

            return loaded;
        }

        private readonly Dictionary<string, VtfFile> _sLoadedVtfs
            = new Dictionary<string, VtfFile>(StringComparer.CurrentCultureIgnoreCase);

        public VtfFile LoadVtf(string filename)
        {
            VtfFile loaded;
            if (_sLoadedVtfs.TryGetValue(filename, out loaded)) return loaded;

            var fullName = "materials/" + filename;
            if (!ContainsFile(fullName)) fullName = filename;

            using (var stream = OpenFile(fullName))
            {
                loaded = VtfFile.FromStream(stream);
                _sLoadedVtfs.Add(filename, loaded);
            }

            return loaded;
        }*/
    }
}
