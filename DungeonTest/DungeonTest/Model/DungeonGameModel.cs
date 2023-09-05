using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DungeonTest.Persistence;

namespace DungeonTest.Model
{
    public class DungeonGameModel
    {
        #region Private fields

        private List<List<Land>> _table;
        private List<List<Field>> _sight, _enemyMap;
        private Dictionary<Tuple<Int32, Int32>, Scrap> _scraps;
        private Dictionary<Tuple<Int32, Int32>, Factory> _factories;
        private readonly IDungeonGameDataAccess _dataAccess;
        private InitMapData _initMapData;
        private readonly Random _random;
        private Timer _gameTimer, _endGameTimer, _stepTimer, _enemyStepTimer;
        private List<Tuple<Int32, Int32>> _factoriesExploredByEnemy, _enemyPath;
        private Queue<Tuple<Int32, Int32>> _enemyPathQueue;
        private Tuple<Int32, Int32> _enemyPreviousField;
        private Dictionary<Field, Tuple<Int32, Int32>> _gates;

        #endregion

        #region Constructor

        public DungeonGameModel(IDungeonGameDataAccess dataAccess)
        {
            _random = new Random();

            _table = new List<List<Land>>();

            _dataAccess = dataAccess;
        }

        #endregion

        #region General properties

        /// <summary>
        /// A teljes játéktábla mérete
        /// </summary>
        public Int32 TableSize { get { return _table.Count; } }

        public Land GetLand(Int32 x, Int32 y)
        {
            if (x < 0 || x >= TableSize)
                throw new ArgumentOutOfRangeException("x", "X coordinate is out of range");

            if (y < 0 || y >= TableSize)
                throw new ArgumentOutOfRangeException("y", "Y coordinate is out of range");

            return _table[x][y];
        }

        public Field GetField(Int32 x, Int32 y)
        {
            if (x < 0 || x >= TableSize)
                throw new ArgumentOutOfRangeException("x", "X coordinate is out of range");

            if (y < 0 || y >= TableSize)
                throw new ArgumentOutOfRangeException("y", "Y coordinate is out of range");

            return _table[x][y].CurrentValue;
        }

        public Field GetBaseValue(Int32 x, Int32 y)
        {
            if (x < 0 || x >= TableSize)
                throw new ArgumentOutOfRangeException("x", "X coordinate is out of range");

            if (y < 0 || y >= TableSize)
                throw new ArgumentOutOfRangeException("y", "Y coordinate is out of range");

            return _table[x][y].BaseValue;
        }

        /// <summary>
        /// Toplista
        /// </summary>
        public List<String> Toplist => _dataAccess.GetToplist();

        /// <summary>
        /// Pálya
        /// </summary>
        public String CurrentMap { get; private set; }

        /// <summary>
        /// Fut-e a játék vagy sem
        /// </summary>
        public Boolean GameIsRunning { get; set; }

        /// <summary>
        /// Maximális életerő
        /// </summary>
        public Int32 MaxHP { get { return 5; } }

        /// <summary>
        /// Aktuális életerő
        /// </summary>
        public Int32 CurrentHP { get; private set; }

        #endregion

        #region Player properties

        /// <summary>
        /// A játékos által látott terület mérete
        /// </summary>
        public Int32 SightTableSize { get { return 19; } }

        /// <summary>
        /// Mező értékének lekérdezése
        /// </summary>
        /// <param name="x">X koordináta</param>
        /// <param name="y">Y koordináta</param>
        /// <returns>A mező értékének</returns>
        /// <exception cref="ArgumentOutOfRangeException">Ha az x vagy az y értéke nem megfelelő</exception>
        public Field GetSField(Int32 x, Int32 y)
        {
            if (x < 0 || x >= SightTableSize)
                throw new ArgumentOutOfRangeException("x", "X coordinate is out of range");

            if (y < 0 || y >= SightTableSize)
                throw new ArgumentOutOfRangeException("y", "Y coordinate is out of range");

            return _sight[x][y];
        }

        /// <summary>
        /// Játékos helyzetének X koordinátája
        /// </summary>
        public Int32 PlayerX { get; private set; }

        /// <summary>
        /// Játékos helyzetének Y koordinátája
        /// </summary>
        public Int32 PlayerY { get; private set; }

        /// <summary>
        /// A játékos el van-e bújva
        /// </summary>
        public Boolean PlayerIsHidden { get; private set; }

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

        /// <summary>
        /// Az eddig eltelt játékidő
        /// </summary>
        public Int32 GameTime { get; private set; }

        /// <summary>
        /// Ennyi ideje van kijutni a játékosnak a játék végi összeomlás kezdetétől kezdve
        /// </summary>
        public Int32 MaxValueOfTimer { get; private set; }

        /// <summary>
        /// A hátra lévő idő, ameddig a játékosnak be kell fejeznie a játékot, hogy ne veszítsen
        /// </summary>
        public Int32 TimeLeft { get; private set; }

        /// <summary>
        /// Aktivált állapotban vannak-e a kijáratok
        /// </summary>
        public Boolean ExitGatesArePowered { get; private set; }

        /// <summary>
        /// A játékosnál lévő kulcs
        /// </summary>
        public Nullable<Field> Key { get; private set; }

        /// <summary>
        /// Akkor igaz, ha a játékosnak engedélyezve van, hogy lépjen
        /// </summary>
        public Boolean CanStep { get; set; }

        /// <summary>
        /// Azon gyárak száma, amelyeknél még nem lett leadva az összes alkatrész
        /// </summary>
        public Int32 NumberOfFactoriesLeft
        {
            get
            {
                if (_factories == null)
                    return 0;

                Int32 count = 0;

                foreach (var factory in _factories.Values)
                {
                    if (!factory.IsClosed)
                        ++count;
                }

                return count;
            }
        }

        #endregion

        #region Enemy properties

        public Int32 EnemyX { get; private set; }

        public Int32 EnemyY { get; private set; }

        public Boolean EnemyTimerIsRunning { get; private set; }

        #endregion

        #region Public methods

        public void PauseAndContinueGame()
        {
            if (_gameTimer == null)
                return;

            if (_gameTimer.Enabled)
                _gameTimer.Stop();
            else
                _gameTimer.Start();

            if (_enemyStepTimer == null)
                return;

            if (_enemyStepTimer.Enabled)
            {
                _enemyStepTimer.Stop();
                EnemyTimerIsRunning = false;
            }
            else
            {
                _enemyStepTimer.Start();
                EnemyTimerIsRunning = true;
            }
        }

        public void FillFactories()
        {
            if (!GameIsRunning || ExitGatesArePowered)
                return;

            if (_factories != null)
            {
                foreach (var factory in _factories.Values)
                {
                    factory.AddScrap(Field.Bulb);
                    factory.AddScrap(Field.Foil);
                    factory.AddScrap(Field.Gear);
                    factory.AddScrap(Field.Pipe);
                }
                foreach (var tuple in _factories.Keys)
                {
                    _table[tuple.Item1][tuple.Item2].BaseValue = _table[tuple.Item1][tuple.Item2].CurrentValue = Field.ClosedFactory;
                }
            }

            InitializeEndGameCollapse();

            Key = Field.Key1;

            for (Int32 i = 1; i < TableSize - 1; i++)
            {
                for (Int32 j = 1; j < TableSize - 1; j++)
                {
                    if (_table[i][j].CurrentValue == Field.Key1)
                        _table[i][j].CurrentValue = _table[i][j].BaseValue;
                }
            }

            OnGameAdvanced();
        }

        /// <summary>
        /// Új játék létrehozása
        /// </summary>
        /// <param name="mapName">A választott pálya neve</param>
        public void NewGame(String mapName)
        {
            EnemyTimerIsRunning = true;
            GameIsRunning = false;

            PlayerIsHidden = false;

            ExitGatesArePowered = false;

            CurrentMap = mapName;

            _initMapData = _dataAccess.LoadMap(mapName);

            InitializeMap(mapName);

            InitializeEnemy();

            HasBulb = false;
            HasFoil = false;
            HasGear = false;
            HasPipe = false;

            Key = null;

            CurrentHP = MaxHP;

            GameTime = 0;
            MaxValueOfTimer = _initMapData.EndGameTime;
            TimeLeft = -1;

            if (_stepTimer != null)
                _stepTimer.Dispose();

            _stepTimer = new Timer
            {
                Interval = 1000,
                AutoReset = false,
                Enabled = false,
            };
            _stepTimer.Elapsed += new ElapsedEventHandler(StepTimer_Elapsed);

            CanStep = true;

            if (_gameTimer != null)
                _gameTimer.Dispose();

            _gameTimer = new Timer
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = false,
            };
            _gameTimer.Elapsed += new ElapsedEventHandler(GameTimer_Elapsed);

            OnGameCreated();
        }

        public void StartTimers()
        {
            _gameTimer.Enabled = true;
            _enemyStepTimer.Enabled = true;
            _gameTimer.Start();
            _enemyStepTimer.Start();
        }

        /// <summary>
        /// Egy lépés lekezelése
        /// </summary>
        /// <param name="x">A lépés irányának x koordinátája</param>
        /// <param name="y">A lépés irányának y koordinátája</param>
        public void Step(Int32 x, Int32 y)
        {
            if (!GameIsRunning || !CanStep)
                return;

            if (!(x == 1 && y == 0 || x == -1 && y == 0 || x == 0 && y == 1 || x == 0 && y == -1))
                return;

            if (PlayerX + x < 0 || PlayerX + x >= TableSize || PlayerY + y < 0 || PlayerY + y >= TableSize)
                return;

            if (_table[PlayerX + x][PlayerY + y].IsOccupied)
                return;

            Boolean moveHappened = false, steppedInTrap = false;

            switch (_table[PlayerX + x][PlayerY + y].CurrentValue)
            {
                case Field.FreeTile:
                    moveHappened = ChangeFields(x, y, Field.Player, false);
                    break;
                case Field.Bush:
                    moveHappened = ChangeFields(x, y, Field.HiddenPlayer, true);
                    break;
                case Field.Puddle:
                    moveHappened = ChangeFields(x, y, Field.PlayerInPuddle, false);
                    break;
                case Field.BearTrap:
                    steppedInTrap = true;
                    _table[PlayerX + x][PlayerY + y] = new Land(Field.FreeTile, false);
                    moveHappened = ChangeFields(x, y, Field.TrappedPlayer, false);
                    --CurrentHP;
                    break;
                case Field.Heal:
                    if (CurrentHP < MaxHP)
                    {
                        moveHappened = ChangeFields(x, y, Field.Player, false);
                        ++CurrentHP;
                    }
                    break;
                case Field.Bulb:
                    if (!HasBulb)
                    {
                        moveHappened = ChangeFields(x, y, Field.Player, false);

                        _scraps[new Tuple<int, int>(PlayerX, PlayerY)].StartTimer();
                        
                        HasBulb = true;
                    }
                    break;
                case Field.Foil:
                    if (!HasFoil)
                    {
                        moveHappened = ChangeFields(x, y, Field.Player, false);

                        _scraps[new Tuple<int, int>(PlayerX, PlayerY)].StartTimer();

                        HasFoil = true;
                    }
                    break;
                case Field.Gear:
                    if (!HasGear)
                    {
                        moveHappened = ChangeFields(x, y, Field.Player, false);

                        _scraps[new Tuple<int, int>(PlayerX, PlayerY)].StartTimer();

                        HasGear = true;
                    }
                    break;
                case Field.Pipe:
                    if (!HasPipe)
                    {
                        moveHappened = ChangeFields(x, y, Field.Player, false);

                        _scraps[new Tuple<int, int>(PlayerX, PlayerY)].StartTimer();

                        HasPipe = true;
                    }
                    break;
                case Field.Key1:
                    moveHappened = ChangeFields(x, y, Field.Player, false);
                    Nullable<Field> key1 = Key;
                    Key = Field.Key1;

                    if (key1 != null)
                        _table[PlayerX][PlayerY].BaseValue = key1.Value;
                    break;
                case Field.Key2:
                    moveHappened = ChangeFields(x, y, Field.Player, false);
                    Nullable<Field> key2 = Key;
                    Key = Field.Key2;

                    if (key2 != null)
                        _table[PlayerX][PlayerY].BaseValue = key2.Value;
                    break;
                case Field.Key3:
                    moveHappened = ChangeFields(x, y, Field.Player, false);
                    Nullable<Field> key3 = Key;
                    Key = Field.Key3;

                    if (key3 != null)
                        _table[PlayerX][PlayerY].BaseValue = key3.Value;
                    break;
                case Field.Key4:
                    moveHappened = ChangeFields(x, y, Field.Player, false);
                    Nullable<Field> key4 = Key;
                    Key = Field.Key4;

                    if (key4 != null)
                        _table[PlayerX][PlayerY].BaseValue = key4.Value;
                    break;
            }

            if (CurrentHP <= 0)
            {
                OnGameAdvanced();
                DisposeGame();
                OnGameOver(new Tuple<Boolean, Int32>(false, -1));
                return;
            }
            else
            {
                if (moveHappened)
                {
                    if (_table[PlayerX - x][PlayerY - y].BaseValue == Field.Bulb ||
                        _table[PlayerX - x][PlayerY - y].BaseValue == Field.Foil ||
                        _table[PlayerX - x][PlayerY - y].BaseValue == Field.Gear ||
                        _table[PlayerX - x][PlayerY - y].BaseValue == Field.Pipe)
                        _table[PlayerX - x][PlayerY - y].BaseValue = Field.FreeTile;

                    CanStep = false;
                    _stepTimer.Interval = steppedInTrap ? 2000 : _table[PlayerX][PlayerY].DelayUntilNextMove;
                    _stepTimer.Start();
                }

                OnGameAdvanced();
            }
        }

        /// <summary>
        /// Ha van a játékos mellett egy gyár, akkor lekéri annak az adatait
        /// és esemény formájában tovább küldi azokat a nézetmodellnek
        /// </summary>
        public void GetDataOfFactory()
        {
            PauseAndContinueGame();

            Tuple<Int32, Int32> factoryCoord = LookForFactory();

            if (factoryCoord.Item1 == -1 && factoryCoord.Item2 == -1)
                return;

            if (!_factories.ContainsKey(factoryCoord))
                return;

            Factory fact = _factories[new Tuple<Int32, Int32>(factoryCoord.Item1, factoryCoord.Item2)];

            OnFactoryInformation(new Tuple<Boolean, Boolean, Boolean, Boolean>(fact.HasBulb, fact.HasFoil, fact.HasGear, fact.HasPipe));
        }

        /// <summary>
        /// A SPACE billentyű lenyomásának lekezelése
        /// Ha még nincsenek aktiválva a kijáratok, akkor a játékos
        /// leadja a nála lévő alkatrészeket a mellette lévő gyárban
        /// Ha már aktívak a kijáratok, akkor a játékos kinyitja a mellette lévő
        /// kijáratot és kijut a labirintusból feltéve, hogy nála van a megfelelő kulcs
        /// </summary>
        public void HandleSpaceCommand()
        {
            if (!ExitGatesArePowered)
                HandleFactory();
            else
                OpenExit();
        }

        /// <summary>
        /// Játék állapotának alaphelyzetbe állítása
        /// </summary>
        public void DisposeGame()
        {
            CanStep = false;
            GameIsRunning = false;
            EnemyTimerIsRunning = false;
            PlayerIsHidden = false;
            ExitGatesArePowered = false;

            if (_stepTimer != null)
            {
                _stepTimer.Stop();
                _stepTimer.Elapsed -= StepTimer_Elapsed;
                _stepTimer.Dispose();
            }
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Elapsed -= GameTimer_Elapsed;
                _gameTimer.Dispose();
            }
            if (_endGameTimer != null)
            {
                _endGameTimer.Stop();
                _endGameTimer.Elapsed -= EndGameTimer_Elapsed;
                _endGameTimer.Dispose();
            }
            if (_enemyStepTimer != null)
            {
                _enemyStepTimer.Stop();
                _enemyStepTimer.Elapsed -= EnemyStepTimer_Elapsed;
                _enemyStepTimer.Dispose();
            }

            if (_scraps != null)
            {
                foreach (Tuple<Int32, Int32> key in _scraps.Keys)
                {
                    _scraps[key].DisposeTimer();
                }

                _scraps.Clear();
            }

            GameTime = 0;
            TimeLeft = -1;

            if (_table != null)
                _table.Clear();
            if (_sight != null)
                _sight.Clear();
            if (_factories != null)
                _factories.Clear();

            _enemyPreviousField = null;
            if (_enemyMap != null)
                _enemyMap.Clear();
            if (_enemyPath != null)
                _enemyPath.Clear();
            if (_enemyPathQueue != null)
                _enemyPathQueue.Clear();
            if (_factoriesExploredByEnemy != null)
                _factoriesExploredByEnemy.Clear();

            if (_gates != null)
                _gates.Clear();

            if (_initMapData != null)
                _initMapData.Clear();
        }

        /// <summary>
        /// A pályák neveinek kilistázása
        /// </summary>
        /// <returns>A pályák neveinek listája</returns>
        /// <exception cref="InvalidOperationException">Ha az adatelérés nem lett inicializálva,
        /// kivételt dobunk</exception>
        public ICollection<SaveEntry> ListMaps()
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("Nincs adatelérés.");

            return _dataAccess.List();
        }

        /// <summary>
        /// Eredmény hozzáadása a toplistához
        /// </summary>
        /// <param name="map">Pálya név</param>
        /// <param name="playerName">Játékos név</param>
        /// <param name="result">Eredmény</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddResult(String playerName, Int32 result)
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("Nincs adatelérés.");

            _dataAccess.AddResult(CurrentMap, playerName, result);
        }

        /// <summary>
        /// Toplista kimentése fájlba
        /// </summary>
        public void SaveToplist()
        {
            if (_dataAccess != null)
                _dataAccess.SaveToplist();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Pálya inicializálása
        /// </summary>
        /// <param name="mapName">A pálya neve</param>
        private void InitializeMap(String mapName)
        {
            _table = new List<List<Land>>();
            _scraps = new Dictionary<Tuple<Int32, Int32>, Scrap>();
            _factories = new Dictionary<Tuple<Int32, Int32>, Factory>();
            _factoriesExploredByEnemy = new List<Tuple<Int32, Int32>>();

            for (Int32 i = 0; i < _initMapData.TableSize; ++i)
            {
                _table.Add(new List<Land>());
                for (Int32 j = 0; j < _initMapData.TableSize; ++j)
                    _table[i].Add(new Land(Field.FreeTile, false));
            }

            //PrimMaze(10, 10, 0, TableSize - 1, 0, TableSize - 1);
            KruskalMaze(0, TableSize - 1, 0, TableSize - 1);
            CreateRooms();
            List<Tuple<Int32, Int32>> wallList = PickWallsWithTwoFreeNeighbours();
            PutDownHealsAndScraps();
            PutDownObstacles();
            PutDownFactories(wallList);
            PutDownExitGates();

            PlayerX = PlayerY = 1;

            _table[PlayerX][PlayerY].BaseValue = Field.FreeTile;
            _table[PlayerX][PlayerY].CurrentValue = Field.Player;

            _sight = new List<List<Field>>();
            for (Int32 i = 0; i < SightTableSize; ++i)
            {
                _sight.Add(new List<Field>());
                for (Int32 j = 0; j < SightTableSize; ++j)
                    _sight[i].Add(Field.FreeTile);
            }         
        }

        private void InitializeEnemy()
        {
            _enemyPath = new List<Tuple<Int32, Int32>>();
            _enemyPathQueue = new Queue<Tuple<Int32, Int32>>();

            _enemyMap = new List<List<Field>>();

            for (Int32 i = 0; i < _initMapData.TableSize; i++)
            {
                _enemyMap.Add(new List<Field>());
                for (Int32 j = 0; j < _initMapData.TableSize; j++)
                {
                    _enemyMap[i].Add(Field.Unexplored);
                }
            }

            List<Tuple<Int32, Int32>> freeTiles = new List<Tuple<Int32, Int32>>();

            for (Int32 i = TableSize / 2 - 2; i < TableSize / 2 + 2; i++)
            {
                for (Int32 j = TableSize / 2 - 2; j < TableSize / 2 + 2; j++)
                {
                    if (_table[i][j].BaseValue == Field.FreeTile)
                        freeTiles.Add(new Tuple<Int32, Int32>(i, j));
                }
            }

            Int32 index = _random.Next(freeTiles.Count);
            EnemyX = freeTiles[index].Item1;
            EnemyY = freeTiles[index].Item2;
            _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = Field.Enemy;

            _enemyPreviousField = new Tuple<Int32, Int32>(EnemyX, EnemyY);

            _enemyStepTimer = new Timer
            {
                Interval = 500,
                AutoReset = true,
                Enabled = false
            };
            _enemyStepTimer.Elapsed += new ElapsedEventHandler(EnemyStepTimer_Elapsed);
        }

        /// <summary>
        /// Alkatrészek leadása egy gyárban
        /// </summary>
        /// <param name="factoryCoord">A gyár</param>
        private void TransmitScraps(Tuple<Int32, Int32> factoryCoord)
        {
            if (HasBulb)
            {
                if (_factories[factoryCoord].AddScrap(Field.Bulb))
                {
                    HasBulb = false;
                }
            }

            if (HasFoil)
            {
                if (_factories[factoryCoord].AddScrap(Field.Foil))
                {
                    HasFoil = false;
                }
            }

            if (HasGear)
            {
                if (_factories[factoryCoord].AddScrap(Field.Gear))
                {
                    HasGear = false;
                }
            }

            if (HasPipe)
            {
                if (_factories[factoryCoord].AddScrap(Field.Pipe))
                {
                    HasPipe = false;
                }
            }
        }

        /// <summary>
        /// A játékvégi összeomlás inicializálása
        /// </summary>
        private void InitializeEndGameCollapse()
        {
            foreach (Scrap scrap in _scraps.Values)
            {
                scrap.DisposeTimer();

                _table[scrap.X][scrap.Y] = new Land(Field.FreeTile, false);

                if (PlayerX == scrap.X && PlayerY == scrap.Y)
                    _table[scrap.X][scrap.Y].CurrentValue = Field.Player;
            }

            List<Tuple<Int32, Int32>> coords = new List<Tuple<Int32, Int32>>();
            foreach (Tuple<Int32, Int32> tuple in _scraps.Keys)
            {
                coords.Add(tuple);
            }

            _scraps.Clear();

            List<Field> keys = new List<Field>
            {
                Field.Key1,
                Field.Key2,
                Field.Key3,
                Field.Key4
            };

            foreach (Field key in keys)
            {
                try
                {
                    Int32 index = _random.Next(coords.Count);

                    if (_table[coords[index].Item1][coords[index].Item2].CurrentValue != Field.Player)
                    {
                        _table[coords[index].Item1][coords[index].Item2].CurrentValue = key;
                    }
                    else
                    {
                        if (Key == null)
                            Key = key;
                        else
                            _table[coords[index].Item1][coords[index].Item2].BaseValue = key;
                    }

                    coords.RemoveAt(index);
                }
                catch (Exception ex) { OnGameError(ex.Message); }
            }

            for (Int32 i = 0; i < TableSize; i++)
            {
                for (Int32 j = 0; j < TableSize; j++)
                {
                    _enemyMap[i][j] = _table[i][j].CurrentValue;
                    if (PlayerX == i && PlayerY == j)
                        _enemyMap[i][j] = _table[i][j].BaseValue;
                }
            }

            ExitGatesArePowered = true;

            TimeLeft = MaxValueOfTimer;

            _endGameTimer = new Timer
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };
            _endGameTimer.Elapsed += new ElapsedEventHandler(EndGameTimer_Elapsed);
            _endGameTimer.Start();
        }

        private Boolean ChangeFields(Int32 x, Int32 y, Field playerField, Boolean isPlayerHidden)
        {
            _table[PlayerX][PlayerY].CurrentValue = _table[PlayerX][PlayerY].BaseValue;

            PlayerX += x;
            PlayerY += y;

            _table[PlayerX][PlayerY].CurrentValue = playerField;
            PlayerIsHidden = isPlayerHidden;
            return true;
        }

        /// <summary>
        /// Annak kezelése, amikor a játékos megkísérel kinyitni egy kijáratot
        /// </summary>
        private void OpenExit()
        {
            if (!ExitGatesArePowered)
                return;

            List<Tuple<Int32, Int32>> _lands = new List<Tuple<Int32, Int32>>();
            _lands.Add(new Tuple<Int32, Int32>(PlayerX, PlayerY - 1));
            _lands.Add(new Tuple<Int32, Int32>(PlayerX + 1, PlayerY));
            _lands.Add(new Tuple<Int32, Int32>(PlayerX, PlayerY + 1));
            _lands.Add(new Tuple<Int32, Int32>(PlayerX - 1, PlayerY));

            Boolean exitFound = false;
            Tuple<Int32, Int32> exitCoord = new Tuple<Int32, Int32>(0, 0);

            foreach (Tuple<Int32, Int32> tuple in _lands)
            {
                if (tuple.Item1 < 0 || tuple.Item1 >= TableSize || tuple.Item2 < 0 || tuple.Item2 >= TableSize)
                    continue;

                if (_table[tuple.Item1][tuple.Item2].CurrentValue == Field.ExitGate1 ||
                    _table[tuple.Item1][tuple.Item2].CurrentValue == Field.ExitGate2 ||
                    _table[tuple.Item1][tuple.Item2].CurrentValue == Field.ExitGate3 ||
                    _table[tuple.Item1][tuple.Item2].CurrentValue == Field.ExitGate4)
                {
                    exitFound = true;
                    exitCoord = new Tuple<Int32, Int32>(tuple.Item1, tuple.Item2);
                }
            }

            if (!exitFound)
                return;

            Boolean gameIsWon = false;

            switch (_table[exitCoord.Item1][exitCoord.Item2].CurrentValue)
            {
                case Field.ExitGate1:
                    if (Key == Field.Key1)
                        gameIsWon = true;
                    break;
                case Field.ExitGate2:
                    if (Key == Field.Key2)
                        gameIsWon = true;
                    break;
                case Field.ExitGate3:
                    if (Key == Field.Key3)
                        gameIsWon = true;
                    break;
                case Field.ExitGate4:
                    if (Key == Field.Key4)
                        gameIsWon = true;
                    break;
            }

            if (gameIsWon)
            {
                Int32 gameTime = GameTime;
                OnGameAdvanced();
                DisposeGame();
                OnGameOver(new Tuple<Boolean, Int32>(true, gameTime));
            }
        }

        /// <summary>
        /// Azon gyár kezelése, amelyben a játékos megkísérli leadni a nála található alkatrészeket
        /// </summary>
        private void HandleFactory()
        {
            if (!HasBulb && !HasFoil && !HasGear && !HasPipe)
                return;

            Tuple<Int32, Int32> factoryCoord = LookForFactory();

            if (factoryCoord.Item1 == -1 && factoryCoord.Item2 == -1)
                return;

            TransmitScraps(factoryCoord);

            if (_factories[factoryCoord].IsClosed)
            {
                Boolean exitGatesArePowered = true;

                _factoriesExploredByEnemy.Remove(factoryCoord);

                _table[factoryCoord.Item1][factoryCoord.Item2].CurrentValue = Field.ClosedFactory;
                _enemyMap[factoryCoord.Item1][factoryCoord.Item2] = Field.ClosedFactory;

                foreach (Factory factory in _factories.Values)
                {
                    exitGatesArePowered &= factory.IsClosed;
                }

                if (exitGatesArePowered)
                    InitializeEndGameCollapse();
            }

            OnGameAdvanced();
        }

        /// <summary>
        /// Keres egy gyárat közvetlenül a játékos mellett
        /// </summary>
        /// <returns>Ha talál egy gyárat a játékos mellett, akkor visszaadja
        /// annak a koordinátáját, különben (-1, -1)-et ad vissza</returns>
        private Tuple<Int32, Int32> LookForFactory()
        {
            if (ExitGatesArePowered)
                return new Tuple<Int32, Int32>(-1, -1);

            List<Tuple<Int32, Int32>> _lands = new List<Tuple<Int32, Int32>>
            {
                new Tuple<Int32, Int32>(PlayerX, PlayerY - 1),
                new Tuple<Int32, Int32>(PlayerX + 1, PlayerY),
                new Tuple<Int32, Int32>(PlayerX, PlayerY + 1),
                new Tuple<Int32, Int32>(PlayerX - 1, PlayerY)
            };

            Tuple<Int32, Int32> factoryCoord = new Tuple<Int32, Int32>(-1, -1);

            foreach (Tuple<Int32, Int32> tuple in _lands)
            {
                if (tuple.Item1 < 0 || tuple.Item1 >= TableSize || tuple.Item2 < 0 || tuple.Item2 >= TableSize)
                    continue;

                if (_table[tuple.Item1][tuple.Item2].CurrentValue == Field.OpenFactory && _factories.ContainsKey(tuple))
                {
                    factoryCoord = new Tuple<Int32, Int32>(tuple.Item1, tuple.Item2);
                }
            }

            return factoryCoord;
        }

        #endregion

        #region Map generation

        /// <summary>
        /// Labirintus generálása Prim algoritmussal
        /// </summary>
        /// <param name="x">A kezdő mező x koordinátája</param>
        /// <param name="y">A kezdő mező y koordinátája</param>
        /// <param name="a">A labirintus kiterjedését szabályozó bal felső pont x koordinátája</param>
        /// <param name="b">A labirintus kiterjedését szabályozó bal felső pont y koordinátája</param>
        /// <param name="c">A labirintus kiterjedését szabályozó jobb alsó pont x koordinátája</param>
        /// <param name="d">A labirintus kiterjedését szabályozó jobb alsó pont y koordinátája</param>
        private void PrimMaze(Int32 x, Int32 y, Int32 a, Int32 b, Int32 c, Int32 d)
        {
            if (x < a || x > b || y < c || y > d)
            {
                x = _random.Next(b);
                y = _random.Next(d);
            }

            List<List<Boolean>> processed = new List<List<Boolean>>();

            for (Int32 i = a; i <= b; i++)
            {
                processed.Add(new List<Boolean>());
                for (Int32 j = c; j <= d; j++)
                {
                    _table[i][j].ChangeLand(Field.Wall, true);
                    processed[i].Add(false);
                }
            }

            _table[x][y].ChangeLand(Field.FreeTile, false);

            List<Tuple<Int32, Int32>> walls = new List<Tuple<Int32, Int32>>();

            if (x > a)
            {
                walls.Add(new Tuple<Int32, Int32>(x - 1, y));
            }
            if (x < b)
            {
                walls.Add(new Tuple<Int32, Int32>(x + 1, y));
            }
            if (y > c)
            {
                walls.Add(new Tuple<Int32, Int32>(x, y - 1));
            }
            if (y < d)
            {
                walls.Add(new Tuple<Int32, Int32>(x, y + 1));
            }

            while (walls.Count > 0)
            {
                Tuple<Int32, Int32> cell = walls[_random.Next(walls.Count)];

                List<Tuple<Int32, Int32>> neighbourWalls = GetNeighbours(cell, a, b, c, d, Field.Wall, true);

                if (neighbourWalls.Count >= 3)
                {
                    _table[cell.Item1][cell.Item2].ChangeLand(Field.FreeTile, false);

                    foreach (Tuple<Int32, Int32> neighbour in neighbourWalls)
                    {
                        if (!walls.Contains(neighbour) && !processed[neighbour.Item1][neighbour.Item2])
                            walls.Add(neighbour);
                    }
                }

                processed[cell.Item1][cell.Item2] = true;

                walls.Remove(cell);
            }
        }

        /// <summary>
        /// Labirintus generálása Kruskal algoritmussal
        /// </summary>
        /// <param name="a">A labirintus kiterjedését szabályozó bal felső pont x koordinátája</param>
        /// <param name="b">A labirintus kiterjedését szabályozó bal felső pont y koordinátája</param>
        /// <param name="c">A labirintus kiterjedését szabályozó jobb alsó pont x koordinátája</param>
        /// <param name="d">A labirintus kiterjedését szabályozó jobb alsó pont y koordinátája</param>
        private void KruskalMaze(Int32 a, Int32 b, Int32 c, Int32 d)
        {
            if (a < 0 || b >= TableSize || c < 0 || d >= TableSize)
                return;

            List<Tuple<Int32, Int32>> walls = new List<Tuple<Int32, Int32>>();

            List<HashSet<Tuple<Int32, Int32>>> freeTiles = new List<HashSet<Tuple<Int32, Int32>>>();

            for (Int32 i = a; i <= b; i++)
            {
                for (Int32 j = c; j <= d; j++)
                {
                    if (i % 2 == 1 && j % 2 == 1)
                    {
                        freeTiles.Add(new HashSet<Tuple<Int32, Int32>> { Tuple.Create(i, j) });
                        _table[i][j].ChangeLand(Field.FreeTile, false);
                    }
                    else
                    {
                        walls.Add(Tuple.Create(i, j));
                        _table[i][j].ChangeLand(Field.Wall, true);
                    }
                }
            }

            while (walls.Count > 0)
            {
                Tuple<Int32, Int32> cell = walls[_random.Next(walls.Count)];

                List<Tuple<Int32, Int32>> neighbours = GetNeighbours(cell, a, b, c, d, Field.FreeTile, true);

                if (neighbours.Count >= 2)
                {
                    List<Int32> index = new List<Int32>(neighbours.Count);

                    for (Int32 i = 0; i < neighbours.Count; i++)
                        index.Add(0);

                    for (Int32 i = 0; i < neighbours.Count; i++)
                    {
                        for (Int32 j = 0; j < freeTiles.Count; j++)
                        {
                            if (freeTiles[j].Contains(neighbours[i]))
                            {
                                index[i] = j;
                            }
                        }
                    }

                    List<Int32> delIndex = new List<Int32>();
                    Boolean isCoherent = true;

                    for (Int32 i = 1; i < index.Count; i++)
                    {
                        if (index[0] != index[i])
                        {
                            isCoherent = false;
                            foreach (var tuple in freeTiles[index[i]])
                            {
                                freeTiles[index[0]].Add(tuple);
                            }
                            delIndex.Add(index[i]);
                        }
                    }

                    for (Int32 i = 0; i < delIndex.Count; i++)
                    {
                        freeTiles[delIndex[i]].Clear();
                    }

                    if (!isCoherent)
                    {
                        freeTiles[index[0]].Add(cell);
                        _table[cell.Item1][cell.Item2].ChangeLand(Field.FreeTile, false);
                    }
                }

                walls.Remove(cell);
            }
        }

        /// <summary>
        /// Összegyűjti egy listába egy adott mező négy, a field változó értékével megegyező szomszédait
        /// </summary>
        /// <param name="cell">A mező</param>
        /// <param name="a">A labirintus kiterjedését szabályozó bal felső pont x koordinátája</param>
        /// <param name="b">A labirintus kiterjedését szabályozó bal felső pont y koordinátája</param>
        /// <param name="c">A labirintus kiterjedését szabályozó jobb alsó pont x koordinátája</param>
        /// <param name="d">A labirintus kiterjedését szabályozó jobb alsó pont y koordinátája</param>
        /// <param name="field">A mező keresett értéke</param>
        /// <returns>A mező négy, a field változó értékével megegyező szomszédainak listája</returns>
        private List<Tuple<Int32, Int32>> GetNeighbours(Tuple<Int32, Int32> cell, Int32 a, Int32 b, Int32 c, Int32 d, Field field, Boolean equality)
        {
            List<Tuple<Int32, Int32>> neighbours = new List<Tuple<Int32, Int32>>();

            if (equality)
            {
                if (cell.Item1 > a && _table[cell.Item1 - 1][cell.Item2].BaseValue == field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1 - 1, cell.Item2));
                }
                if (cell.Item1 < b && _table[cell.Item1 + 1][cell.Item2].BaseValue == field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1 + 1, cell.Item2));
                }
                if (cell.Item2 > c && _table[cell.Item1][cell.Item2 - 1].BaseValue == field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1, cell.Item2 - 1));
                }
                if (cell.Item2 < d && _table[cell.Item1][cell.Item2 + 1].BaseValue == field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1, cell.Item2 + 1));
                }
            }
            else
            {
                if (cell.Item1 > a && _table[cell.Item1 - 1][cell.Item2].BaseValue != field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1 - 1, cell.Item2));
                }
                if (cell.Item1 < b && _table[cell.Item1 + 1][cell.Item2].BaseValue != field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1 + 1, cell.Item2));
                }
                if (cell.Item2 > c && _table[cell.Item1][cell.Item2 - 1].BaseValue != field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1, cell.Item2 - 1));
                }
                if (cell.Item2 < d && _table[cell.Item1][cell.Item2 + 1].BaseValue != field)
                {
                    neighbours.Add(new Tuple<Int32, Int32>(cell.Item1, cell.Item2 + 1));
                }
            }

            return neighbours;
        }

        /// <summary>
        /// Szoba létrehozása
        /// </summary>
        private void CreateRooms()
        {
            List<Tuple<Int32, Int32>> coords = _initMapData.Rooms;

            for (Int32 i = 0; i < coords.Count; i++)
            {
                if (coords[i].Item1 < 0 || coords[i].Item1 >= TableSize || coords[i].Item2 < 0 || coords[i].Item2 >= TableSize)
                    continue;

                Int32 x = coords[i].Item1;
                Int32 y = coords[i].Item2;

                for (Int32 j = x; j < x + 5; j++)
                {
                    for (Int32 k = y; k < y + 5; k++)
                    {
                        _table[j][k].ChangeLand(Field.FreeTile, false);
                    }
                }

                switch (_random.Next(4))
                {
                    case 0:
                        _table[x][y].ChangeLand(Field.Bush, false);
                        _table[x + 1][y].ChangeLand(Field.Bush, false);
                        _table[x][y + 1].ChangeLand(Field.Bush, false);
                        break;
                    case 1:
                        _table[x + 4][y].ChangeLand(Field.Bush, false);
                        _table[x + 3][y].ChangeLand(Field.Bush, false);
                        _table[x + 4][y + 1].ChangeLand(Field.Bush, false);
                        break;
                    case 2:
                        _table[x + 4][y + 4].ChangeLand(Field.Bush, false);
                        _table[x + 3][y + 4].ChangeLand(Field.Bush, false);
                        _table[x + 4][y + 3].ChangeLand(Field.Bush, false);
                        break;
                    case 3:
                        _table[x][y + 4].ChangeLand(Field.Bush, false);
                        _table[x][y + 3].ChangeLand(Field.Bush, false);
                        _table[x + 1][y + 4].ChangeLand(Field.Bush, false);
                        break;
                }

                Int32 rand = _random.Next(2);

                if (rand == 0)
                {
                    _table[x + 2][y + 1].ChangeLand(Field.Box, true);
                    _table[x + 3][y + 1].ChangeLand(Field.Box, true);
                    _table[x + 3][y + 2].ChangeLand(Field.Box, true);
                }
                else
                {
                    _table[x + 1][y + 2].ChangeLand(Field.Box, true);
                    _table[x + 1][y + 3].ChangeLand(Field.Box, true);
                    _table[x + 2][y + 3].ChangeLand(Field.Box, true);
                }

                if (_table[x - 1][y].BaseValue == Field.Wall && _table[x][y - 1].BaseValue == Field.Wall
                    && _table[x - 1][y - 1].BaseValue != Field.Wall)
                    _table[x][y].ChangeLand(Field.Wall, true);

                if (_table[x + 5][y].BaseValue == Field.Wall && _table[x + 4][y - 1].BaseValue == Field.Wall
                    && _table[x + 5][y - 1].BaseValue != Field.Wall)
                    _table[x + 4][y].ChangeLand(Field.Wall, true);

                if (_table[x + 5][y + 4].BaseValue == Field.Wall && _table[x + 4][y + 5].BaseValue == Field.Wall
                    && _table[x + 5][y + 5].BaseValue != Field.Wall)
                    _table[x + 4][y + 4].ChangeLand(Field.Wall, true);

                if (_table[x - 1][y + 4].BaseValue == Field.Wall && _table[x][y + 5].BaseValue == Field.Wall
                    && _table[x - 1][y + 5].BaseValue != Field.Wall)
                    _table[x][y + 4].ChangeLand(Field.Wall, true);
            }
        }

        /// <summary>
        /// Azon falak koordinátájának listába rendezése, amelyeknek a szomszédos mezőik közül
        /// pontosan kettő szabad és ezek balra illetve jobbra vagy felfele és lefele vannak az adott faltól
        /// </summary>
        /// <returns>A falak koordinátáinak listája</returns>
        private List<Tuple<Int32, Int32>> PickWallsWithTwoFreeNeighbours()
        {
            for (Int32 i = 1; i < TableSize - 1; i++)
            {
                _table[i][1].ChangeLand(Field.FreeTile, false);
                _table[1][i].ChangeLand(Field.FreeTile, false);
                _table[i][TableSize - 2].ChangeLand(Field.FreeTile, false);
                _table[TableSize - 2][i].ChangeLand(Field.FreeTile, false);
            }

            List<Tuple<Int32, Int32>> walls = new List<Tuple<Int32, Int32>>();
            Int32 count = _random.Next(8, 14);

            for (Int32 i = 0; i < TableSize; i++)
            {
                for (Int32 j = 0; j < TableSize; j++)
                {
                    if (_table[i][j].BaseValue != Field.Wall)
                        continue;

                    if (GetNeighbours(new Tuple<Int32, Int32>(i, j), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 2)
                    {
                        Boolean suitable = false;

                        if (i > 0 && _table[i - 1][j].BaseValue == Field.FreeTile
                            && i < TableSize - 1 && _table[i + 1][j].BaseValue == Field.FreeTile)
                        {
                            if (j - 1 > 0 && j + 1 < TableSize - 1 &&
                                GetNeighbours(new Tuple<Int32, Int32>(i, j - 1), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 2
                                && GetNeighbours(new Tuple<Int32, Int32>(i, j + 1), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 2)
                            {
                                suitable = true;
                            }
                        }
                        else if (j > 0 && _table[i][j - 1].BaseValue == Field.FreeTile
                            && j < TableSize - 1 && _table[i][j + 1].BaseValue == Field.FreeTile)
                        {
                            if (i - 1 > 0 && i + 1 < TableSize - 1 &&
                                GetNeighbours(new Tuple<Int32, Int32>(i - 1, j), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 2
                                && GetNeighbours(new Tuple<Int32, Int32>(i + 1, j), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 2)
                            {
                                suitable = true;
                            }
                        }

                        if (suitable)
                        {
                            --count;

                            if (count <= 0)
                            {
                                _table[i][j].ChangeLand(Field.FreeTile, false);
                                count = _random.Next(6, 14);
                            }
                            else
                            {
                                walls.Add(new Tuple<Int32, Int32>(i, j));
                            }
                        }
                    }
                }
            }

            List<Tuple<Int32, Int32, Int32, Int32>> lonelyWalls = new List<Tuple<Int32, Int32, Int32, Int32>>();

            for (Int32 i = 2; i < TableSize - 2; i++)
            {
                if (_table[i][2].BaseValue == Field.Wall) lonelyWalls.Add(new Tuple<Int32, Int32, Int32, Int32>(i, 2, i, 1));
                if (_table[i][TableSize - 3].BaseValue == Field.Wall) lonelyWalls.Add(new Tuple<Int32, Int32, Int32, Int32>(i, TableSize - 3, i, TableSize - 2));
                if (_table[i][2].BaseValue == Field.Wall) lonelyWalls.Add(new Tuple<Int32, Int32, Int32, Int32>(2, i, 1, i));
                if (_table[i][2].BaseValue == Field.Wall) lonelyWalls.Add(new Tuple<Int32, Int32, Int32, Int32>(TableSize - 3, i, TableSize - 2, i));
            }

            foreach (Tuple<Int32, Int32, Int32, Int32> coord in lonelyWalls)
            {
                if (GetNeighbours(new Tuple<Int32, Int32>(coord.Item1, coord.Item2), 0, TableSize - 1, 0, TableSize - 1, Field.FreeTile, true).Count == 4)
                {
                    _table[coord.Item3][coord.Item4].ChangeLand(Field.Wall, true);
                }
            }

            return walls;
        }

        private void PutDownObstacles()
        {
            List<Tuple<Int32, Int32>> freeTiles = new List<Tuple<Int32, Int32>>();

            for (Int32 i = 3; i < TableSize - 4; i++)
            {
                for (Int32 j = 3; j < TableSize - 4; j++)
                {
                    List<Tuple<Int32, Int32>> neighbours =
                    GetNeighbours(new Tuple<Int32, Int32>(i, j), 0, 0, TableSize - 1, TableSize - 1, Field.Wall, true);
                    if (_table[i][j].CurrentValue == Field.FreeTile && neighbours.Count <= 2)
                        freeTiles.Add(new Tuple<Int32, Int32>(i, j));
                }
            }

            for (Int32 i = 0; i < _initMapData.NumberOfBearTraps; i++)
                PutDownObstacle(ref freeTiles, Field.BearTrap, 1500);
            for (Int32 i = 0; i < _initMapData.NumberOfBushes; i++)
                PutDownObstacle(ref freeTiles, Field.Bush, 250);
            for (Int32 i = 0; i < _initMapData.NumberOfPuddles; i++)
                PutDownObstacle(ref freeTiles, Field.Puddle, 1800);
        }

        private void PutDownObstacle(ref List<Tuple<Int32, Int32>> freeTiles, Field obstacle, Int32 delay)
        {
            if (freeTiles.Count == 0)
                for (Int32 i = 1; i < TableSize - 1; ++i)
                {
                    for (Int32 j = 1; j < TableSize - 1; ++j)
                    {
                        List<Tuple<Int32, Int32>> neighbours =
                        GetNeighbours(new Tuple<Int32, Int32>(i, j), 0, 0, TableSize - 1, TableSize - 1, Field.Wall, true);
                        if (_table[i][j].CurrentValue == Field.FreeTile && neighbours.Count <= 2)
                            freeTiles.Add(Tuple.Create(i, j));
                    }
                }

            Tuple<Int32, Int32> coord = freeTiles[_random.Next(freeTiles.Count)];
            _table[coord.Item1][coord.Item2] = new Land(obstacle, false, delay);
            freeTiles.Remove(coord);

            freeTiles.Remove(new Tuple<Int32, Int32>(coord.Item1 + 1, coord.Item2));
            freeTiles.Remove(new Tuple<Int32, Int32>(coord.Item1 - 1, coord.Item2));
            freeTiles.Remove(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 + 1));
            freeTiles.Remove(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 - 1));
        }

        private void PutDownFactories(List<Tuple<Int32, Int32>> wallList)
        {
            for (Int32 i = 0; i < _initMapData.NumberOfFactories; i++)
            {
                Tuple<Int32, Int32> coord = wallList[_random.Next(wallList.Count)];
                wallList.Remove(coord);

                CleanWallList(ref wallList, coord);

                Tuple<Int32, Int32> factory = new Tuple<Int32, Int32>(coord.Item1, coord.Item2);
                _factories.Add(factory, new Factory(coord.Item1, coord.Item2));
            }

            foreach (var factory in _factories)
            {
                try
                {
                    _table[factory.Key.Item1][factory.Key.Item2].ChangeLand(Field.OpenFactory, true);
                }
                catch
                {
                    _factories.Remove(factory.Key);
                    continue;
                }
            }
        }

        private void CleanWallList(ref List<Tuple<Int32, Int32>> wallList, Tuple<Int32, Int32> coord)
        {
            if (wallList == null || coord == null)
                return;

            for (Int32 i = -2; i < 3; ++i)
            {
                for (Int32 j = -2; j < 3; ++j)
                {
                    if (i == 0 && j == 0)
                        continue;

                    Tuple<Int32, Int32> tuple = new Tuple<Int32, Int32>(coord.Item1 + i, coord.Item2 + j);
                    if (wallList.Contains(tuple))
                        wallList.Remove(tuple);
                }
            }

            if (wallList.Contains(new Tuple<Int32, Int32>(coord.Item1 - 2, coord.Item2)))
                wallList.Remove(new Tuple<Int32, Int32>(coord.Item1 - 2, coord.Item2));

            if (wallList.Contains(new Tuple<Int32, Int32>(coord.Item1 + 2, coord.Item2)))
                wallList.Remove(new Tuple<Int32, Int32>(coord.Item1 + 2, coord.Item2));

            if (wallList.Contains(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 - 2)))
                wallList.Remove(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 - 2));

            if (wallList.Contains(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 + 2)))
                wallList.Remove(new Tuple<Int32, Int32>(coord.Item1, coord.Item2 + 2));
        }

        private void PutDownExitGates()
        {
            List<Tuple<Int32, Int32>> listOfFreeWalls = new List<Tuple<Int32, Int32>>();
            for (Int32 i = 4; i < TableSize - 4; i++)
            {
                if (i % 5 != 1)
                    continue;

                if (_table[1][i].BaseValue == Field.FreeTile)
                    listOfFreeWalls.Add(new Tuple<Int32, Int32>(0, i));

                if (_table[i][1].BaseValue == Field.FreeTile)
                    listOfFreeWalls.Add(new Tuple<Int32, Int32>(i, 0));

                if (_table[TableSize - 2][i].BaseValue == Field.FreeTile)
                    listOfFreeWalls.Add(new Tuple<Int32, Int32>(TableSize - 1, i));

                if (_table[i][TableSize - 2].BaseValue == Field.FreeTile)
                    listOfFreeWalls.Add(new Tuple<Int32, Int32>(i, TableSize - 1));
            }

            Tuple<Int32, Int32> tuple = listOfFreeWalls[_random.Next(listOfFreeWalls.Count)];
            _table[tuple.Item1][tuple.Item2].ChangeLand(Field.ExitGate1, true);
            listOfFreeWalls.Remove(tuple);

            tuple = listOfFreeWalls[_random.Next(listOfFreeWalls.Count)];
            _table[tuple.Item1][tuple.Item2].ChangeLand(Field.ExitGate2, true);
            listOfFreeWalls.Remove(tuple);

            tuple = listOfFreeWalls[_random.Next(listOfFreeWalls.Count)];
            _table[tuple.Item1][tuple.Item2].ChangeLand(Field.ExitGate3, true);
            listOfFreeWalls.Remove(tuple);

            tuple = listOfFreeWalls[_random.Next(listOfFreeWalls.Count)];
            _table[tuple.Item1][tuple.Item2].ChangeLand(Field.ExitGate4, true);
            listOfFreeWalls.Remove(tuple);
        }

        public void PutDownHealsAndScraps()
        {
            for (Int32 i = 0; i < _initMapData.Rooms.Count; i++)
            {
                _table[_initMapData.Rooms[i].Item1 + 2][_initMapData.Rooms[i].Item2 + 2].CurrentValue = Field.Heal;
                //if (_initMapData.Scraps.Count == 0)
                //    continue;

                //Field field = _initMapData.Scraps[_random.Next(_initMapData.Scraps.Count)];
                //_initMapData.Scraps.Remove(field);

                //Tuple<Int32, Int32> scrapTuple = new Tuple<Int32, Int32>(_initMapData.Rooms[i].Item1 + 2, _initMapData.Rooms[i].Item2 + 2);
                //_scraps.Add(scrapTuple, new Scrap(scrapTuple.Item1, scrapTuple.Item2, field));
                //_scraps[scrapTuple].Respawn += new EventHandler<ScrapEventargs>(Scrap_Respawn);
                //_table[scrapTuple.Item1][scrapTuple.Item2] = new Land(Field.FreeTile, field, false, 1500);
            }

            List<Tuple<Int32, Int32>> coords = new List<Tuple<Int32, Int32>>();

            for (Int32 i = 0; i < TableSize - 1; i++)
            {
                for (Int32 j = 0; j < TableSize - 1; j++)
                {
                    if (_table[i][j].BaseValue == Field.FreeTile &&
                        GetNeighbours(new Tuple<Int32, Int32>(i, j), 0, TableSize - 1, 0, TableSize - 1, Field.Wall, true).Count == 3)
                    {
                        coords.Add(new Tuple<Int32, Int32>(i, j));
                    }
                }
            }

            if (coords.Contains(new Tuple<Int32, Int32>(1, 1)))
                coords.Remove(new Tuple<Int32, Int32>(1, 1));

            for (Int32 i = 0; i < _initMapData.Scraps.Count; i++)
            {
                Tuple<Int32, Int32> cell = coords[_random.Next(coords.Count)];
                coords.Remove(cell);

                Tuple<Int32, Int32> scrapTuple = new Tuple<Int32, Int32>(cell.Item1, cell.Item2);
                _scraps.Add(scrapTuple, new Scrap(cell.Item1, cell.Item2, _initMapData.Scraps[i]));
                _scraps[scrapTuple].Respawn += new EventHandler<ScrapEventargs>(Scrap_Respawn);
                _table[cell.Item1][cell.Item2] = new Land(Field.FreeTile, _initMapData.Scraps[i], false, 700);
            }

            while (coords.Count > 0 && _initMapData.NumberOfHeals > 0)
            {
                Tuple<Int32, Int32> cell = coords[_random.Next(coords.Count)];
                coords.Remove(cell);

                _table[cell.Item1][cell.Item2].CurrentValue = Field.Heal;

                --_initMapData.NumberOfHeals;
            }
        }

        #endregion

        #region Sight calculation

        /// <summary>
        /// Annak kiszámolása, hogy mit lát a játékos.
        /// </summary>
        private void CalculateSight()
        {
            Int32 Xindex1 = PlayerX - SightTableSize / 2;
            Int32 Yindex1 = PlayerY - SightTableSize / 2;
            Int32 Xindex2 = PlayerX + SightTableSize / 2 + SightTableSize % 2;
            Int32 Yindex2 = PlayerY + SightTableSize / 2 + SightTableSize % 2; // + % 2 mert a / 2 lefele kerekít

            // az alábbi 4 if korrigálásra kell,
            // ha elérnénk a játéktábla szélét

            if (Xindex1 < 0)
            {
                Xindex2 += -1 * Xindex1;
                Xindex1 = 0;
            }

            if (Yindex1 < 0)
            {
                Yindex2 += -1 * Yindex1;
                Yindex1 = 0;
            }

            if (Xindex2 >= TableSize)
            {
                Xindex1 -= (Xindex2 - TableSize);
                Xindex2 = TableSize - 1;
            }

            if (Yindex2 >= TableSize)
            {
                Yindex1 -= (Yindex2 - TableSize);
                Yindex2 = TableSize - 1;
            }

            Int32 x = 0, y = 0;

            for (Int32 i = Xindex1; i < Xindex1 + SightTableSize; ++i)
            {
                y = 0;
                for (Int32 j = Yindex1; j < Yindex1 + SightTableSize; ++j)
                {
                    _sight[x][y] = _table[i][j].CurrentValue;

                    //if (_enemyPreviousField.Item1 == i && _enemyPreviousField.Item2 == j)
                    //    _sight[x][y] = Field.Prev;
                    //else
                    //    _sight[x][y] = _enemyMap[i][j];

                    //for (Int32 k = -1; k < 2; ++k)
                    //{
                    //    for (Int32 l = -1; l < 2; ++l)
                    //    {
                    //        _sight[PlayerX + k][PlayerY + l] = _table[PlayerX + k][PlayerY + l].CurrentValue;
                    //    }
                    //}

                    //if (_table[i][j].BaseValue == Field.OpenFactory && _enemyMap[i][j] == Field.Unexplored)
                    //    _sight[x][y] = Field.Prev;

                    //if (_enemyPath.Contains(new Tuple<Int32, Int32>(i, j)))
                    //    _sight[x][y] = Field.Path;

                    //if (_enemyMap[i][j] == Field.Enemy)
                    //    _sight[x][y] = Field.Enemy;
                    ++y;
                }
                ++x;
            }
        }
        
        #endregion

        #region Events

        public event EventHandler GameCreated;

        public event EventHandler GameAdvanced;

        public event EventHandler<Tuple<Boolean, Boolean, Boolean, Boolean>> FactoryInformation;

        public event EventHandler<Tuple<Boolean, Int32>> GameOver;

        public event EventHandler<String> GameError;

        #endregion

        #region Event Triggers

        private void OnGameCreated()
        {
            try
            {
                CalculateSight();
                GameCreated?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                OnGameError("Hibás nézet frissítés a játék létrehozása közben.");
            }
        }

        private void OnGameAdvanced()
        {
            try
            {
                CalculateSight();
                GameAdvanced?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                OnGameError("Hibás nézet frissítés a játék futása közben.");
            }
        }

        private void OnFactoryInformation(Tuple<Boolean, Boolean, Boolean, Boolean> data)
        {
            FactoryInformation?.Invoke(this, data);
        }

        private void OnGameOver(Tuple<Boolean, Int32> result)
        {
            GameOver?.Invoke(this, result);
        }

        private void OnGameError(String s)
        {
            GameError?.Invoke(this, s);
        }

        #endregion

        #region Event handlers

        private void Scrap_Respawn(object sender, ScrapEventargs e)
        {
            if (e.Pair.Item1 < 0 || e.Pair.Item2 < 0 || e.Pair.Item1 >= TableSize || e.Pair.Item2 >= TableSize)
                return;

            if (_table[e.Pair.Item1][e.Pair.Item2].CurrentValue == Field.Player)
            {
                Boolean pickedUp = false;

                switch (e.Type)
                {
                    case Field.Bulb:
                        if (!HasBulb)
                            pickedUp = true;
                        HasBulb = true;
                        break;
                    case Field.Foil:
                        if (!HasFoil)
                            pickedUp = true;
                        HasFoil = true;
                        break;
                    case Field.Gear:
                        if (!HasGear)
                            pickedUp = true;
                        HasGear = true;
                        break;
                    case Field.Pipe:
                        if (!HasPipe)
                            pickedUp = true;
                        HasPipe = true;
                        break;
                }

                if (pickedUp)
                    _scraps[new Tuple<int, int>(e.Pair.Item1, e.Pair.Item2)].StartTimer();
                else
                {
                    _table[e.Pair.Item1][e.Pair.Item2].BaseValue = e.Type;
                    _scraps[new Tuple<int, int>(e.Pair.Item1, e.Pair.Item2)].StopTimer();
                }
            }
            else if (_table[e.Pair.Item1][e.Pair.Item2].CurrentValue == Field.Enemy)
            {
                _table[e.Pair.Item1][e.Pair.Item2].BaseValue = e.Type;
                _scraps[new Tuple<int, int>(e.Pair.Item1, e.Pair.Item2)].StopTimer();
            }
            else
            {
                _scraps[new Tuple<int, int>(e.Pair.Item1, e.Pair.Item2)].StopTimer();
                _table[e.Pair.Item1][e.Pair.Item2].CurrentValue = e.Type;
            }

            OnGameAdvanced();
        }

        private void GameTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ++GameTime;

            OnGameAdvanced();
        }

        private void EndGameTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            --TimeLeft;

            OnGameAdvanced();
            if (TimeLeft == 0)
            {
                DisposeGame();
                OnGameOver(new Tuple<Boolean, Int32>(false, -1));
            }
        }

        private void StepTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _stepTimer.Stop();
            CanStep = true;
        }
                

        private void EnemyStepTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!EnemyTimerIsRunning || !GameIsRunning)
                return;

            List<Tuple<Int32, Int32>> directions = GetDirections();

            List<Tuple<Int32, Int32>> unexploredTiles = new List<Tuple<Int32, Int32>>();
            List<Tuple<Int32, Int32>> freeTiles = new List<Tuple<Int32, Int32>>();

            Tuple<Int32, Int32> previousDirection = new Tuple<Int32, Int32>(0, 0);

            Boolean gameOver = false;

            foreach (var dir in directions)
            {
                if (gameOver)
                    continue;

                Int32 x = EnemyX + dir.Item1, y = EnemyY + dir.Item2;

                if (x < 0 || y < 0 || x >= TableSize || y >= TableSize)
                    continue;

                Land land = _table[x][y];
                if (land.CurrentValue == Field.Player || land.CurrentValue == Field.PlayerInPuddle
                    || land.CurrentValue == Field.TrappedPlayer)
                {
                    gameOver = true;
                    continue;
                }

                if (land.CurrentValue == Field.OpenFactory && !_factoriesExploredByEnemy.Contains(new Tuple<Int32, Int32>(x, y)))
                    _factoriesExploredByEnemy.Add(new Tuple<Int32, Int32>(x, y));

                if (land.CurrentValue == Field.OpenFactory && !_enemyPathQueue.Contains(new Tuple<Int32, Int32>(x, y)))
                    _enemyPathQueue.Enqueue(new Tuple<Int32, Int32>(x, y));

                if (land.IsOccupied || land.CurrentValue == Field.Heal
                    || land.CurrentValue == Field.Bulb || land.CurrentValue == Field.Foil
                    || land.CurrentValue == Field.Gear || land.CurrentValue == Field.Pipe
                    || land.CurrentValue == Field.Key1 || land.CurrentValue == Field.Key2
                    || land.CurrentValue == Field.Key3 || land.CurrentValue == Field.Key4)
                {
                    _enemyMap[x][y] = land.CurrentValue;                    

                    continue;
                }

                if (x == _enemyPreviousField.Item1 && y == _enemyPreviousField.Item2)
                {
                    previousDirection = dir;
                }

                if (_enemyMap[x][y] == Field.Unexplored)
                {
                    unexploredTiles.Add(dir);
                }
                else
                {
                    freeTiles.Add(dir);
                }
            }

            if (gameOver)
            {
                DisposeGame();
                OnGameOver(new Tuple<Boolean, Int32>(false, -1));
                return;
            }

            Int32 numberOfOpenFactories = 0;

            foreach (var factory in _factories.Values)
            {
                if (!factory.IsClosed)
                    ++numberOfOpenFactories;
            }

            if (Math.Abs(PlayerX - EnemyX) <= 6 && Math.Abs(PlayerY - EnemyY) <= 6
                && _enemyMap[PlayerX][PlayerY] != Field.Unexplored && !PlayerIsHidden)
            {
                Tuple<Int32, Int32> tuple = Tuple.Create(PlayerX, PlayerY);

                if (_enemyPath == null || _enemyPath.Count <= 1 || _enemyPath.Count >= 1.5 * TableSize)
                {
                    _enemyPath = EnemyPathToPlayer(new List<Tuple<Int32, Int32>>());
                }

                if (!_enemyPath.Contains(tuple))
                {
                    _enemyPath.Reverse();
                    _enemyPath = EnemyPathToPlayer(new List<Tuple<Int32, Int32>>());
                }

                if (_enemyPath.Count == 1 && _enemyPath.Contains(tuple))
                {
                    DisposeGame();
                    OnGameOver(new Tuple<Boolean, Int32>(false, -1));
                    return;
                }
                else
                {
                    MoveEnemy(new List<Tuple<Int32, Int32>> { _enemyPath.Last() }, true);
                    _enemyPath.Remove(_enemyPath.Last());
                }
            }
            else if (ExitGatesArePowered && Key != null)
            {
                if (_enemyPath == null || _enemyPath.Count == 0)
                    _enemyPath = EndgamePathForEnemy();

                if (_enemyPath.Count > 0)
                {
                    MoveEnemy(new List<Tuple<Int32, Int32>> { _enemyPath.Last() }, true);
                    _enemyPath.Remove(_enemyPath.Last());
                }
            }
            else if (unexploredTiles.Count > 0)
            {
                _enemyPath.Clear();
                MoveEnemy(unexploredTiles, false);
            }
            // ha minden gyárról tudjuk, hogy hol van
            else if (_enemyPathQueue.Count >= numberOfOpenFactories && numberOfOpenFactories >= 2)
            {
                if (_enemyPath == null || _enemyPath.Count == 0)
                {
                    Boolean done = false;
                    Int32 k = 0;
                    while (!done)
                    {
                        if (_enemyPathQueue.Count <= 1)
                        {
                            done = true;
                            continue;
                        }

                        Tuple<Int32, Int32> top = _enemyPathQueue.Peek();

                        if (_enemyMap[top.Item1][top.Item2] != Field.OpenFactory)
                        {
                            _enemyPathQueue.Dequeue();
                        }
                        else if ((top.Item1 == EnemyX + 1 && top.Item2 == EnemyY)
                            || (top.Item1 == EnemyX - 1 && top.Item2 == EnemyY)
                            || (top.Item1 == EnemyX && top.Item2 == EnemyY + 1)
                            || (top.Item1 == EnemyX && top.Item2 == EnemyY - 1))
                        {
                            _enemyPathQueue.Enqueue(_enemyPathQueue.Dequeue());
                            if (++k >= _enemyPathQueue.Count)
                            {
                                done = true;
                                _enemyPathQueue.Clear();
                            }
                        }
                        else
                        {
                            done = true;
                        }
                    }

                    if (_enemyPathQueue.Count >= 2)
                    {
                        _enemyPath = PathBetweenEnemyAndFactory(_enemyPathQueue.Peek());
                        _enemyPathQueue.Enqueue(_enemyPathQueue.Dequeue());
                    }
                }

                if (_enemyPath == null || _enemyPath.Count == 0 || _enemyPathQueue.Count <= 1)
                    return;

                Tuple<Int32, Int32> tuple = new Tuple<Int32, Int32>(_enemyPath.Last().Item1, _enemyPath.Last().Item2);
                MoveEnemy(new List<Tuple<Int32, Int32>> { tuple }, true);
                _enemyPath.Remove(tuple);
            }
            else if (freeTiles.Count > 0)
            {
                _enemyPath.Clear();

                if (freeTiles.Count > 1 && freeTiles.Contains(previousDirection))
                    freeTiles.Remove(previousDirection);

                MoveEnemy(freeTiles, false);
            }
            else
            {
                _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = _table[EnemyX][EnemyY].BaseValue;
                
                List<Tuple<Int32, Int32>> coords = new List<Tuple<Int32, Int32>>();

                for (Int32 i = 1; i < TableSize - 1; i++)
                {
                    if (!_table[1][i].IsOccupied
                        && _table[1][i - 1].CurrentValue != Field.Player
                        && _table[1][i + 1].CurrentValue != Field.Player
                        && _table[2][i].CurrentValue != Field.Player)
                    {
                        coords.Add(new Tuple<Int32, Int32>(1, i));
                    }
                }
                
                MoveEnemy(coords, true);

                OnGameError("Az ellenség olyan mezőre került, ahonnan nem tud továbblépni, ezért áthelyeztük egy valid mezőre.");
            }
        }

        #endregion

        #region Enemy movement methods

        /// <summary>
        /// Kiszámolja a távolságot az ellenség és egy, a paraméterben megadott gyár között
        /// </summary>
        /// <param name="factory">A cél gyár koordinátái</param>
        /// <exception cref="InvalidOperationException">Ha nem gyár koordinátáit kaptuk paraméterül</exception>
        private List<Tuple<Int32, Int32>> PathBetweenEnemyAndFactory(Tuple<Int32, Int32> factory)
        {
            if (_enemyMap[factory.Item1][factory.Item2] != Field.OpenFactory)
            {
                OnGameError("Hiba történt az ellenség objektívek közötti mozgása közben.");
                return null;
            }

            List<Tuple<Int32, Int32>> directions = GetDirections();

            HashSet<Tuple<Int32, Int32>> deadEnds = new HashSet<Tuple<Int32, Int32>>();
            List<Tuple<Int32, Int32>> path = new List<Tuple<Int32, Int32>>();

            Boolean pathIsComplete = false;

            while (!pathIsComplete)
            {
                if (path.Count == 0)
                    path.Add(new Tuple<Int32, Int32>(EnemyX, EnemyY));

                Boolean destinationFound = false;
                List<Tuple<Int32, Int32>> options = GetOptions(directions, path, deadEnds, factory, ref destinationFound);

                if (destinationFound)
                {
                    pathIsComplete = true;
                    continue;
                }

                // töröljük az opciók közül azt a mezőt, ahonnan jöttünk
                if (path.Count >= 2)
                    options.Remove(path[path.Count - 2]);

                if (options.Count == 0)
                {
                    // zsákutca => visszalépés
                    deadEnds.Add(path.Last());
                    if (!path.Remove(path.Last()))
                        throw new InvalidOperationException();
                }
                else if (options.Count == 1)
                {
                    if (path.Contains(options[0]))
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // tovább lépés
                        path.Add(options[0]);
                    }
                }
                else
                {
                    List<Tuple<Int32, Int32>> indexes = new List<Tuple<Int32, Int32>>();

                    for (Int32 i = 0; i < options.Count; i++)
                    {
                        if (path.Contains(options[i]))
                            indexes.Add(options[i]);
                    }

                    for (Int32 i = 0; i < indexes.Count; i++)
                    {
                        options.Remove(indexes[i]);
                    }

                    if (options.Count == 0)
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // tovább lépés
                        path.Add(options[_random.Next(options.Count)]);
                    }
                }
            }

            path.Reverse();
            path.Remove(path.Last());

            return path;
        }

        private List<Tuple<Int32, Int32>> EnemyPathToPlayer(List<Tuple<Int32, Int32>> path)
        {
            Tuple<Int32, Int32> playerTuple = Tuple.Create(PlayerX, PlayerY);

            List<Tuple<Int32, Int32>> directions = GetDirections();

            HashSet<Tuple<Int32, Int32>> deadEnds = new HashSet<Tuple<Int32, Int32>>();

            Boolean pathIsComplete = false;

            while (!pathIsComplete)
            {
                if (path.Count == 0)
                    path.Add(new Tuple<Int32, Int32>(EnemyX, EnemyY));

                Boolean destinationFound = false;
                List<Tuple<Int32, Int32>> options = GetOptions(directions, path, deadEnds, playerTuple, ref destinationFound);

                if (destinationFound)
                {
                    path.Add(new Tuple<Int32, Int32>(PlayerX, PlayerY));
                    pathIsComplete = true;
                    continue;
                }

                // töröljük az opciók közül azt a mezőt, ahonnan jöttünk
                if (path.Count >= 2)
                    options.Remove(path[path.Count - 2]);

                if (options.Count == 0)
                {
                    // zsákutca => visszalépés
                    deadEnds.Add(path.Last());
                    if (!path.Remove(path.Last()))
                        throw new InvalidOperationException();
                }
                else if (options.Count == 1)
                {
                    if (path.Contains(options[0]))
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // tovább lépés
                        path.Add(options[0]);
                    }
                }
                else
                {
                    List<Tuple<Int32, Int32>> items = new List<Tuple<Int32, Int32>>();

                    for (Int32 i = 0; i < options.Count; i++)
                    {
                        if (path.Contains(options[i]))
                            items.Add(options[i]);
                    }

                    for (Int32 i = 0; i < items.Count; i++)
                    {
                        options.Remove(items[i]);
                    }

                    if (options.Count == 0)
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // heurisztika, ami a célhoz való közelebb lépést segíti
                        List<Int32> differences = new List<Int32>();
                        Int32 minIndex = 0;

                        if (options.Count > 1)
                        {
                            for (Int32 i = 0; i < options.Count; i++)
                            {
                                differences.Add(Math.Abs(options[i].Item1 - playerTuple.Item1) + Math.Abs(options[i].Item2 - playerTuple.Item2));
                            }

                            for (Int32 i = 1; i < differences.Count; i++)
                            {
                                if (differences[i] < differences[minIndex])
                                    minIndex = i;
                            }
                        }

                        // tovább lépés
                        path.Add(options[minIndex]);
                    }
                }
            }

            path.Reverse();
            path.Remove(path.Last());

            return path;
        }

        /// <summary>
        /// Kiszámolja a távolságot az ellenség és aközött a kijárat között, amelyikhez tartozó
        /// kulcs van a játékosnál
        /// </summary>
        /// <returns>Az útvonallal tér vissza</returns>
        /// <exception cref="InvalidOperationException">Nem sikerült a visszalépés</exception>
        private List<Tuple<Int32, Int32>> EndgamePathForEnemy()
        {
            if (_gates == null || _gates.Count == 0)
            {
                _gates = new Dictionary<Field, Tuple<Int32, Int32>>();

                for (Int32 i = 0; i < TableSize; i++)
                {
                    for (Int32 j = 0; j < TableSize; j++)
                    {
                        switch (_table[i][j].BaseValue)
                        {
                            case Field.ExitGate1:
                                _gates.Add(Field.Key1, Tuple.Create(i, j));
                                break;
                            case Field.ExitGate2:
                                _gates.Add(Field.Key2, Tuple.Create(i, j));
                                break;
                            case Field.ExitGate3:
                                _gates.Add(Field.Key3, Tuple.Create(i, j));
                                break;
                            case Field.ExitGate4:
                                _gates.Add(Field.Key4, Tuple.Create(i, j));
                                break;
                        }
                    }
                }
            }

            Tuple<Int32, Int32> gateTuple = Tuple.Create(_gates[Key.Value].Item1, _gates[Key.Value].Item2);

            List<Tuple<Int32, Int32>> directions = GetDirections();

            HashSet<Tuple<Int32, Int32>> deadEnds = new HashSet<Tuple<Int32, Int32>>();
            List<Tuple<Int32, Int32>> path = new List<Tuple<Int32, Int32>>();

            Boolean pathIsComplete = false;

            while (!pathIsComplete)
            {
                if (path.Count == 0)
                    path.Add(new Tuple<Int32, Int32>(EnemyX, EnemyY));

                Boolean destinationFound = false;
                List<Tuple<Int32, Int32>> options = GetOptions(directions, path, deadEnds, gateTuple, ref destinationFound);

                if (destinationFound)
                {
                    pathIsComplete = true;
                    continue;
                }

                // töröljük az opciók közül azt a mezőt, ahonnan jöttünk
                if (path.Count >= 2)
                    options.Remove(path[path.Count - 2]);

                if (options.Count == 0)
                {
                    // zsákutca => visszalépés
                    deadEnds.Add(path.Last());
                    if (!path.Remove(path.Last()))
                        throw new InvalidOperationException();
                }
                else if (options.Count == 1)
                {
                    if (path.Contains(options[0]))
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // tovább lépés
                        path.Add(options[0]);
                    }
                }
                else
                {
                    List<Tuple<Int32, Int32>> items = new List<Tuple<Int32, Int32>>();

                    for (Int32 i = 0; i < options.Count; i++)
                    {
                        if (path.Contains(options[i]))
                            items.Add(options[i]);
                    }

                    for (Int32 i = 0; i < items.Count; i++)
                    {
                        options.Remove(items[i]);
                    }

                    if (options.Count == 0)
                    {
                        // kör => visszalépés
                        deadEnds.Add(path.Last());
                        if (!path.Remove(path.Last()))
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // heurisztika, ami a célhoz való közelebb lépést segíti
                        List<Int32> differences = new List<Int32>();
                        Int32 minIndex = 0;

                        if (options.Count > 1)
                        {
                            for (Int32 i = 0; i < options.Count; i++)
                            {
                                differences.Add(Math.Abs(options[i].Item1 - gateTuple.Item1) + Math.Abs(options[i].Item2 - gateTuple.Item2));
                            }

                            for (Int32 i = 1; i < differences.Count; i++)
                            {
                                if (differences[i] < differences[minIndex])
                                    minIndex = i;
                            }
                        }

                        // tovább lépés
                        path.Add(options[minIndex]);
                    }
                }
            }

            path.Reverse();
            path.Remove(path.Last());

            return path;
        }

        private void MoveEnemy(List<Tuple<Int32, Int32>> tiles, Boolean patrollModeOn)
        {
            _enemyPreviousField = new Tuple<Int32, Int32>(EnemyX, EnemyY);

            Int32 index = _random.Next(tiles.Count);
            _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = _table[EnemyX][EnemyY].BaseValue;
            
            if (_scraps.Keys.Contains(_enemyPreviousField))
            {
                _table[EnemyX][EnemyY].BaseValue = Field.FreeTile;
            }

            if (patrollModeOn)
            {
                EnemyX = tiles[index].Item1;
                EnemyY = tiles[index].Item2;
            }
            else
            {
                EnemyX += tiles[index].Item1;
                EnemyY += tiles[index].Item2;
            }

            if (EnemyX == PlayerX && EnemyY == PlayerY)
            {
                DisposeGame();
                OnGameOver(new Tuple<Boolean, Int32>(false, -1));
                return;
            }

            switch (_table[EnemyX][EnemyY].BaseValue)
            {
                case Field.Puddle:
                    _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = Field.EnemyInPuddle;
                    _enemyStepTimer.Interval = 1200;
                    break;
                case Field.BearTrap:
                    _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = Field.TrappedEnemy;
                    _enemyStepTimer.Interval = 2000;
                    break;
                case Field.Bush:
                    _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = Field.HiddenEnemy;
                    _enemyStepTimer.Interval = 500;
                    break;
                default:
                    _table[EnemyX][EnemyY].CurrentValue = _enemyMap[EnemyX][EnemyY] = Field.Enemy;
                    _enemyStepTimer.Interval = _initMapData.EnemySpeed;
                    break;
            }

            OnGameAdvanced();
        }

        #endregion

        #region Assistant methods

        private List<Tuple<Int32, Int32>> GetDirections()
        {
            return new List<Tuple<Int32, Int32>>
            {
                new Tuple<Int32, Int32>(1, 0),
                new Tuple<Int32, Int32>(-1, 0),
                new Tuple<Int32, Int32>(0, 1),
                new Tuple<Int32, Int32>(0, -1)
            };
        }

        private List<Tuple<Int32, Int32>> GetOptions(List<Tuple<Int32, Int32>> directions, List<Tuple<Int32, Int32>> path, HashSet<Tuple<Int32, Int32>> deadEnds, Tuple<Int32, Int32> destination, ref Boolean destinationFound, Boolean useEnemyMap = true)
        {
            List<Tuple<Int32, Int32>> options = new List<Tuple<Int32, Int32>>();

            foreach (var tuple in directions)
            {
                Int32 x = path.Last().Item1 + tuple.Item1, y = path.Last().Item2 + tuple.Item2;

                if (x < 0 || y < 0 || x >= _enemyMap.Count || y >= _enemyMap.Count)
                    continue;

                Field field;

                if (useEnemyMap)
                    field = _enemyMap[x][y];
                else                    
                    field = _table[x][y].CurrentValue;

                Tuple<Int32, Int32> coordinates = new Tuple<Int32, Int32>(x, y);

                if ((field == Field.FreeTile || field == Field.BearTrap
                    || field == Field.Bush || field == Field.Puddle)
                    && !deadEnds.Contains(coordinates))
                    options.Add(coordinates);

                if (x == destination.Item1 && y == destination.Item2)
                    destinationFound = true;
            }

            return options;
        }

        #endregion
    }
}
