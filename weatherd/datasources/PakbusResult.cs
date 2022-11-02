using System.Collections.Generic;

namespace weatherd.datasources
{
    public class PakbusResult
    {
        private readonly Dictionary<string, object> _resultObjects;

        public IEnumerable<string> Keys => _resultObjects.Keys;

        public PakbusResult()
        {
            _resultObjects = new Dictionary<string, object>();
        }

        internal void Add<T>(string key, T obj)
            => _resultObjects.Add(key, obj);

        public T Get<T>(string key)
        {
            if (!_resultObjects.ContainsKey(key))
                throw new KeyNotFoundException("Could not find key '" + key + "'");

            object result = _resultObjects[key];

            return (T)result;
        }

        public bool TryGet<T>(string key, out T result)
        {
            result = default(T);
            if (!_resultObjects.ContainsKey(key))
                return false;

            object resultObject = _resultObjects[key];

            result = (T)resultObject;
            return true;
        }
    }
}
