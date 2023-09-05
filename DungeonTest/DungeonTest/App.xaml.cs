using DungeonTest.Model;
using DungeonTest.Persistence;
using DungeonTest.View;
using DungeonTest.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;

namespace DungeonTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Private fields

        private DungeonGameDataAccess _dataAccess;
        private DungeonGameModel _model;
        private DungeonViewModel _gameViewModel;
        private GameWindow _gameWindow;
        private FactoryWindow _factoryWindow;
        private MenuViewModel _menuViewModel;
        private MainMenuWindow _mainMenuWindow;
        private LoadWindow _loadWindow;
        private ToplistWindow _toplistWindow;
        private SaveResultWindow _saveResultWindow;

        #endregion

        #region Constructor

        public App()
        {
            Startup += new StartupEventHandler(App_Startup);
        }

        #endregion

        #region Application event handler

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _dataAccess = new DungeonGameDataAccess(@"Resources\");
            _model = new DungeonGameModel(_dataAccess);
            _model.GameCreated += new EventHandler(Model_GameCreated);
            _model.GameError += new EventHandler<String>(ErrorMessage);

            _gameViewModel = new DungeonViewModel(_model);
            _gameViewModel.FactoryInformation += new EventHandler(GameViewModel_FactoryInformation);
            _gameViewModel.DisposeGame += new EventHandler(GameViewModel_DisposeGame);
            _gameViewModel.FactoryDetailExit += new EventHandler(GameViewModel_FactoryDetailExit);
            _gameViewModel.ErrorMessage += new EventHandler<String>(ErrorMessage);
            _gameViewModel.AbortGame += new EventHandler(GameViewModel_AbortGame);

            _menuViewModel = new MenuViewModel(_model);
            _menuViewModel.LoadMapOpen += new EventHandler(MenuViewModel_LoadMapOpen);
            _menuViewModel.LoadMapClose += new EventHandler<String>(MenuViewModel_LoadMapClose);
            _menuViewModel.OpenToplist += new EventHandler(MenuViewModel_OpenToplist);
            _menuViewModel.CloseToplist += new EventHandler(MenuViewModel_CloseToplist);
            _menuViewModel.ExitRequested += new EventHandler(MenuViewModel_ExitRequested);
            _menuViewModel.GameOver += new EventHandler<Tuple<Boolean, Int32>>(MenuViewModel_GameOver);
            _menuViewModel.CloseResult += new EventHandler(MenuViewModel_CloseResult);
            _menuViewModel.CancelMapLoad += new EventHandler(MenuViewModel_CancelMapLoad);
            _menuViewModel.ErrorMessage += new EventHandler<String>(MenuViewModel_ErrorMessage);

            _mainMenuWindow = new MainMenuWindow { DataContext = _menuViewModel };
            _mainMenuWindow.Closing += new CancelEventHandler(MainMenuWindow_Closing);
            _mainMenuWindow.Show();
        }

        #endregion

        #region Model event handlers

        private void Model_GameCreated(object sender, EventArgs e)
        {
            if (_gameWindow != null)
                _gameWindow.Close();

            _gameWindow = new GameWindow { DataContext = _gameViewModel };
            _gameWindow.Show();

            _mainMenuWindow.Hide();
        }

        private void ErrorMessage(object sender, String e)
        {
            MessageBox.Show(e, "Hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region MenuViewModel event handlers

        private void MenuViewModel_LoadMapOpen(object sender, EventArgs e)
        {
            _mainMenuWindow.Hide();
            _menuViewModel.SelectedGame = null; // kezdetben nincsen kiválasztott elem

            _loadWindow = new LoadWindow { DataContext = _menuViewModel };
            _loadWindow.ShowDialog();
        }

        private void MenuViewModel_LoadMapClose(object sender, String name)
        {
            if (name != null)
            {
                try
                {
                    _model.NewGame(name);
                }
                catch
                {
                    MessageBox.Show("Játék betöltése sikertelen!", "Hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                    _model.DisposeGame();
                    this.Dispatcher.Invoke(() => _mainMenuWindow.Show());
                }
            }

            this.Dispatcher.Invoke(() => _loadWindow.Close());            
        }

        private void MenuViewModel_OpenToplist(object sender, EventArgs e)
        {
            _mainMenuWindow.Hide();
            _toplistWindow = new ToplistWindow { DataContext = _menuViewModel };
            _toplistWindow.Show();
        }

        private void MenuViewModel_CloseToplist(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => _toplistWindow.Close());
            this.Dispatcher.Invoke(() => _mainMenuWindow.Show());
        }

        private void MenuViewModel_ExitRequested(object sender, EventArgs e)
        {

            _model.SaveToplist();

            if (_factoryWindow != null)
                this.Dispatcher.Invoke(() => _factoryWindow.Close());
            if (_loadWindow != null)
                this.Dispatcher.Invoke(() => _loadWindow.Close());
            if (_toplistWindow != null)
                this.Dispatcher.Invoke(() => _toplistWindow.Close());
            if (_saveResultWindow != null)
                this.Dispatcher.Invoke(() => _saveResultWindow.Close());
            if (_gameWindow != null)
                this.Dispatcher.Invoke(() => _gameWindow.Close());

            this.Dispatcher.Invoke(() => _mainMenuWindow.Close());
        }

        private void MenuViewModel_GameOver(object sender, Tuple<Boolean, Int32> tuple)
        {
            if (tuple.Item1)
            {
                if (_saveResultWindow != null)
                    this.Dispatcher.Invoke(() => _saveResultWindow.Close());

                _saveResultWindow = new SaveResultWindow { DataContext = _menuViewModel };
                _saveResultWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Elvesztetted a játékot!", "Játék vége", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }

            this.Dispatcher.Invoke(() => _gameWindow.Close());
            this.Dispatcher.Invoke(() => _mainMenuWindow.Show());
        }

        private void MenuViewModel_CloseResult(object sender, EventArgs e) => this.Dispatcher.Invoke(() => _saveResultWindow.Close());

        private void MenuViewModel_CancelMapLoad(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => _loadWindow.Close());
            this.Dispatcher.Invoke(() => _mainMenuWindow.Show());
        }

        private void MenuViewModel_ErrorMessage(object sender, String e)
        {
            this.Dispatcher.Invoke(() => _loadWindow.Close());
            MessageBox.Show(e, "Hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region GameViewModel event handlers

        private void GameViewModel_FactoryInformation(object sender, EventArgs e)
        {
            _factoryWindow = new FactoryWindow
            {
                DataContext = _gameViewModel
            };

            _factoryWindow.ShowDialog();
        }

        private void GameViewModel_DisposeGame(object sender, EventArgs e)
        {
            _model.PauseAndContinueGame();
            if (MessageBox.Show("Biztosan ki akarsz lépni a játékból?", "Kilépés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _gameWindow?.Close();
                _model.DisposeGame();
                _mainMenuWindow.Show();
            }
            else
            {
                _model.PauseAndContinueGame();
            }
        }

        private void GameViewModel_FactoryDetailExit(object sender, EventArgs e)
        {
            _factoryWindow?.Close();
            _model.PauseAndContinueGame();
        }

        private void GameViewModel_AbortGame(object sender, EventArgs e)
        {
            _gameWindow?.Close();
            _model.DisposeGame();
            _mainMenuWindow.Show();

            MessageBox.Show("Hiba történt a játék inicializálása közben.", "Hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region MainMenuView event handlers

        private void MainMenuWindow_Closing(object sender, CancelEventArgs e)
        {
            _model.SaveToplist();

            if (_factoryWindow != null)
                this.Dispatcher.Invoke(() => _factoryWindow.Close());
            if (_loadWindow != null)
                this.Dispatcher.Invoke(() => _loadWindow.Close());
            if (_toplistWindow != null)
                this.Dispatcher.Invoke(() => _toplistWindow.Close());
            if (_saveResultWindow != null)
                this.Dispatcher.Invoke(() => _saveResultWindow.Close());
            if (_gameWindow != null)
                this.Dispatcher.Invoke(() => _gameWindow.Close());

        }

        #endregion
    }
}
