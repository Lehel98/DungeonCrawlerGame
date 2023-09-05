using System;

namespace DungeonTest.ViewModel
{
    public class DungeonField : ViewModelBase
    {
        #region Private Fields

        private String _picture;

        #endregion

        #region Properties

        public String Picture
        {
            get { return _picture; }
            set
            {
                if (_picture != value)
                {
                    _picture = value;
                    OnPropertyChanged();
                }
            }
        }

        public Int32 X { get; set; }

        public Int32 Y { get; set; }

        public Int32 Number { get; set; }

        #endregion
    }
}
