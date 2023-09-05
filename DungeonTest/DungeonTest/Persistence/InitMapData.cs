using System;
using System.Collections.Generic;

namespace DungeonTest.Persistence
{
    public class InitMapData
    {
        public Int32 TableSize { get; set; }

        public Int32 NumberOfBearTraps { get; set; }

        public Int32 NumberOfBushes { get; set; }

        public Int32 NumberOfPuddles { get; set; }

        public Int32 NumberOfFactories { get; set; }

        public Int32 NumberOfHeals { get; set; }

        public Int32 EnemySpeed { get; set; }

        public Int32 EndGameTime { get; set; }

        public List<Tuple<Int32, Int32>> Rooms { get; set; }

        public List<Field> Scraps { get; set; }

        public InitMapData() { }

        public void Clear()
        {
            TableSize = 0;
            NumberOfBearTraps = 0;
            NumberOfBushes = 0;
            NumberOfPuddles = 0;
            NumberOfFactories = 0;
            NumberOfHeals = 0;
            EnemySpeed = 0;
            EndGameTime = 0;
            Rooms.Clear();
            Scraps.Clear();
        }
    }
}
