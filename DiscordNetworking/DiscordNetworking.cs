using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Handler;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace AMP.DiscordNetworking {
    internal class DiscordNetworking : NetworkHandler {
        
        public enum Mode {
            NONE,
            CLIENT,
            SERVER
        }
        
        Discord.NetworkManager networkManager;
        Discord.LobbyManager lobbyManager;
        Discord.UserManager userManager;

        Discord.User currentUser;

        public Discord.Lobby currentLobby;

        public Mode mode = Mode.NONE;

        private int currentId = 1;

        private const int RELIABLE_CHANNEL = 0;
        private const int UNRELIABLE_CHANNEL = 1;

        Discord.Discord discord;
        public DiscordNetworking() {
            discord = new Discord.Discord(Config.DISCORD_CLIENT_ID, (UInt64) Discord.CreateFlags.NoRequireDiscord);

            networkManager = discord.GetNetworkManager();
            lobbyManager = discord.GetLobbyManager();
            userManager = discord.GetUserManager();

            // Get yourself
            //currentUser = userManager.GetCurrentUser();

            RegisterEvents();
        }

        public override void RunCallbacks() {
            discord.RunCallbacks();
        }

        public void CreateLobby(uint maxPlayers, Action callback) {
            var txn = lobbyManager.GetLobbyCreateTransaction();

            // Set lobby information
            txn.SetCapacity(maxPlayers);
            txn.SetType(Discord.LobbyType.Public);
            //txn.SetMetadata("a", "123");

            lobbyManager.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) => {
                Log.Debug(String.Format("[Server] Lobby \"{0}\" created with secret \"{1}\".", lobby.Id, lobby.Secret));

                mode = Mode.SERVER;

                InitNetworking(lobby);

                callback();
            });
        }

        public override void Connect() {
            Log.Debug(mode);
            if(mode == Mode.SERVER) {
                onPacketReceived.Invoke(new Packet(PacketWriter.Welcome(currentId++).ToArray()));
            } else {

            }
        }

        public void JoinLobby(long lobbyId, string secret, Action callback) {
            lobbyManager.ConnectLobby(lobbyId, secret, (Result result, ref Lobby lobby) => {
                if(result == Result.Ok) {
                    Console.WriteLine("[Client] Connected to lobby {0}!", lobby.Id);

                    mode = Mode.CLIENT;

                    InitNetworking(lobby);

                    callback();
                }
            });
        }

        public void RegisterEvents() {
            lobbyManager.OnMemberConnect += LobbyManager_OnMemberConnect;
            lobbyManager.OnMemberDisconnect += LobbyManager_OnMemberDisconnect;
            lobbyManager.OnNetworkMessage += LobbyManager_OnNetworkMessage;
            lobbyManager.OnLobbyDelete += LobbyManager_OnLobbyDelete;
        }

        private void LobbyManager_OnLobbyDelete(long lobbyId, uint reason) {
            if(lobbyId == currentLobby.Id) isConnected = false;
        }

        public void InitNetworking(Discord.Lobby lobby) {
            lobbyManager.ConnectNetwork(lobby.Id);
            lobbyManager.OpenNetworkChannel(lobby.Id, RELIABLE_CHANNEL, true);
            lobbyManager.OpenNetworkChannel(lobby.Id, UNRELIABLE_CHANNEL, false);

            isConnected = true;
            currentLobby = lobby;
        }

        private long[] userIds = new long[0];

        private void LobbyManager_OnMemberDisconnect(long lobbyId, long userId) {
            UpdateUserIds();
        }

        private void LobbyManager_OnMemberConnect(long lobbyId, long userId) {
            UpdateUserIds();

            userManager.GetUser(userId, (Result result, ref User user) => {
                if(result == Result.Ok) {
                    if(mode == Mode.SERVER) {
                        SendUserRequiredData(user);
                    }
                }
            });
        }

        public void SendUserRequiredData(User user) {
            SendReliable(PacketWriter.Welcome(currentId++), user.Id);
        }

        private void LobbyManager_OnNetworkMessage(long lobbyId, long userId, byte channelId, byte[] data) {
            Packet packet = new Packet(data);

            onPacketReceived.Invoke(packet);
        }

        private void UpdateUserIds() {
            userIds = new long[lobbyManager.MemberCount(currentLobby.Id)];
            for(int i = 0; i < lobbyManager.MemberCount(currentLobby.Id); i++) {
                userIds[i] = lobbyManager.GetMemberUserId(currentLobby.Id, i);
            }
        }

        private byte[] PreparePacket(Packet packet) {
            packet.WriteLength();
            return packet.ToArray();
        }

        public override void SendReliable(Packet packet) {
            if(packet == null) return;
            
            foreach(int userId in userIds) {
                SendReliable(packet, userId);
            }
        }

        private void SendReliable(Packet packet, long userId) {
            if(packet == null) return;

            byte[] data = PreparePacket(packet);
            lobbyManager.SendNetworkMessage(currentLobby.Id, userId, RELIABLE_CHANNEL, data);
        }

        public override void SendUnreliable(Packet packet) {
            if(packet == null) return;

            foreach(int userId in userIds) {
                SendUnreliable(packet, userId);
            }
        }

        private void SendUnreliable(Packet packet, long userId) {
            if(packet == null) return;

            byte[] data = PreparePacket(packet);
            lobbyManager.SendNetworkMessage(currentLobby.Id, userId, UNRELIABLE_CHANNEL, data);
        }
    }
}
