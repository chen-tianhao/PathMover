using System.Collections.Generic;

namespace PathMover
{
    /// <summary>
    /// Manages mapping between string control point names and integer IDs.
    /// Used for human-readable logging while maintaining integer efficiency.
    /// </summary>
    public class IdMapper
    {
        private readonly Dictionary<ushort, string> _idToName = new Dictionary<ushort, string>();
        private readonly Dictionary<string, ushort> _nameToId = new Dictionary<string, ushort>();

        /// <summary>
        /// Get the string name for a control point ID
        /// </summary>
        public string GetName(ushort id)
        {
            return _idToName.TryGetValue(id, out var name) ? name : id.ToString();
        }

        /// <summary>
        /// Get the integer ID for a control point name
        /// </summary>
        public ushort GetId(string name)
        {
            return _nameToId[name];
        }

        /// <summary>
        /// Try to get the integer ID for a control point name
        /// </summary>
        public bool TryGetId(string name, out ushort id)
        {
            return _nameToId.TryGetValue(name, out id);
        }

        /// <summary>
        /// Register a new ID mapping
        /// </summary>
        public void Register(ushort id, string name)
        {
            _idToName[id] = name;
            _nameToId[name] = id;
        }

        /// <summary>
        /// Get all registered IDs
        /// </summary>
        public IEnumerable<ushort> GetAllIds()
        {
            return _idToName.Keys;
        }

        /// <summary>
        /// Get all registered names
        /// </summary>
        public IEnumerable<string> GetAllNames()
        {
            return _nameToId.Keys;
        }

        /// <summary>
        /// Get total count of registered mappings
        /// </summary>
        public int Count => _idToName.Count;
    }
}
