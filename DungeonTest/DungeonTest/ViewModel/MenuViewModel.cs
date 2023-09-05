using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DungeonTest.Persistence;
using DungeonTest.Model;

namespace DungeonTest.ViewModel
{
    public class MenuViewModel : ViewModelBase
    {
        private readonly DungeonGameModel _model;
        private SaveEntry _selectedGame;
        private String _newName = String.Empty;

        #region Properties

        public SaveEntry SelectedGame
        {
            get { return _selectedGame; }
            set
            {
                _selectedGame = value;
                if (_selectedGame != null)
                    NewName = String.Copy(_selectedGame.Name);

                OnPropertyChanged();
                LoadMapCloseCommand.RaiseCanExecuteChanged();
            }
        }

        public String NewName
        {
            get { return _newName; }
            set
            {
                _newName = value;

                OnPropertyChanged();
            }
        }

        public String MapName { get { return "Pálya neve: " + _model.CurrentMap; } }

        public String Result { get; private set; }

        public String PlayerName { get; set; }

        /// <summary>
        /// Toplista
        /// </summary>
        public ObservableCollection<String> Toplist { get; set; }

        public ObservableCollection<SaveEntry> Games { get; set; }

        public DelegateCommand LoadMapOpenCommand { get; private set; }

        public DelegateCommand LoadMapCloseCommand { get; private set; }

        public DelegateCommand PlayCommand { get; private set; }

        public DelegateCommand ToplistCommand { get; private set; }

        public DelegateCommand CloseToplistCommand { get; private set; }

        public DelegateCommand ExitCommand { get; private set; }

        public DelegateCommand CloseResultCommand { get; private set; }

        public DelegateCommand CancelMapLoadCommand { get; private set; }

        #endregion

        #region Constructor

        public MenuViewModel(DungeonGameModel model)
        {
            _model = model;
            _model.GameCreated += new EventHandler(Model_GameCreated);
            _model.GameOver += new EventHandler<Tuple<Boolean, Int32>>(Model_GameOver);

            LoadMapOpenCommand = new DelegateCommand(param =>
            {
                try
                {
                    Games = new ObservableCollection<SaveEntry>(_model.ListMaps());
                    OnLoadMapOpen();
                }
                catch (InvalidOperationException ex)
                {
                    OnErrorMessage(ex.Message);
                }
            });
            LoadMapCloseCommand = new DelegateCommand(param =>
            {
                if (SelectedGame != null)
                {
                    OnLoadMapClose(SelectedGame.Name);
                }
            });

            ToplistCommand = new DelegateCommand(param => OnOpenToplist());

            CloseToplistCommand = new DelegateCommand(param => OnCloseToplist());

            ExitCommand = new DelegateCommand(param => OnExitRequested());

            CloseResultCommand = new DelegateCommand(param => SaveResult());

            CancelMapLoadCommand = new DelegateCommand(param => OnCancelMapLoad());
        }

        #endregion

        private void SaveResult()
        {
            if (PlayerName != String.Empty)
            {
                _model.AddResult(PlayerName, Convert.ToInt32(Result.Split(' ')[0]));
                OnCloseResult();
            }
        }

        #region InitOrRefreshTopList

        private void InitOrRefreshToplist()
        {
            if (Toplist != null)
                Toplist.Clear();

            Toplist = new ObservableCollection<String>(_model.Toplist);
            OnPropertyChanged(nameof(Toplist));
        }

        #endregion

        #region Events

        /// <summary>
        /// Betöltő ablak megnyitásának eseménye
        /// </summary>
        public event EventHandler LoadMapOpen;

        /// <summary>
        /// Betöltő ablak bezárásának eseménye
        /// </summary>
        public event EventHandler<String> LoadMapClose;

        /// <summary>
        /// Toplista ablak megnyitásának eseménye
        /// </summary>
        public event EventHandler OpenToplist;

        /// <summary>
        /// Toplista ablak bezárásának eseménye
        /// </summary>
        public event EventHandler CloseToplist;

        /// <summary>
        /// Kilépés eseménye
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// Játék vége esemény
        /// </summary>
        public event EventHandler<Tuple<Boolean, Int32>> GameOver;

        /// <summary>
        /// Játék mentése ablak bezárása eseménye
        /// </summary>
        public event EventHandler CloseResult;

        public event EventHandler CancelMapLoad;

        public event EventHandler<String> ErrorMessage;

        #endregion

        #region Event triggers

        /// <summary>
        /// Betöltő ablak megnyitása esemény kiváltása
        /// </summary>
        private void OnLoadMapOpen()
        {
            LoadMapOpen?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Betöltő ablak bezárása esemény kiváltása
        /// </summary>
        /// <param name="name">Fájlnév.</param>
        private void OnLoadMapClose(String name)
        {
            LoadMapClose?.Invoke(this, name);
        }

        /// <summary>
        /// Toplista ablak megnyitása esemény kiváltása
        /// </summary>
        private void OnOpenToplist()
        {
            InitOrRefreshToplist();
            OpenToplist?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Toplista ablak bezárása esemény kiváltása
        /// </summary>
        private void OnCloseToplist()
        {
            CloseToplist?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Kilépés esemény kiváltása
        /// </summary>
        private void OnExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Játék vége esemény kiváltása
        /// </summary>
        /// <param name="tuple">Győzött-e a játékos, illetve a játék idő</param>
        private void OnGameOver(Tuple<Boolean, Int32> tuple)
        {
            GameOver?.Invoke(this, tuple);
        }

        /// <summary>
        /// Játék mentése ablak bezárása esemény kiváltása
        /// </summary>
        private void OnCloseResult()
        {
            CloseResult?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancelMapLoad()
        {
            CancelMapLoad?.Invoke(this, EventArgs.Empty);
        }

        private void OnErrorMessage(String message)
        {
            ErrorMessage?.Invoke(this, message);
        }

        #endregion

        #region Event handlers

        private void Model_GameCreated(object sender, EventArgs e)
        {
            PlayerName = String.Empty;
            OnPropertyChanged(nameof(PlayerName));
        }

        private void Model_GameOver(object sender, Tuple<Boolean, Int32> e)
        {
            Result = e.Item2.ToString() + " másodperc alatt jutottál ki labirintusból";
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(MapName));
            OnGameOver(e);
        }

        #endregion
    }
}
