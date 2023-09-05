using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DungeonTest.Persistence
{
    public class DungeonGameDataAccess : IDungeonGameDataAccess
    {
        private String _saveDirectory;
        private Toplist _toplist;

        public DungeonGameDataAccess(String saveDirectory)
        {
            _saveDirectory = saveDirectory;
            LoadToplist();
        }

        public InitMapData LoadMap(String path)
        {
            try
            {
                path = Path.Combine(Environment.CurrentDirectory, _saveDirectory, path + ".map");

                InitMapData initMapData = new InitMapData();

                using (StreamReader reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    initMapData.TableSize = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    initMapData.NumberOfBearTraps = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    initMapData.NumberOfBushes = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    initMapData.NumberOfPuddles = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    initMapData.NumberOfFactories = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (initMapData.NumberOfFactories <= 0) initMapData.NumberOfFactories = 1;

                    initMapData.NumberOfHeals = Convert.ToInt32(reader.ReadLine().Split('=')[1]);

                    initMapData.Scraps = new List<Field>();
                    Int32 numberOfBulbs = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (numberOfBulbs <= 0) numberOfBulbs = 1;
                    for (Int32 i = 0; i < numberOfBulbs; i++) initMapData.Scraps.Add(Field.Bulb);
                    Int32 numberOfFoils = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (numberOfFoils <= 0) numberOfFoils = 1;
                    for (Int32 i = 0; i < numberOfFoils; i++) initMapData.Scraps.Add(Field.Foil);
                    Int32 numberOfGears = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (numberOfGears <= 0) numberOfGears = 1;
                    for (Int32 i = 0; i < numberOfGears; i++) initMapData.Scraps.Add(Field.Gear);
                    Int32 numberOfPipes = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (numberOfPipes <= 0) numberOfPipes = 1;
                    for (Int32 i = 0; i < numberOfPipes; i++) initMapData.Scraps.Add(Field.Pipe);

                    initMapData.EnemySpeed = Convert.ToInt32(reader.ReadLine().Split('=')[1]);

                    initMapData.EndGameTime = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    if (initMapData.EndGameTime <= 0) initMapData.EndGameTime = 60;

                    initMapData.Rooms = new List<Tuple<Int32, Int32>>(Convert.ToInt32(reader.ReadLine().Split('=')[1]));
                    for (Int32 i = 0; i < initMapData.Rooms.Capacity; i++)
                    {
                        String[] strArray = reader.ReadLine().Split(' ');
                        initMapData.Rooms.Add(new Tuple<Int32, Int32>(Convert.ToInt32(strArray[0]), Convert.ToInt32(strArray[1])));
                    }
                }

                return initMapData;
            }
            catch { throw new DungeonGameDataException(); }
        }
        
        public ICollection<SaveEntry> List()
        {
            try
            {
                List<SaveEntry> list = Directory.GetFiles(_saveDirectory, "*.map")
                        .Select(path => new SaveEntry
                        {
                            Name = Path.GetFileNameWithoutExtension(path)
                        })
                        .ToList();

                foreach (SaveEntry entry in list)
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, _saveDirectory, entry.Name + ".map")))
                    {
                        entry.Difficulty = Convert.ToInt32(reader.ReadLine().Split('=')[1]);
                    }
                }

                return list.OrderBy(x => x.Difficulty).ToList();
            }
            catch { throw new DungeonGameDataException(); }
        }

        private void LoadToplist()
        {
            List<SaveEntry> list = new List<SaveEntry>(List());

            _toplist = new Toplist();

            foreach (SaveEntry map in list)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, map.Name + ".list")))
                    {
                        Int32 length = Convert.ToInt32(reader.ReadLine());

                        for (Int32 i = 0; i < length; ++i)
                        {
                            String[] line = reader.ReadLine().Split(' ');

                            _toplist.AddResult(line[0], line[1], Convert.ToInt32(line[2]));
                        }
                    }
                }
                catch (Exception) { }
            }

            _toplist.MergeSortAll();
        }

        public void SaveToplist()
        {
            _toplist.MergeSortAll();

            foreach (String map in new List<String>(_toplist.Maps))
            {
                try
                {
                    using (StreamWriter writer = File.CreateText(Path.Combine(Environment.CurrentDirectory, map + ".list")))
                    {
                        writer.WriteLine(_toplist[map].Count);

                        for (Int32 i = 0; i < _toplist[map].Count; ++i)
                        {
                            writer.WriteLine(map + " " + _toplist[map][i].Item1 + " " + _toplist[map][i].Item2);
                        }
                    }
                }
                catch { throw new DungeonGameDataException(); }
            }
        }

        public void AddResult(String map, String player, Int32 result)
        {
            _toplist.AddResult(map, player, result);
            _toplist.MergeSortAll();
        }

        public List<String> GetToplist() => _toplist.GetToplist;
                
    }
}
