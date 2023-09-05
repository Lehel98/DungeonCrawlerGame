using System;
using DungeonTest.Persistence;

namespace DungeonTest.Model
{
    public class Land
    {
        public Land(Field baseValue, Boolean isOccupied, Int32 delayUntilNextMove = 100)
        {
            BaseValue = baseValue;
            CurrentValue = baseValue;
            IsOccupied = isOccupied;
            DelayUntilNextMove = delayUntilNextMove;
        }

        public Land(Field baseValue, Field currentValue, Boolean isOccupied, Int32 delayUntilNextMove = 100)
        {
            BaseValue = baseValue;
            CurrentValue = currentValue;
            IsOccupied = isOccupied;
            DelayUntilNextMove = delayUntilNextMove;
        }

        public void ChangeLand(Field baseValue, Boolean isOccupied)
        {
            BaseValue = baseValue;
            CurrentValue = baseValue;
            IsOccupied = isOccupied;
        }

        /// <summary>
        /// Ez a mező abszolút értéke. Ha elhagyjuk,
        /// akkor ezt az értéket fogja visszakapni
        /// </summary>
        public Field BaseValue { get; set; }

        /// <summary>
        /// A mező jelenlegi értéke
        /// </summary>
        public Field CurrentValue { get; set; }

        /// <summary>
        /// Annak megállapítása, hogy ez egy olyan
        /// típusú mező-e, amelyre rá szabad lépni
        /// </summary>
        public Boolean IsOccupied { get; private set; }

        /// <summary>
        /// Tárolja azt az időt (miliszekundumokban), amennyit
        /// várnia kell a játékosnak, ha rálép erre a mezőre
        /// </summary>
        public Int32 DelayUntilNextMove { get; }
    }
}
