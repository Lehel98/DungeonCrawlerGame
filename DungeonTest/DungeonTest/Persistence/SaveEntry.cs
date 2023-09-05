using System;

namespace DungeonTest.Persistence
{
    public class SaveEntry
    {
        /// <summary>
        /// Mentés neve vagy elérési útja
        /// </summary>
        public String Name { get; set; }
        
        /// <summary>
        /// Nehézségi szint
        /// </summary>
        public Int32 Difficulty { get; set; }
    }
}
