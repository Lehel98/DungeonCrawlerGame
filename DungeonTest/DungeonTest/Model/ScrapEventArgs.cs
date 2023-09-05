using System;
using DungeonTest.Persistence;

namespace DungeonTest.Model
{
    public class ScrapEventargs : EventArgs
    {
        public Tuple<Int32, Int32> Pair { get; set; }

        public Field Type { get; set; }
    }
}
