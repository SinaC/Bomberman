﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bomberman.Common.DataContracts;

namespace Bomberman.Client.Console
{
    public class ConsoleUI
    {
        private int _playerId;
        private EntityTypes _playerEntity;
        private Map _map;

        public ConsoleUI()
        {
            System.Console.SetWindowSize(80, 30);
            System.Console.SetBufferSize(80, 30);
        }

        public void OnLoggedOn(LoginResults result, int playerId, EntityTypes playerEntity, List<MapDescription> maps, bool isGameStarted)
        {
            if (result == LoginResults.Successful)
            {
                _playerId = playerId;
                _playerEntity = playerEntity;
                string mapsAsString = maps == null ? String.Empty : maps.Select(x => String.Format("[{0},{1},{2}]", x.Id, x.Title, x.Size)).Aggregate((s, s1) => s + s1);
                System.Console.SetCursorPosition(30, 20);
                System.Console.Write("Login successful as {0}. Maps: {1}. Started: {2}", playerId, mapsAsString, isGameStarted);
            }
            else
            {
                System.Console.SetCursorPosition(30, 20);
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Write("Login failed: {0}", result);
            }
        }

        public void OnUserConnected(string player, int playerId)
        {
            System.Console.SetCursorPosition(30,1);
            System.Console.Write("New user connected: {0}|{1}", player, playerId);
        }

        public void OnUserDisconnected(string player, int playerId)
        {
            System.Console.SetCursorPosition(30, 1);
            System.Console.Write("User disconnected: {0}|{1}", player, playerId);
        }

        public void OnGameStarted(Map map)
        {
            _map = map;
            
            System.Console.SetCursorPosition(30, 2);
            System.Console.Write("Game started: Map: {0},{1}", map.Description.Id, map.Description.Title);
            for(int y = 0; y < map.Description.Size; y++)
                for(int x = 0; x < map.Description.Size; x++)
                    DisplayEntity(x, y);
        }

        public void OnBonusPickedUp(EntityTypes bonus)
        {
            System.Console.SetCursorPosition(30, 10);
            System.Console.Write("New bonus: {0}", bonus);
        }

        public void OnChatReceived(int playerId, string player, string msg)
        {
            System.Console.SetCursorPosition(30, 0);
            System.Console.Write("{0}:{1}", player, msg);
        }

        public void OnEntityAdded(EntityTypes entity, int locationX, int locationY)
        {
            DisplayEntity(locationX, locationY);
        }

        public void OnEntityDeleted(EntityTypes entity, int locationX, int locationY)
        {
            DisplayEntity(locationX, locationY);
        }
        
        public void OnEntityMoved(EntityTypes entity, int oldLocationX, int oldLocationY, int newLocationX, int newLocationY)
        {
            DisplayEntity(oldLocationX, oldLocationY);
            DisplayEntity(newLocationX, newLocationY);
        }

        public void OnEntityTransformed(EntityTypes oldEntity, EntityTypes newEntity, int locationX, int locationY)
        {
            DisplayEntity(locationX, locationY);
        }

        public void OnGameDraw()
        {
            System.Console.SetCursorPosition(30, 19);
            System.Console.Write("Game ended in a DRAW");
        }

        public void OnGameLost()
        {
            System.Console.SetCursorPosition(30, 19);
            System.Console.Write("You have LOST");
        }

        public void OnGameWon(bool won, string name)
        {
            System.Console.SetCursorPosition(30, 19);
            if (won)
                System.Console.Write("You have WON");
            else
                System.Console.Write("Game won by {0}", name);
        }

        public void OnKilled(string name)
        {
            System.Console.SetCursorPosition(30, 18);
            System.Console.Write("Player {0} killed", name);
        }

        public void OnConnectionLost()
        {
            System.Console.SetCursorPosition(30, 4);
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.Write("Connection to server lost");
        }

        public void OnRedraw()
        {
            for (int y = 0; y < _map.Description.Size; y++)
                for (int x = 0; x < _map.Description.Size; x++)
                    DisplayEntity(x, y);
        }

        //
        private void DisplayEntity(EntityTypes entity, int x, int y)
        {
            ConsoleColor color = ConsoleColor.Gray;
            if (IsFlames(entity))
                color = ConsoleColor.Red;
            if (IsBonus(entity))
                color = ConsoleColor.Green;
            if (IsBomb(entity))
                color = ConsoleColor.Yellow;

            char c = '?';
            if (IsPlayer(entity))
                c = 'X';
            else if (IsOpponent(entity))
                c = GetOpponent(entity);
            else if (IsBonus(entity))
                c = GetBonus(entity);
            else if (IsBomb(entity))
                c = '*';
            else if (IsWall(entity))
                c = '█';
            else if (IsDust(entity))
                c = '.';
            else if (IsEmpty(entity))
                c = '_';

            System.Console.ForegroundColor = color;
            System.Console.SetCursorPosition(x, y);
            System.Console.Write(c);

            System.Console.ResetColor();
        }

        private void DisplayEntity(int x, int y)
        {
            EntityTypes entity = _map.GetEntity(x, y);

            DisplayEntity(entity, x, y);
        }

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
            return (entity & _playerEntity) == _playerEntity;
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
    }
}
