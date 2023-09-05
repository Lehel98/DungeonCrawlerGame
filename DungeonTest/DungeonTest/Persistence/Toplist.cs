using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonTest.Persistence
{
    internal class Toplist
    {
        ConcurrentDictionary<String, List<Tuple<String, Int32>>> _toplists;

        public Toplist() => _toplists = new ConcurrentDictionary<String, List<Tuple<String, Int32>>>();

        public ICollection<String> Maps => _toplists.Keys.ToList();

        public List<Tuple<String, Int32>> this[String key]
        {
            get
            {
                if (!_toplists.ContainsKey(key))
                    throw new InvalidOperationException();

                return _toplists[key];
            }
        }

        public List<String> GetToplist
        {
            get
            {
                List<String> toplist = new List<String>();

                foreach (String key in _toplists.Keys)
                {                    
                    foreach (Tuple<String, Int32> tuple in _toplists[key])
                    {
                        toplist.Add(key + " " + tuple.Item1 + " " + tuple.Item2 + " másodperc");
                    }
                }

                return toplist;
            }
        }

        public void AddResult(String map, String player, Int32 result)
        {
            if (!_toplists.ContainsKey(map))
            {
                _toplists.TryAdd(map, new List<Tuple<String, Int32>>());
            }

            _toplists[map].Add(new Tuple<String, Int32>(player, result));
        }

        #region Sort lists with merge sort

        public void MergeSortAll()
        {
            foreach (String key in _toplists.Keys)
            {
                List<Tuple<String, Int32>> list = _toplists[key];
                MS(ref list, 0, _toplists[key].Count);
                _toplists[key] = list;
            }
        }

        private void MS(ref List<Tuple<String, Int32>> map, Int32 u, Int32 v)
        {
            if (u < v - 1)
            {
                int m = (u + v) / 2;

                MS(ref map, u, m);
                MS(ref map, m, v);
                Merge(ref map, u, m, v);
            }
        }

        private void Merge(ref List<Tuple<String, Int32>> map, Int32 u, Int32 m, Int32 v)
        {
            Int32 d = m - u;

            List<Tuple<String, Int32>> Z = new List<Tuple<String, Int32>>();

            for (Int32 i = 0; i < d; ++i) { Z.Add(map[u + i]); }

            Int32 k = u, j = 0, l = m;

            while (l < v && j < d)
            {
                if (map[l].Item2 < Z[j].Item2)
                {
                    map[k] = map[l];
                    ++l;
                }
                else
                {
                    map[k] = Z[j];
                    ++j;
                }

                ++k;
            }

            while (j < d)
            {
                map[k] = Z[j];
                ++k;
                ++j;
            }
        }

        #endregion
    }
}
