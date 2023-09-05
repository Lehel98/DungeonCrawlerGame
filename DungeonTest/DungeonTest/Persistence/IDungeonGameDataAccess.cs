using System;
using System.Collections.Generic;

namespace DungeonTest.Persistence
{
    public interface IDungeonGameDataAccess
    {
        InitMapData LoadMap(String path);

        ICollection<SaveEntry> List();

        void SaveToplist();

        void AddResult(String map, String player, Int32 result);

        List<String> GetToplist();
    }
}
