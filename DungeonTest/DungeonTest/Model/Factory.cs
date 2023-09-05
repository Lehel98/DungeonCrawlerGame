using System;
using DungeonTest.Persistence;

namespace DungeonTest.Model
{
    public class Factory
    {
        public Factory(Int32 x, Int32 y)
        {
            X = x;
            Y = y;
            HasBulb = false;
            HasFoil = false;
            HasGear = false;
            HasPipe = false;
        }

        /// <summary>
        /// X koordináta
        /// </summary>
        public Int32 X { get; }

        /// <summary>
        /// Y koordináta
        /// </summary>
        public Int32 Y { get; }

        /// <summary>
        /// Ha a gyárba elvittük az összes alkatrészt, akkor bezár.
        /// </summary>
        public Boolean IsClosed { get { return HasBulb && HasFoil && HasGear && HasPipe; } }

        /// <summary>
        /// Van-e égő a játékosnál
        /// </summary>
        public Boolean HasBulb { get; private set; }

        /// <summary>
        /// Van-e fémlap a játékosnál
        /// </summary>
        public Boolean HasFoil { get; private set; }

        /// <summary>
        /// Van-e fogaskerék a játékosnál
        /// </summary>
        public Boolean HasGear { get; private set; }

        /// <summary>
        /// Van-e cső a játékosnál
        /// </summary>
        public Boolean HasPipe { get; private set; }

        public Boolean AddScrap(Field scrap)
        {
            if (IsClosed)
                return false;

            Boolean scrapTaken = false;

            switch (scrap)
            {
                case Field.Bulb:
                    if (!HasBulb)
                    {
                        scrapTaken = true;
                        HasBulb = true;
                    }
                    break;
                case Field.Foil:
                    if (!HasFoil)
                    {
                        scrapTaken = true;
                        HasFoil = true;
                    }
                    break;
                case Field.Gear:
                    if (!HasGear)
                    {
                        scrapTaken = true;
                        HasGear = true;
                    }
                    break;
                case Field.Pipe:
                    if (!HasPipe)
                    {
                        scrapTaken = true;
                        HasPipe = true;
                    }
                    break;
            }

            if (IsClosed)
                OnClosingFactory();

            return scrapTaken;
        }

        public event EventHandler<Tuple<Int32, Int32>> ClosingFactory;

        private void OnClosingFactory()
        {
            ClosingFactory?.Invoke(this, new Tuple<Int32, Int32>(X, Y));
        }
    }
}
