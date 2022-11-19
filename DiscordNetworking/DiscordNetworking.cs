using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Handler;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.Overlay;
using AMP.SupportFunctions;
using AMP.Useless;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThunderRoad;

namespace AMP.DiscordNetworking {
    internal class DiscordNetworking : NetworkHandler {

        internal static DiscordNetworking instance;

        internal enum Mode {
            NONE,
            CLIENT,
            SERVER
        }

        ActivityManager activityManager;
        NetworkManager networkManager;
        LobbyManager lobbyManager;
        UserManager userManager;

        internal User currentUser;

        internal Lobby currentLobby;
        internal string activitySecret = "";

        internal Mode mode = Mode.NONE;

        private const int RELIABLE_CHANNEL = 0;
        private const int UNRELIABLE_CHANNEL = 1;

        internal Action<User, NetPacket> onPacketReceivedFromUser;

        private Dictionary<long, ulong> userPeers = new Dictionary<long, ulong>();

        private Dictionary<long, User> users = new Dictionary<long, User>();
        private long[] userIds = new long[0];

        private static Discord.Discord discord;
        internal DiscordNetworking() {
            instance = this;

            if(discord == null) discord = new Discord.Discord(Config.DISCORD_APP_ID, (UInt64) CreateFlags.NoRequireDiscord);

            activityManager = discord.GetActivityManager();
            networkManager = discord.GetNetworkManager();
            lobbyManager = discord.GetLobbyManager();
            userManager = discord.GetUserManager();

            // Get yourself
            //currentUser = userManager.GetCurrentUser();
            userManager.OnCurrentUserUpdate += () => {
                currentUser = userManager.GetCurrentUser();
            };

            RegisterEvents();
        }

        internal override void RunCallbacks() {
            discord.RunCallbacks();
        }

        internal override void RunLateCallbacks() {
            networkManager.Flush();
            lobbyManager.FlushNetwork();
        }

        internal override void Disconnect() {
            try {
                if(currentLobby.OwnerId == currentUser.Id) {
                    lobbyManager.DeleteLobby(currentLobby.Id, (result) => {
                        if(result == Result.Ok) {
                            isConnected = false;

                            UpdateActivity();
                        }
                    });
                } else {
                    lobbyManager.DisconnectLobby(currentLobby.Id, (result) => {
                        if(result == Result.Ok) {
                            isConnected = false;

                            UpdateActivity();
                        }
                    });
                }
            } catch(Exception e) { Log.Err(e); }

            try {
                foreach(ulong peerId in userPeers.Values) {
                    networkManager.ClosePeer(peerId);
                //
                //    networkManager.CloseChannel(peerId, RELIABLE_CHANNEL);
                //    networkManager.CloseChannel(peerId, UNRELIABLE_CHANNEL);
                }
            } catch(Exception e) { Log.Err(e); }

            users.Clear();
            userPeers.Clear();
            userIds = new long[0];
            currentLobby.Id = -1;
        }

        internal void CreateLobby(uint maxPlayers, Action callback) {
            var txn = lobbyManager.GetLobbyCreateTransaction();

            // Set lobby information
            txn.SetCapacity(maxPlayers);
            txn.SetType(Discord.LobbyType.Public);
            //txn.SetMetadata("a", "123");

            lobbyManager.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) => {
                Log.Debug(Defines.SERVER, $"Lobby \"{lobby.Id}\" created with secret \"{lobby.Secret}\".");

                if(lobby.Id <= 0) return;

                mode = Mode.SERVER;

                InitNetworking(lobby);

                callback();
            });
        }

        internal override void Connect() {
            if(mode == Mode.SERVER) {
                //onPacketReceived.Invoke(new WelcomePacket(currentUser.Id));
            } else {
                new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION).SendToServerReliable();
            }
        }

        internal void JoinLobby(string secret, Action callback) {
            lobbyManager.ConnectLobbyWithActivitySecret(secret, (Result result, ref Lobby lobby) => {
                if(result == Result.Ok) {
                    Log.Debug(Defines.CLIENT, $"Connected to lobby {lobby.Id}!");

                    mode = Mode.CLIENT;

                    InitNetworking(lobby);

                    callback();
                }
            });
        }

        internal void RegisterEvents() {
            lobbyManager.OnMemberConnect += LobbyManager_OnMemberConnect;
            lobbyManager.OnMemberDisconnect += LobbyManager_OnMemberDisconnect;
            //lobbyManager.OnNetworkMessage += LobbyManager_OnNetworkMessage;
            lobbyManager.OnLobbyDelete += LobbyManager_OnLobbyDelete;
            lobbyManager.OnMemberUpdate += LobbyManager_OnMemberUpdate;

            networkManager.OnRouteUpdate += NetworkManager_OnRouteUpdate;
            networkManager.OnMessage += NetworkManager_OnMessage; ;

            activityManager.OnActivityJoin += ActivityManager_OnActivityJoin;
        }

        private string routeData = "";
        private void NetworkManager_OnRouteUpdate(string routeData) {
            this.routeData = routeData;

            if(currentLobby.Id <= 0) return;

            var txn = lobbyManager.GetMemberUpdateTransaction(currentLobby.Id, currentUser.Id);
            txn.SetMetadata("route", routeData);
            lobbyManager.UpdateMember(currentLobby.Id, currentUser.Id, txn, (result => {
                // Who needs error handling anyway
                Console.WriteLine(result);
            }));
        }

        private void LobbyManager_OnMemberUpdate(long lobbyId, long userId) {
            if(userPeers.ContainsKey(userId)) {
                var peerId = userPeers[userId];
                var newRoute = lobbyManager.GetMemberMetadataValue(lobbyId, userId, "metadata.route");
                networkManager.UpdatePeer(peerId, newRoute);
            }else if(users.ContainsKey(userId)) {
                OpenChannels(userId, currentLobby.Id);
            }
        }

        private void ActivityManager_OnActivityJoin(string secret) {
            DiscordGUIManager.JoinLobby(secret);
        }

        private void LobbyManager_OnLobbyDelete(long lobbyId, uint reason) {
            if(lobbyId == currentLobby.Id) {
                isConnected = false;
                UpdateActivity();
            }
        }

        internal void InitNetworking(Lobby lobby) {
            string myPeerId = Convert.ToString(networkManager.GetPeerId());

            var txn = lobbyManager.GetMemberUpdateTransaction(lobby.Id, currentUser.Id);
            txn.SetMetadata("metadata.peer_id", myPeerId);
            txn.SetMetadata("metadata.route", routeData);
            lobbyManager.UpdateMember(lobby.Id, currentUser.Id, txn, (result) => {
                // Who needs error handling anyway
                //Console.WriteLine(result);
            });

            //lobbyManager.ConnectNetwork(lobby.Id);
            //lobbyManager.OpenNetworkChannel(lobby.Id, RELIABLE_CHANNEL, true);
            //lobbyManager.OpenNetworkChannel(lobby.Id, UNRELIABLE_CHANNEL, false);

            isConnected = true;
            currentLobby = lobby;

            UpdateUserIds();
        }

        private void OpenChannels(long userId, long lobbyId) {
            if(userPeers.ContainsKey(userId)) return;
            if(mode != Mode.SERVER && userId != currentLobby.OwnerId) return;

            string rawPeerId;
            string route;
            try {
                rawPeerId = lobbyManager.GetMemberMetadataValue(lobbyId, userId, "metadata.peer_id");
                route = lobbyManager.GetMemberMetadataValue(lobbyId, userId, "metadata.route");
            }catch(ResultException) {
                return;
            }

            var peerId = System.Convert.ToUInt64(rawPeerId);

            networkManager.OpenPeer(peerId, route);

            networkManager.OpenChannel(peerId, RELIABLE_CHANNEL, true);
            networkManager.OpenChannel(peerId, UNRELIABLE_CHANNEL, false);

            userPeers.Add(userId, peerId);

            Log.Debug(Defines.DISCORD_SDK, "Opened peer to " + userId + " => " + peerId);

            if(mode == Mode.SERVER) {
                userManager.GetUser(userId, (Result result, ref User user) => {
                    if(result == Result.Ok) {
                        ClientData clientData = new ClientData(user.Id);
                        clientData.name = NameColorizer.FormatSpecialName(user.Id.ToString(), user.Username);
                        ModManager.serverInstance.GreetPlayer(clientData);
                    }
                });
            }
        }

        internal void UpdateActivity() {
            Discord.Activity activity;
            if(isConnected) {
                activitySecret = lobbyManager.GetLobbyActivitySecret(currentLobby.Id);
                activity = new Discord.Activity {
                    State = "Playing on " + Level.current.data.id,
                    Details = $"Blade & Sorcery Multiplayer ({ Defines.MOD_NAME })",
                    Party = {
                        Id = currentLobby.Id.ToString(),
                        Size = {
                            CurrentSize = lobbyManager.MemberCount(currentLobby.Id),
                            MaxSize = (int) currentLobby.Capacity,
                        },
                    },
                    Secrets = {
                        Join = activitySecret,
                    },
                    Instance = true,
                };
            } else {
                string state = "Playing Solo";
                if(Level.current != null) {
                    state = "Playing on " + Level.current.data.id;
                }

                activity = new Discord.Activity {
                    State = state,
                    Details = "Blade & Sorcery",
                    Instance = true,
                };
            }

            activityManager.UpdateActivity(activity, (result) => {
                //Log.Debug($"Updated Activity {result}");
            });
        }

        private void LobbyManager_OnMemberDisconnect(long lobbyId, long userId) {
            UpdateUserIds();

            if(ModManager.serverInstance.clients.ContainsKey(userId)) {
                ModManager.serverInstance.LeavePlayer(ModManager.serverInstance.clients[userId]);
            }

            if(users.ContainsKey(userId)) {
                users.Remove(userId);
            }
            if(userPeers.ContainsKey(userId)) {
                userPeers.Remove(userId);
            }
        }

        private void LobbyManager_OnMemberConnect(long lobbyId, long userId) {
            UpdateUserIds();

            userManager.GetUser(userId, (Result result, ref User user) => {
                if(result == Result.Ok) {
                    if(mode == Mode.SERVER) {
                        RegisterUser(user);
                    }
                }
            });
        }

        internal void RegisterUser(User user) {
            if(users.ContainsKey(user.Id)) users[user.Id] = user;
            else users.Add(user.Id, user);

            if(!userPeers.ContainsKey(user.Id)) {
                OpenChannels(user.Id, currentLobby.Id);
            }
        }


        private void NetworkManager_OnMessage(ulong peerId, byte channelId, byte[] data) {
            if(userPeers.ContainsValue(peerId)) {
                long userId = userPeers.First(x => x.Value == peerId).Key;

                LobbyManager_OnNetworkMessage(currentLobby.Id, userId, channelId, data);
            }
        }

        private void LobbyManager_OnNetworkMessage(long lobbyId, long userId, byte channelId, byte[] data) {
            //Log.Debug("LobbyManager_OnNetworkMessage");

            NetPacket packet = NetPacket.ReadPacket(data);
            
            if(mode == Mode.SERVER) {
                if(onPacketReceivedFromUser != null) {
                    User user = new User();

                    if(users.ContainsKey(userId)) user = users[userId];
                    else if(userId == currentUser.Id) user = currentUser;

                    if(user.Id > 0) {
                         onPacketReceivedFromUser.Invoke(user, packet);
                    }
                }
            } else {
                if(onPacketReceived != null) onPacketReceived.Invoke(packet);
            }

            if(channelId == RELIABLE_CHANNEL) Interlocked.Add(ref reliableReceive, data.Length);
            else if(channelId == UNRELIABLE_CHANNEL) Interlocked.Add(ref unreliableReceive, data.Length);
        }

        private void UpdateUserIds() {
            userIds = new long[lobbyManager.MemberCount(currentLobby.Id)];
            for(int i = 0; i < lobbyManager.MemberCount(currentLobby.Id); i++) {
                userIds[i] = lobbyManager.GetMemberUserId(currentLobby.Id, i);

                OpenChannels(userIds[i], currentLobby.Id);
            }

            UpdateActivity();
        }

        private byte[] PreparePacket(NetPacket packet) {
            //packet.WriteLength();
            return packet.GetData();
        }

        internal override void SendReliable(NetPacket packet) {
            SendReliable(packet, currentLobby.OwnerId);
        }

        internal void SendReliableToAll(NetPacket packet) {
            if(packet == null) return;
            
            foreach(int userId in userIds) {
                SendReliable(packet, userId);
            }
        }

        internal void SendReliable(NetPacket packet, long userId, bool fromServer = false) {
            if(packet == null) return;

            byte[] data = PreparePacket(packet);
            if(userId == currentUser.Id) {
                if(fromServer) {
                    ModManager.clientInstance.OnPacket(NetPacket.ReadPacket(data));
                } else {
                    LobbyManager_OnNetworkMessage(currentLobby.Id, userId, RELIABLE_CHANNEL, data);
                    //networkManager.SendMessage(userPeers[userId], RELIABLE_CHANNEL, data);
                }
            } else {
                networkManager.SendMessage(userPeers[userId], RELIABLE_CHANNEL, data);
            }
            Interlocked.Add(ref reliableSent, data.Length);
        }

        internal override void SendUnreliable(NetPacket packet) {
            SendUnreliable(packet, currentLobby.OwnerId);
        }

        internal void SendUnreliableToAll(NetPacket packet) {
            if(packet == null) return;

            foreach(int userId in userIds) {
                SendUnreliable(packet, userId);
            }
        }

        internal void SendUnreliable(NetPacket packet, long userId, bool fromServer = false) {
            if(packet == null) return;

            byte[] data = PreparePacket(packet);
            if(userId == currentUser.Id) {
                if(fromServer) {
                    ModManager.clientInstance.OnPacket(NetPacket.ReadPacket(data));
                } else {
                    LobbyManager_OnNetworkMessage(currentLobby.Id, userId, UNRELIABLE_CHANNEL, data);
                    //networkManager.SendMessage(userPeers[userId], UNRELIABLE_CHANNEL, data);
                    //lobbyManager.SendNetworkMessage(currentLobby.Id, userId, UNRELIABLE_CHANNEL, data);
                }
            } else {
                networkManager.SendMessage(userPeers[userId], UNRELIABLE_CHANNEL, data);
            }
            Interlocked.Add(ref unreliableSent, data.Length);
        }
    }
}
