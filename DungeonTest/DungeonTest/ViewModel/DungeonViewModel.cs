using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using DungeonTest.Model;

namespace DungeonTest.ViewModel
{
    public class DungeonViewModel : ViewModelBase
    {
        private readonly DungeonGameModel _model;

        #region Properties

        /// <summary>
        /// A teljes játéktábla mérete
        /// </summary>
        public Int32 TableSize { get { return _model.TableSize; } }

        /// <summary>
        /// A játékos által látott terület mérete
        /// </summary>
        public Int32 SightTableSize { get { return _model.SightTableSize; } }

        /// <summary>
        /// Van-e égő a játékosnál
        /// </summary>
        public String HasBulb 
        {
            get
            {
                if (!EndGameTimerStarted)
                {
                    if (_model.HasBulb)
                        return "Van égő";
                    else
                        return "Nincs égő";
                }

                return "";
            }
        }

        /// <summary>
        /// Van-e fémlap a játékosnál
        /// </summary>
        public String HasFoil
        {
            get
            {
                if (!EndGameTimerStarted)
                {
                    if (_model.HasFoil)
                        return "Van fémlap";
                    else
                        return "Nincs fémlap";
                }

                return "";
            }
        }

        /// <summary>
        /// Van-e fogaskerék a játékosnál
        /// </summary>
        public String HasGear
        {
            get
            {
                if (!EndGameTimerStarted)
                {
                    if (_model.HasGear)
                        return "Van fogaskerék";
                    else
                        return "Nincs fogaskerék";
                }

                return "";
            }
        }

        /// <summary>
        /// Van-e cső a játékosnál
        /// </summary>
        public String HasPipe
        {
            get
            {
                if (!EndGameTimerStarted)
                {
                    if (_model.HasPipe)
                        return "Van csővezeték";
                    else
                        return "Nincs csővezeték";
                }

                return "";
            }
        }

        /// <summary>
        /// Az eddig eltelt játékidő
        /// </summary>
        public String GameTime { get { return "Eltelt játékidő: " + _model.GameTime + " mp"; } }

        /// <summary>
        /// Elkezdődött-e már a játék végi összeomlás
        /// </summary>
        public Boolean EndGameTimerStarted { get { return _model.TimeLeft >= 0; } }

        /// <summary>
        /// Ennyi ideje van kijutni a játékosnak a játék végi összeomlás kezdetétől kezdve
        /// </summary>
        public Int32 MaxValueOfTimer { get { return _model.MaxValueOfTimer; } }

        /// <summary>
        /// A hátra lévő idő, ameddig a játékosnak be kell fejeznie a játékot, hogy ne veszítsen
        /// </summary>
        public Int32 TimeLeft { get { return _model.TimeLeft == -1 ? 0 : _model.TimeLeft; } }

        /// <summary>
        /// Van-e égő a vizsgált gyárban
        /// </summary>
        public Boolean FactoryHasBulb { get; private set; }

        /// <summary>
        /// Van-e fémlap a vizsgált gyárban
        /// </summary>
        public Boolean FactoryHasFoil { get; private set; }

        /// <summary>
        /// Van-e fogaskerék a vizsgált gyárban
        /// </summary>
        public Boolean FactoryHasGear { get; private set; }

        /// <summary>
        /// Van-e cső a vizsgált gyárban
        /// </summary>
        public Boolean FactoryHasPipe { get; private set; }

        /// <summary>
        /// Azon gyárak száma, amelyeknél még nem lett leadva az összes alkatrész
        /// </summary>
        public String NumberOfFactoriesLeft { get { return !EndGameTimerStarted ? _model.NumberOfFactoriesLeft.ToString() + " gyárba kell még alkatrészeket vinned" : ""; } }

        /// <summary>
        /// A játékosnál lévő kulcs
        /// </summary>
        public String Key
        {
            get { return _model.Key == null ? "Key0" : _model.Key.ToString(); }
        }

        /// <summary>
        /// Életerő
        /// </summary>
        public String HP { get { return "Életerő: " + _model.MaxHP + "/" + _model.CurrentHP; } }

        /// <summary>
        /// A pálya azon része, amelyet megjelenítünk a játékos részére
        /// felügyelt kollekcióba szervezve
        /// </summary>
        public ObservableCollection<DungeonField> Fields { get; set; }

        public DelegateCommand UpCommand { get; private set; }

        public DelegateCommand RightCommand { get; private set; }

        public DelegateCommand DownCommand { get; private set; }

        public DelegateCommand LeftCommand { get; private set; }

        public DelegateCommand QCommand { get; private set; }

        public DelegateCommand SpaceCommand { get; private set; }

        public DelegateCommand ExitCommand { get; private set; }

        public DelegateCommand PauseCommand { get; private set; }

        public DelegateCommand FillFactoriesCommand { get; private set; }

        public DelegateCommand FactoryDetailExitCommand { get; private set; }

        #endregion

        #region Constructor

        public DungeonViewModel(DungeonGameModel model)
        {
            _model = model;
            _model.GameCreated += new EventHandler(Model_GameCreated);
            _model.GameAdvanced += new EventHandler(Model_GameAdvanced);
            _model.FactoryInformation += new EventHandler<Tuple<Boolean, Boolean, Boolean, Boolean>>(Model_FactoryInformation);

            UpCommand = new DelegateCommand(param => _model.Step(-1, 0));
            RightCommand = new DelegateCommand(param => _model.Step(0, 1));
            DownCommand = new DelegateCommand(param => _model.Step(1, 0));
            LeftCommand = new DelegateCommand(param => _model.Step(0, -1));
            QCommand = new DelegateCommand(param => _model.GetDataOfFactory());

            SpaceCommand = new DelegateCommand(param => _model.HandleSpaceCommand());

            ExitCommand = new DelegateCommand(param => OnDisposeGame());

            PauseCommand = new DelegateCommand(param => _model.PauseAndContinueGame());

            FillFactoriesCommand = new DelegateCommand(param => _model.FillFactories());

            FactoryDetailExitCommand = new DelegateCommand(param => OnFactoryDetailExit());
        }

        #endregion

        #region Private Methods

        private void RefreshTable()
        {
            foreach (DungeonField field in Fields)
            {
                try
                {
                    field.Picture = _model.GetSField(field.X, field.Y).ToString();
                }
                catch (ArgumentOutOfRangeException)
                {
                    field.Picture = "Error";
                    OnErrorMessage("A kért elem létezik, mert az indexelés túlmutat a pálya határain. Ha ez a hibaüzenet többször is megjelenik, szakítsa meg a játékot!");
                }
            }
            OnPropertyChanged(nameof(SightTableSize));
            OnPropertyChanged(nameof(Fields));
            OnPropertyChanged(nameof(HasBulb));
            OnPropertyChanged(nameof(HasFoil));
            OnPropertyChanged(nameof(HasGear));
            OnPropertyChanged(nameof(HasPipe));
            OnPropertyChanged(nameof(GameTime));
            OnPropertyChanged(nameof(EndGameTimerStarted));
            OnPropertyChanged(nameof(MaxValueOfTimer));
            OnPropertyChanged(nameof(TimeLeft));
            OnPropertyChanged(nameof(NumberOfFactoriesLeft));
            OnPropertyChanged(nameof(Key));
            OnPropertyChanged(nameof(HP));
        }

        private void InitFields()
        {
            if (Fields != null)
                Fields.Clear();

            Fields = new ObservableCollection<DungeonField>();
            try
            {
                for (Int32 i = 0; i < _model.SightTableSize; ++i)
                {
                    for (Int32 j = 0; j < _model.SightTableSize; ++j)
                        Fields.Add(new DungeonField
                        {
                            Picture = _model.GetSField(i, j).ToString(),
                            X = i,
                            Y = j,
                            Number = i * _model.SightTableSize + j
                        });
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                OnAbortGame();
            }
        }

        #endregion

        #region Game Event Handlers

        private void Model_GameCreated(object sender, EventArgs e)
        {
            InitFields();
            _model.GameIsRunning = true;
            _model.StartTimers();
            RefreshTable();
        }

        private void Model_GameAdvanced(object sender, EventArgs e)
        {
            if (_model.GameIsRunning)
                RefreshTable();
        }

        private void Model_FactoryInformation(object sender, Tuple<Boolean, Boolean, Boolean, Boolean> e)
        {
            FactoryHasBulb = e.Item1;
            FactoryHasFoil = e.Item2;
            FactoryHasGear = e.Item3;
            FactoryHasPipe = e.Item4;

            OnFactoryInformation();
        }

        #endregion

        #region Events

        public event EventHandler FactoryInformation;

        public event EventHandler DisposeGame;

        public event EventHandler FactoryDetailExit;

        public event EventHandler<String> ErrorMessage;

        public event EventHandler AbortGame;

        #endregion

        #region Event triggers

        private void OnFactoryInformation()
        {
            FactoryInformation?.Invoke(this, EventArgs.Empty);
        }

        private void OnDisposeGame()
        {
            DisposeGame?.Invoke(this, EventArgs.Empty);
        }

        private void OnFactoryDetailExit()
        {
            FactoryDetailExit?.Invoke(this, EventArgs.Empty);
        }

        private void OnErrorMessage(String message)
        {
            ErrorMessage?.Invoke(this, message);
        }

        private void OnAbortGame()
        {
            AbortGame?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
