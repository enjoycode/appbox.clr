using System;
using System.Collections.Generic;

namespace appbox.Data
{
    partial class Entity
    {
        private Dictionary<string, object> _attached;

        internal void AddAttached(string name, object value)
        {
            if (_attached == null) _attached = new Dictionary<string, object>();
            _attached.Add(name, value);
        }

        public object GetAttached(string name)
        {
            if (_attached.TryGetValue(name, out object value))
                return value;
            return null;
        }
    }
}
