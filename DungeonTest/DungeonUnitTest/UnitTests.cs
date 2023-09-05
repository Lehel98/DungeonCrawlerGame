using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DungeonTest.Persistence;
using DungeonTest.Model;
using System;
using Moq;

namespace DungeonUnitTest
{
    [TestClass]
    public class UnitTests
    {
        private DungeonGameModel _model;
        private Mock<IDungeonGameDataAccess> _mock;
        private InitMapData _initMapData;


        [TestInitialize]
        public void Initialize()
        {
            _initMapData = new InitMapData();

            _initMapData.TableSize = 19;
            _initMapData.NumberOfBearTraps = 4;
            _initMapData.NumberOfBushes = 10;
            _initMapData.NumberOfPuddles = 6;
            _initMapData.NumberOfFactories = 1;
            _initMapData.NumberOfHeals = 1;
            _initMapData.Scraps = new List<Field>();
            _initMapData.Scraps.Add(Field.Bulb);
            _initMapData.Scraps.Add(Field.Foil);
            _initMapData.Scraps.Add(Field.Gear);
            _initMapData.Scraps.Add(Field.Pipe);
            _initMapData.EndGameTime = 120;
            _initMapData.Rooms = new List<Tuple<Int32, Int32>>();
            _initMapData.Rooms.Add(new Tuple<Int32, Int32>(4, 7));

            _mock = new Mock<IDungeonGameDataAccess>();
            _mock.Setup(mock => mock.LoadMap(It.IsAny<String>()))
                .Returns(() => _initMapData);

            _model = new DungeonGameModel(_mock.Object);
        }

        [TestMethod]
        public void NewGame()
        {
            _model.NewGame("Intro");

            // ellenõrizzük, hogy meghívták-e a LoadMap mûveletet a megadott paraméterrel
            _mock.Verify(dataAccess => dataAccess.LoadMap("Intro"), Times.Once());

            Assert.IsTrue(_model.EnemyTimerIsRunning);
            Assert.IsFalse(_model.PlayerIsHidden);
            Assert.AreEqual(_model.CurrentMap, "Intro");
            Assert.IsFalse(_model.HasBulb);
            Assert.IsFalse(_model.HasFoil);
            Assert.IsFalse(_model.HasGear);
            Assert.IsFalse(_model.HasPipe);
            Assert.AreEqual(_model.Key, null);
            Assert.AreEqual(_model.CurrentHP, _model.MaxHP);
            Assert.AreEqual(_model.MaxValueOfTimer, _initMapData.EndGameTime);
            Assert.IsFalse(_model.ExitGatesArePowered);
            Assert.AreEqual(_model.TimeLeft, -1);
            Assert.AreEqual(_model.PlayerX, 1);
            Assert.AreEqual(_model.PlayerY, 1);

            Assert.AreEqual(_model.GetField(6, 9), Field.Heal);
            Assert.AreEqual(_model.GetField(1, 1), Field.Player);

            Int32 numberOfBearTraps = 0, numberOfBushes = 0, numberOfPuddles = 0, numberOfHeals = 0;
            Int32 numberOfBulbs = 0, numberOfFoils = 0, numberOfGears = 0, numberOfPipes = 0;

            for (Int32 i = 0; i < _model.TableSize; i++)
            {
                for (Int32 j = 0; j < _model.TableSize; j++)
                {
                    switch (_model.GetField(i, j))
                    {
                        case Field.BearTrap:
                            numberOfBearTraps++;
                            break;
                        case Field.Bush:
                            numberOfBushes++;
                            break;
                        case Field.Puddle:
                            numberOfPuddles++;
                            break;
                        case Field.Heal:
                            numberOfHeals++;
                            break;
                        case Field.Bulb:
                            numberOfBulbs++;
                            break;
                        case Field.Foil:
                            numberOfFoils++;
                            break;
                        case Field.Gear:
                            numberOfGears++;
                            break;
                        case Field.Pipe:
                            numberOfPipes++;
                            break;
                    }

                    if (i == 0 || j == 0 || i == _model.TableSize - 1 || j == _model.TableSize - 1)
                        Assert.IsTrue(_model.GetField(i, j) == Field.Wall || _model.GetField(i, j) == Field.ExitGate1
                            || _model.GetField(i, j) == Field.ExitGate2 || _model.GetField(i, j) == Field.ExitGate3
                            || _model.GetField(i, j) == Field.ExitGate4);
                }
            }

            Assert.AreEqual(numberOfBearTraps, 4);
            // alapból 10 bokor + 3 az 5x5-ös szobában
            Assert.IsTrue(numberOfBushes >= 10);
            Assert.AreEqual(numberOfPuddles, 6);
            Assert.AreEqual(numberOfHeals, 2);
            Assert.AreEqual(numberOfBulbs, 1);
            Assert.AreEqual(numberOfFoils, 1);
            Assert.AreEqual(numberOfGears, 1);
            Assert.AreEqual(numberOfPipes, 1);
        }

        [TestMethod]
        public void StepTest()
        {
            _model.DisposeGame();
            _model.NewGame("Intro");
            _model.GameIsRunning = true;

            _model.Step(2, 4);
            Assert.AreEqual(_model.PlayerX, 1);
            Assert.AreEqual(_model.PlayerY, 1);
            Assert.AreEqual(_model.GetField(1, 1), Field.Player);

            _model.CanStep = true;
            _model.Step(-1, 0);
            Assert.AreEqual(_model.PlayerX, 1);
            Assert.AreEqual(_model.PlayerY, 1);
            Assert.AreEqual(_model.GetField(1, 1), Field.Player);

            _model.CanStep = true;
            _model.Step(0, -1);
            Assert.AreEqual(_model.PlayerX, 1);
            Assert.AreEqual(_model.PlayerY, 1);
            Assert.AreEqual(_model.GetField(1, 1), Field.Player);

            _model.CanStep = true;
            _model.Step(1, 0);
            if (_model.GetLand(2, 1).IsOccupied)
            {
                Assert.AreEqual(_model.PlayerX, 1);
                Assert.AreEqual(_model.PlayerY, 1);
                Assert.AreEqual(_model.GetField(1, 1), Field.Player);
                Assert.AreNotEqual(_model.GetField(2, 1), Field.Player);
                Assert.AreNotEqual(_model.GetField(2, 1), Field.PlayerInPuddle);
                Assert.AreNotEqual(_model.GetField(2, 1), Field.TrappedEnemy);
                Assert.AreNotEqual(_model.GetField(2, 1), Field.HiddenPlayer);
            }
            else
            {
                Assert.AreEqual(_model.PlayerX, 2);
                Assert.AreEqual(_model.PlayerY, 1);

                switch (_model.GetBaseValue(2, 1))
                {
                    case Field.FreeTile:
                        Assert.AreEqual(_model.GetField(2, 1), Field.Player);
                        break;
                    case Field.Puddle:
                        Assert.AreEqual(_model.GetField(2, 1), Field.PlayerInPuddle);
                        break;
                    case Field.BearTrap:
                        Assert.AreEqual(_model.GetField(2, 1), Field.TrappedEnemy);
                        break;
                    case Field.Bush:
                        Assert.AreEqual(_model.GetField(2, 1), Field.HiddenPlayer);
                        break;
                }

                _model.CanStep = true;
                _model.Step(-1, 0);
                Assert.AreEqual(_model.PlayerX, 1);
                Assert.AreEqual(_model.PlayerY, 1);
            }

            _model.CanStep = true;
            _model.Step(0, 1);
            if (_model.GetLand(1, 2).IsOccupied)
            {
                Assert.AreEqual(_model.PlayerX, 1);
                Assert.AreEqual(_model.PlayerY, 1);
                Assert.AreEqual(_model.GetField(1, 1), Field.Player);
                Assert.AreNotEqual(_model.GetField(1, 2), Field.Player);
                Assert.AreNotEqual(_model.GetField(1, 2), Field.PlayerInPuddle);
                Assert.AreNotEqual(_model.GetField(1, 2), Field.TrappedEnemy);
                Assert.AreNotEqual(_model.GetField(1, 2), Field.HiddenPlayer);
            }
            else
            {
                Assert.AreEqual(_model.PlayerX, 1);
                Assert.AreEqual(_model.PlayerY, 2);

                switch (_model.GetBaseValue(1, 2))
                {
                    case Field.FreeTile:
                        Assert.AreEqual(_model.GetField(1, 2), Field.Player);
                        break;
                    case Field.Puddle:
                        Assert.AreEqual(_model.GetField(1, 2), Field.PlayerInPuddle);
                        break;
                    case Field.BearTrap:
                        Assert.AreEqual(_model.GetField(1, 2), Field.TrappedEnemy);
                        break;
                    case Field.Bush:
                        Assert.AreEqual(_model.GetField(1, 2), Field.HiddenPlayer);
                        break;
                }
            }
        }

        [TestMethod]
        public void Toplist()
        {
            _model.NewGame("Intro");

            _model.DisposeGame();

            _model.AddResult("Lehel", 21);

            _mock.Verify(mock => mock.AddResult("Intro", "Lehel", 21), Times.Once());

            _model.SaveToplist();

            _mock.Verify(mock => mock.SaveToplist(), Times.Once());
        }
    }
}