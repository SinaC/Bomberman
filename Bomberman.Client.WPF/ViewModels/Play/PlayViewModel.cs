﻿using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Bomberman.Client.Interfaces;
using Bomberman.Client.WPF.Helpers;
using Bomberman.Client.WPF.MVVM;
using Bomberman.Common.DataContracts;
using Bomberman.Common.Helpers;

namespace Bomberman.Client.WPF.ViewModels.Play
{
    public class PlayViewModel : ViewModelBase
    {
        private bool _isGameStarted;

        private ObservableCollection<CellItem> _cells;

        public ObservableCollection<CellItem> Cells
        {
            get { return _cells; }
            set { Set(() => Cells, ref _cells, value); }
        }

        private int _width;

        public int Width
        {
            get { return _width; }
            set { Set(() => Width, ref _width, value); }
        }

        private int _height;

        public int Height
        {
            get { return _height; }
            set { Set(() => Height, ref _height, value); }
        }

        #region ViewModelBase

        protected override void UnsubscribeFromClientEvents(IClient oldClient)
        {
            oldClient.ConnectionLost -= OnConnectionLost;
            oldClient.GameStarted -= OnGameStarted;
            oldClient.GameDraw -= OnGameDraw;
            oldClient.GameLost -= OnGameLost;
            oldClient.GameWon -= OnGameWon;
            oldClient.EntityAdded -= OnEntityAdded;
            oldClient.EntityDeleted -= OnEntityDeleted;
            oldClient.EntityMoved -= OnEntityMoved;
            oldClient.EntityTransformed -= OnEntityTransformed;
            oldClient.MultipleEntityModified -= OnMultipleEntityModified;
            oldClient.BonusPickedUp -= OnBonusPickedUp;
        }

        protected override void SubscribeToClientEvents(IClient newClient)
        {
            newClient.ConnectionLost += OnConnectionLost;
            newClient.GameStarted += OnGameStarted;
            newClient.GameDraw += OnGameDraw;
            newClient.GameLost += OnGameLost;
            newClient.GameWon += OnGameWon;
            newClient.EntityAdded += OnEntityAdded;
            newClient.EntityDeleted += OnEntityDeleted;
            newClient.EntityMoved += OnEntityMoved;
            newClient.EntityTransformed += OnEntityTransformed;
            newClient.MultipleEntityModified += OnMultipleEntityModified;
            newClient.BonusPickedUp += OnBonusPickedUp;
        }

        #endregion

        #region IClient event handlers

        private void OnGameStarted(Map map)
        {
            _isGameStarted = true;
            ExecuteOnUIThread.Invoke(() => BuildCells(map));
        }

        private void OnGameLost()
        {
            _isGameStarted = false;
        }

        private void OnGameDraw()
        {
            _isGameStarted = false;
        }

        private void OnConnectionLost()
        {
            _isGameStarted = false;
        }

        private void OnGameWon(bool won, string name)
        {
            _isGameStarted = false;
        }

        private void OnMultipleEntityModified()
        {
            ExecuteOnUIThread.Invoke(() => BuildCells(Client.GameMap));
        }

        private void OnEntityTransformed(EntityTypes oldEntity, EntityTypes newEntity, int locationX, int locationY)
        {
            CellItem cell = Cells.FirstOrDefault(c => c.X == locationX && c.Y == locationY && c.Type == oldEntity);
            if (cell != null)
            {
                SolidColorBrush color = GetCellColor(newEntity);
                string text = GetCellText(newEntity);
                cell.Color = color;
                cell.Text = text;
                cell.Type = newEntity;
            }
        }

        private void OnEntityMoved(EntityTypes entity, int oldLocationX, int oldLocationY, int newLocationX, int newLocationY)
        {
            CellItem cell = Cells.FirstOrDefault(c => c.X == oldLocationX && c.Y == oldLocationY && c.Type == entity);
            if (cell != null)
            {
                cell.X = newLocationX;
                cell.Y = newLocationY;
            }
        }

        private void OnEntityDeleted(EntityTypes entity, int locationX, int locationY)
        {
            CellItem cell = Cells.FirstOrDefault(c => c.X == locationX && c.Y == locationY && c.Type == entity);
            if (cell != null)
                ExecuteOnUIThread.Invoke(() => Cells.Remove(cell));
            // TODO: if removed is bomb -> explosion
        }

        private void OnEntityAdded(EntityTypes entity, int locationX, int locationY)
        {
            CellItem cell = CreateCell(entity, locationX, locationY);
            if (cell != null)
                ExecuteOnUIThread.Invoke(() => Cells.Add(cell));
        }

        private void OnBonusPickedUp(EntityTypes bonus)
        {
            // TODO:
        }

        #endregion

        public void MoveLeft()
        {
            if (_isGameStarted)
                Client.Do(x => x.Move(Directions.Left));
        }

        public void MoveRight()
        {
            if (_isGameStarted)
                Client.Do(x => x.Move(Directions.Right));
        }

        public void MoveUp()
        {
            if (_isGameStarted)
                Client.Do(x => x.Move(Directions.Up));
        }

        public void MoveDown()
        {
            if (_isGameStarted)
                Client.Do(x => x.Move(Directions.Down));
        }

        public void PlaceBomb()
        {
            if (_isGameStarted)
                Client.Do(x => x.PlaceBomb());
        }

        private SolidColorBrush GetCellColor(EntityTypes type)
        {
            Color color = Colors.White;
            if (IsFlames(type))
                color = Colors.Red;
            if (IsBonus(type))
                color = Colors.Green;
            if (IsBomb(type))
                color = Colors.Yellow;
            if (IsWall(type))
                color = Colors.Black;
            return new SolidColorBrush(color);
        }

        public string GetCellText(EntityTypes type)
        {
            char c = ' ';
            if (IsPlayer(type))
                c = 'X';
            else if (IsOpponent(type))
                c = GetOpponent(type);
            else if (IsBonus(type))
                c = GetBonus(type);
            else if (IsBomb(type))
                c = '*';
            else if (IsWall(type))
                c = '█';
            else if (IsDust(type))
                c = '.';
            return c.ToString(CultureInfo.InvariantCulture);
        }

        private int GetCellZIndex(EntityTypes type)
        {
            if (IsPlayer(type) || IsOpponent(type))
                return 100;
            if (IsBomb(type))
                return 50;
            return 0;
        }

        private CellItem CreateCell(EntityTypes type, int x, int y)
        {
            if (type == EntityTypes.Empty)
                return null;
            SolidColorBrush color = GetCellColor(type);
            string text = GetCellText(type);
            int z = GetCellZIndex(type);
            CellItem cell = new CellItem
            {
                Type = type,
                X = x,
                Y = y,
                Z = z,
                Width = 20,
                Height = 20,
                Color = color,
                Text = text
            };
            return cell;
        }

        private void BuildCells(Map map)
        {
            Cells = new ObservableCollection<CellItem>();
            Width = map.Description.Size*20;
            Height = map.Description.Size * 20;
            for (int y = 0; y < map.Description.Size; y++)
                for (int x = 0; x < map.Description.Size; x++)
                {
                    EntityTypes type = map.GetEntity(x, y);
                    CellItem cell = CreateCell(type, x, y);
                    if (cell != null)
                        Cells.Add(cell);
                }
        }

        #region Entity

        private static bool IsEmpty(EntityTypes entity)
        {
            return (entity & EntityTypes.Empty) == EntityTypes.Empty;
        }

        private static bool IsDust(EntityTypes entity)
        {
            return (entity & EntityTypes.Dust) == EntityTypes.Dust;
        }

        private static bool IsWall(EntityTypes entity)
        {
            return (entity & EntityTypes.Wall) == EntityTypes.Wall;
        }

        private bool IsPlayer(EntityTypes entity)
        {
            return (entity & Client.Entity) == Client.Entity;
        }

        private bool IsOpponent(EntityTypes entity)
        {
            return !IsPlayer(entity)
                   && ((entity & EntityTypes.Player1) == EntityTypes.Player1
                       || (entity & EntityTypes.Player2) == EntityTypes.Player2
                       || (entity & EntityTypes.Player3) == EntityTypes.Player3
                       || (entity & EntityTypes.Player4) == EntityTypes.Player4);
        }

        private static bool IsFlames(EntityTypes entity)
        {
            return (entity & EntityTypes.Flames) == EntityTypes.Flames;
        }

        private static bool IsBonus(EntityTypes entity)
        {
            return (entity & EntityTypes.BonusFireUp) == EntityTypes.BonusFireUp
                   || (entity & EntityTypes.BonusNoClipping) == EntityTypes.BonusNoClipping
                   || (entity & EntityTypes.BonusBombUp) == EntityTypes.BonusBombUp
                   || (entity & EntityTypes.BonusBombKick) == EntityTypes.BonusBombKick
                   || (entity & EntityTypes.BonusFlameBomb) == EntityTypes.BonusFlameBomb
                   || (entity & EntityTypes.BonusFireDown) == EntityTypes.BonusFireDown
                   || (entity & EntityTypes.BonusBombDown) == EntityTypes.BonusBombDown
                   || (entity & EntityTypes.BonusH) == EntityTypes.BonusH
                   || (entity & EntityTypes.BonusI) == EntityTypes.BonusI
                   || (entity & EntityTypes.BonusJ) == EntityTypes.BonusJ;
        }

        private static bool IsBomb(EntityTypes entity)
        {
            return (entity & EntityTypes.Bomb) == EntityTypes.Bomb;
        }

        private static char GetOpponent(EntityTypes entity)
        {
            if ((entity & EntityTypes.Player1) == EntityTypes.Player1)
                return '1';
            if ((entity & EntityTypes.Player2) == EntityTypes.Player2)
                return '2';
            if ((entity & EntityTypes.Player3) == EntityTypes.Player3)
                return '3';
            if ((entity & EntityTypes.Player4) == EntityTypes.Player4)
                return '4';
            return '\\';
        }

        private static char GetBonus(EntityTypes entity)
        {
            if ((entity & EntityTypes.BonusFireUp) == EntityTypes.BonusFireUp)
                return 'a';
            if ((entity & EntityTypes.BonusNoClipping) == EntityTypes.BonusNoClipping)
                return 'b';
            if ((entity & EntityTypes.BonusBombUp) == EntityTypes.BonusBombUp)
                return 'c';
            if ((entity & EntityTypes.BonusBombKick) == EntityTypes.BonusBombKick)
                return 'd';
            if ((entity & EntityTypes.BonusFlameBomb) == EntityTypes.BonusFlameBomb)
                return 'e';
            if ((entity & EntityTypes.BonusFireDown) == EntityTypes.BonusFireDown)
                return 'f';
            if ((entity & EntityTypes.BonusBombDown) == EntityTypes.BonusBombDown)
                return 'g';
            if ((entity & EntityTypes.BonusH) == EntityTypes.BonusH)
                return 'h';
            if ((entity & EntityTypes.BonusI) == EntityTypes.BonusI)
                return 'i';
            if ((entity & EntityTypes.BonusJ) == EntityTypes.BonusJ)
                return 'j';
            return '/';
        }

        #endregion
    }

    public class PlayViewModelDesignData : PlayViewModel
    {
        public PlayViewModelDesignData()
        {
            Width = 200;
            Height = 200;
        }
    }
}