﻿using System;
using System.ServiceModel;
using Bomberman.Common;
using Bomberman.Common.Contracts;
using Bomberman.Common.DataContracts;
using Bomberman.Common.Helpers;
using Bomberman.Server.Interfaces;

namespace Bomberman.Server.WCFHost
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.Single)]
    public class WCFHost : IHost, IDisposable
    {
        private ServiceHost _serviceHost;

        private readonly IPlayerManager _playerManager;
        private readonly Func<string, IBombermanCallback, IPlayer> _createPlayerFunc;

        public string Port { get; private set; }

        public WCFHost(string port, IPlayerManager playerManager, Func<string, IBombermanCallback, IPlayer> createPlayerFunc)
        {
            if (port == null)
                throw new ArgumentNullException("port");
            if (playerManager == null)
                throw new ArgumentNullException("playerManager");
            if (createPlayerFunc == null)
                throw new ArgumentNullException("createPlayerFunc");

            Port = port;
            _playerManager = playerManager;
            _createPlayerFunc = createPlayerFunc;
        }

        public void Start()
        {
            Uri baseAddress = new Uri("net.tcp://localhost:" + Port);

            _serviceHost = new ServiceHost(this, baseAddress);
            _serviceHost.AddServiceEndpoint(typeof(IBomberman), new NetTcpBinding(SecurityMode.None), "/Bomberman");
            _serviceHost.Open();

            foreach (var endpt in _serviceHost.Description.Endpoints)
            {
                Log.WriteLine(Log.LogLevels.Info, "Enpoint address:\t{0}", endpt.Address);
                Log.WriteLine(Log.LogLevels.Info, "Enpoint binding:\t{0}", endpt.Binding);
                Log.WriteLine(Log.LogLevels.Info, "Enpoint contract:\t{0}\n", endpt.Contract.ContractType.Name);
            }
        }

        public void Stop()
        {
            // Close service host
            _serviceHost.Close();
        }

        #region IHost

        public event LoginEventHandler HostLogin;
        public event LogoutEventHandler HostLogout;
        public event StartGameEventHandler HostStartGame;
        public event MoveEventHandler HostMove;
        public event PlaceBombEventHandler HostPlaceBomb;
        public event ChatEventHandler HostChat;

        public event DisconnectPlayerEventHandler PlayerDisconnected;

        #endregion

        #region IBomberman

        public void Login(string playerName)
        {
            Log.WriteLine(Log.LogLevels.Debug, "Login {0}", playerName);

            LoginResults result = LoginResults.Successful;
            IPlayer player = null;
            int id = -1;
            lock (_playerManager.LockObject)
            {
                if (!_playerManager.IsValid(playerName))
                {
                    result = LoginResults.FailedInvalidName;
                    Log.WriteLine(Log.LogLevels.Warning, "Cannot register {0} because name is invalid", playerName);
                }
                else if (_playerManager.Exists(playerName))
                {
                    result = LoginResults.FailedDuplicateName;
                    Log.WriteLine(Log.LogLevels.Warning, "Cannot register {0} because it already exists", playerName);
                }
                else if (_playerManager.PlayerCount >= _playerManager.MaxPlayers)
                {
                    result = LoginResults.FailedTooManyPlayers;
                    Log.WriteLine(Log.LogLevels.Warning, "Cannot register {0} because too many players are already connected", playerName);
                }
                else
                {
                    player = _createPlayerFunc(playerName, Callback);
                    //
                    player.ConnectionLost += OnConnectionLost;
                    id = _playerManager.Add(player);
                }
            }
            if (id >= 0 && player != null && result == LoginResults.Successful)
            {
                player.ResetTimeout(); // player is alive
                HostLogin.Do(x => x(player, id));
            }
            else
            {
                Log.WriteLine(Log.LogLevels.Info, "Register failed for player {0}", playerName);
                //
                Callback.OnLogin(result, -1, EntityTypes.Empty, null, false);
            }
        }

        public void Logout()
        {
            Log.WriteLine(Log.LogLevels.Debug, "Logout");

            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
                HostLogout.Do(x => x(player));
            }
            else
                Log.WriteLine(Log.LogLevels.Warning, "Logout from unknown player");
        }

        public void StartGame(int mapId)
        {
            Log.WriteLine(Log.LogLevels.Debug, "StartGame {0}", mapId);

            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
                HostStartGame.Do(x => x(player, mapId));
            }
            else
                Log.WriteLine(Log.LogLevels.Warning, "StartGame from unknown player");
        }

        public void Move(Directions direction)
        {
            Log.WriteLine(Log.LogLevels.Debug, "Move {0}: {1}", direction);

            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
                HostMove.Do(x => x(player, direction));
            }
            else
                Log.WriteLine(Log.LogLevels.Warning, "Move from unknown player");
        }

        public void PlaceBomb()
        {
            Log.WriteLine(Log.LogLevels.Debug, "PlaceBomb");

            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
                HostPlaceBomb.Do(x => x(player));
            }
            else
                Log.WriteLine(Log.LogLevels.Warning, "PlaceBomb from unknown player");
        }

        public void Chat(string msg)
        {
            Log.WriteLine(Log.LogLevels.Debug, "Chat {0}", msg);

            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
                HostChat.Do(x => x(player, msg));
            }
            else
                Log.WriteLine(Log.LogLevels.Warning, "Chat from unknown player");
        }

        public void Heartbeat()
        {
            IPlayer player = _playerManager[Callback];
            if (player != null)
            {
                player.ResetTimeout(); // player is alive
            }
            else
            {
                Log.WriteLine(Log.LogLevels.Warning, "Heartbeat from unknown player");
            }
        }

        #endregion

        private void OnConnectionLost(IPlayer player)
        {
            PlayerDisconnected.Do(x => x(player));
        }

        private IBombermanCallback Callback
        {
            get
            {
                //MessageProperties messageProperties = OperationContext.Current.IncomingMessageProperties;
                //RemoteEndpointMessageProperty endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                //if (endpointProperty != null)
                //{
                //    IPAddress address = IPAddress.Parse(endpointProperty.Address);
                //}
                return OperationContext.Current.GetCallbackChannel<IBombermanCallback>();
            }
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _serviceHost.Close();
                }
                catch(Exception ex)
                {
                    _serviceHost.Abort();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
