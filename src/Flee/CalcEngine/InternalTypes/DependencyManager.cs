using Flee.CalcEngine.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{

    /// <summary>
    /// Keeps track of our dependencies
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DependencyManager<T>
    {

        /// <summary>
        /// Map of a node and the nodes that depend on it
        /// </summary>
        private readonly Dictionary<T, Dictionary<T, object>> _dependentsMap;
        private readonly IEqualityComparer<T> _equalityComparer;

        /// <summary>
        /// Map of a node and the number of nodes that point to it
        /// </summary>
        private readonly Dictionary<T, int> _precedentsMap;
        public DependencyManager(IEqualityComparer<T> comparer)
        {
            _equalityComparer = comparer;
            _dependentsMap = new Dictionary<T, Dictionary<T, object>>(_equalityComparer);
            _precedentsMap = new Dictionary<T, int>(_equalityComparer);
        }

        private IDictionary<T, object> CreateInnerDictionary()
        {
            return new Dictionary<T, object>(_equalityComparer);
        }

        private IDictionary<T, object> GetInnerDictionary(T tail)
        {
            Dictionary<T, object> value = null;

            if (_dependentsMap.TryGetValue(tail, out value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        // Create a dependency list with only the dependents of the given tails
        public DependencyManager<T> CloneDependents(T[] tails)
        {
            IDictionary<T, object> seenNodes = this.CreateInnerDictionary();
            DependencyManager<T> copy = new DependencyManager<T>(_equalityComparer);

            foreach (T tail in tails)
            {
                this.CloneDependentsInternal(tail, copy, seenNodes);
            }

            return copy;
        }

        private void CloneDependentsInternal(T tail, DependencyManager<T> target, IDictionary<T, object> seenNodes)
        {
            if (seenNodes.ContainsKey(tail))
            {
                // We've already added this node so just return
                return;
            }
            else
            {
                // Haven't seen this node yet; mark it as visited
                seenNodes.Add(tail, null);
                target.AddTail(tail);
            }

            IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);

            // Do the recursive add
            foreach (T head in innerDict.Keys)
            {
                target.AddDepedency(tail, head);
                this.CloneDependentsInternal(head, target, seenNodes);
            }
        }

        public T[] GetTails()
        {
            T[] arr = new T[_dependentsMap.Keys.Count];
            _dependentsMap.Keys.CopyTo(arr, 0);
            return arr;
        }

        public void Clear()
        {
            _dependentsMap.Clear();
            _precedentsMap.Clear();
        }

        public void ReplaceDependency(T old, T replaceWith)
        {
            Dictionary<T, object> value = _dependentsMap[old];

            _dependentsMap.Remove(old);
            _dependentsMap.Add(replaceWith, value);

            foreach (Dictionary<T, object> innerDict in _dependentsMap.Values)
            {
                if (innerDict.ContainsKey(old))
                {
                    innerDict.Remove(old);
                    innerDict.Add(replaceWith, null);
                }
            }
        }

        public void AddTail(T tail)
        {
            if (!_dependentsMap.ContainsKey(tail))
            {
                _dependentsMap.Add(tail, (Dictionary<T, object>)this.CreateInnerDictionary());
            }
        }

        public void AddDepedency(T tail, T head)
        {
            IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);

            if (!innerDict.ContainsKey(head))
            {
                innerDict.Add(head, head);
                this.AddPrecedent(head);
            }
        }

        public void RemoveDependency(T tail, T head)
        {
            IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);
            this.RemoveHead(head, innerDict);
        }

        private void RemoveHead(T head, IDictionary<T, object> dict)
        {
            if (dict.Remove(head))
            {
                this.RemovePrecedent(head);
            }
        }

        public void Remove(T[] tails)
        {
            foreach (Dictionary<T, object> innerDict in _dependentsMap.Values)
            {
                foreach (T tail in tails)
                {
                    this.RemoveHead(tail, innerDict);
                }
            }

            foreach (T tail in tails)
            {
                _dependentsMap.Remove(tail);
            }
        }

        public void GetDirectDependents(T tail, List<T> dest)
        {
            Dictionary<T, object> innerDict = (Dictionary<T, object>)this.GetInnerDictionary(tail);
            dest.AddRange(innerDict.Keys);
        }

        public T[] GetDependents(T tail)
        {
            Dictionary<T, object> dependents = (Dictionary<T, object>)this.CreateInnerDictionary();
            this.GetDependentsRecursive(tail, dependents);

            T[] arr = new T[dependents.Count];
            dependents.Keys.CopyTo(arr, 0);
            return arr;
        }

        private void GetDependentsRecursive(T tail, Dictionary<T, object> dependents)
        {
            dependents[tail] = null;
            Dictionary<T, object> directDependents = (Dictionary<T, object>)this.GetInnerDictionary(tail);

            foreach (T pair in directDependents.Keys)
            {
                this.GetDependentsRecursive(pair, dependents);
            }
        }

        public void GetDirectPrecedents(T head, IList<T> dest)
        {
            foreach (T tail in _dependentsMap.Keys)
            {
                Dictionary<T, object> innerDict = (Dictionary<T, object>)this.GetInnerDictionary(tail);
                if (innerDict.ContainsKey(head))
                {
                    dest.Add(tail);
                }
            }
        }

        private void AddPrecedent(T head)
        {
            int count = 0;
            _precedentsMap.TryGetValue(head, out count);
            _precedentsMap[head] = count + 1;
        }

        private void RemovePrecedent(T head)
        {
            int count = _precedentsMap[head] - 1;

            if (count == 0)
            {
                _precedentsMap.Remove(head);
            }
            else
            {
                _precedentsMap[head] = count;
            }
        }

        public bool HasPrecedents(T head)
        {
            return _precedentsMap.ContainsKey(head);
        }

        public bool HasDependents(T tail)
        {
            Dictionary<T, object> innerDict = (Dictionary<T, object>)this.GetInnerDictionary(tail);
            return innerDict.Count > 0;
        }

        private string FormatValues(ICollection<T> values)
        {
            string[] strings = new string[values.Count];
            T[] keys = new T[values.Count];
            values.CopyTo(keys, 0);

            for (int i = 0; i <= keys.Length - 1; i++)
            {
                strings[i] = keys[i].ToString();
            }

            if (strings.Length == 0)
            {
                return "<empty>";
            }
            else
            {
                return string.Join(",", strings);
            }
        }

        /// <summary>
        ///  Add all nodes that don't have any incoming edges into a queue
        /// </summary>
        /// <param name="rootTails"></param>
        /// <returns></returns>
        public Queue<T> GetSources(T[] rootTails)
        {
            Queue<T> q = new Queue<T>();

            foreach (T rootTail in rootTails)
            {
                if (!this.HasPrecedents(rootTail))
                {
                    q.Enqueue(rootTail);
                }
            }

            return q;
        }

        public IList<T> TopologicalSort(Queue<T> sources)
        {
            IList<T> output = new List<T>();
            List<T> directDependents = new List<T>();

            while (sources.Count > 0)
            {
                T n = sources.Dequeue();
                output.Add(n);

                directDependents.Clear();
                this.GetDirectDependents(n, directDependents);

                foreach (T m in directDependents)
                {
                    this.RemoveDependency(n, m);

                    if (!this.HasPrecedents(m))
                    {
                        sources.Enqueue(m);
                    }
                }
            }

            if (output.Count != this.Count)
            {
                throw new CircularReferenceException();
            }

            return output;
        }

#if DEBUG
        public string Precedents
        {
            get
            {
                List<string> list = new List<string>();

                foreach (KeyValuePair<T, int> pair in _precedentsMap)
                {
                    list.Add(pair.ToString());
                }

                return string.Join(System.Environment.NewLine, list.ToArray());
            }
        }
#endif

        public string DependencyGraph
        {
            get
            {
                string[] lines = new string[_dependentsMap.Count];
                int index = 0;

                foreach (KeyValuePair<T, Dictionary<T, object>> pair in _dependentsMap)
                {
                    T key = pair.Key;
                    string s = this.FormatValues(pair.Value.Keys);
                    lines[index] = $"{key} -> {s}";
                    index += 1;
                }

                return string.Join(System.Environment.NewLine, lines);
            }
        }

        public int Count => _dependentsMap.Count;
    }

}

