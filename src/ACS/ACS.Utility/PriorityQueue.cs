using System;
using System.Collections;
using System.Collections.Generic;

namespace ACS.Utility
{
    /// <summary>
    /// Java-style PriorityQueue compatibility class for Spring.NET/NHibernate migration.
    /// Wraps SortedSet with IComparer support.
    /// </summary>
    public class PriorityQueue
    {
        private readonly List<object> _items;
        private readonly IComparer _comparer;

        public PriorityQueue(int initialCapacity, IComparer comparer)
        {
            _items = new List<object>(initialCapacity);
            _comparer = comparer;
        }

        public bool IsEmpty => _items.Count == 0;

        public int Count => _items.Count;

        public void Offer(object item)
        {
            _items.Add(item);
            _items.Sort(Comparer<object>.Create((a, b) => _comparer.Compare(a, b)));
        }

        public object Peek()
        {
            if (_items.Count == 0) return null;
            return _items[0];
        }

        public object Poll()
        {
            if (_items.Count == 0) return null;
            var item = _items[0];
            _items.RemoveAt(0);
            return item;
        }
    }
}
