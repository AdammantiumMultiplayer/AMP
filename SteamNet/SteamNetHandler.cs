using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Handler;
using AMP.Network.Packets;
using Steamworks;
using System;

namespace AMP.SteamNet {
    internal class SteamNetHandler : NetworkHandler {

        internal override string TYPE => "STEAM";

        public struct LobbyMetaData {
            public string key;
            public string value;
        }

        public struct LobbyMembers {
            public CSteamID steamId;
            public LobbyMetaData[] data;
        }

        public struct Lobby {
            public CSteamID lobbySteamId;
            public CSteamID ownerSteamId;
            public LobbyMembers[] members;
            public int memberLimit;
            public LobbyMetaData[] data;
        }

        public Lobby currentLobby;

        public bool IsHost {
            get { 
                return currentLobby.ownerSteamId == SteamIntegration.Instance.mySteamId;
            }
        }

        public Action joinCallback;

        // Various callback functions that Steam will call to let us know about whether we should
        // allow clients to play or we should kick/deny them.
        //
        // Tells us a client has been authenticated and approved to play by Steam (passes auth, license check, VAC status, etc...)
        protected Callback<ValidateAuthTicketResponse_t> callbackGSAuthTicketResponse;

        // client connection state
        protected Callback<P2PSessionRequest_t> callbackP2PSessionRequest;
        protected Callback<P2PSessionConnectFail_t> callbackP2PSessionConnectFail;

        protected Callback<LobbyCreated_t> callbackLobbyCreated;
        protected Callback<LobbyEnter_t> callbackLobbyEnter;
        protected Callback<LobbyChatUpdate_t> callbackLobbyChatUpdate;

        private SteamSocket reliableSocket;
        private SteamSocket unreliableSocket;

        public SteamNetHandler() {
            SteamNetworking.AllowP2PPacketRelay(true);

            RegisterCallbacks();
        }

        internal override void Disconnect() {
            LeaveLobby();
            reliableSocket?.Disconnect();
            unreliableSocket?.Disconnect();
            UnregisterCallbacks();
        }

        internal override void Connect(string password = "") {
            if(IsHost) {
                UpdateLobbyInfo(currentLobby.lobbySteamId, ref currentLobby);
            } else {
                Log.Info(Defines.CLIENT, $"Waiting for Steam Host to send any data...");
            }
        }

        public void CreateLobby(uint maxClients) {
            LeaveLobby();

            Log.Debug(Defines.STEAM_API, $"Creating { (ModManager.guiManager.host_steam_friends_only ? "friend only" : "public") } Lobby...");

            isConnected = false;
            currentLobby = default(Lobby);

            ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
            if(!ModManager.guiManager.host_steam_friends_only) { // Not the perfect way, as it is hard coded for the UI Component, but its easier than to rewrite the whole system
                lobbyType = ELobbyType.k_ELobbyTypePublic;
            }
            SteamMatchmaking.CreateLobby(lobbyType, (int) maxClients);

            joinCallback = () => { };
        }


        internal override string GetJoinSecret() {
            if((ulong) currentLobby.lobbySteamId > 0) {
                return TYPE + ":" + currentLobby.lobbySteamId;
            }
            return "";
        }

        public void RegisterCallbacks() {
            callbackGSAuthTicketResponse  = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicketResponse);
            callbackP2PSessionRequest     = Callback<P2PSessionRequest_t>.         Create(OnP2PSessionRequest);
            callbackP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.     Create(OnP2PSessionConnectFail);
            callbackLobbyCreated          = Callback<LobbyCreated_t>.              Create(OnLobbyCreated);
            callbackLobbyEnter            = Callback<LobbyEnter_t>.                Create(OnLobbyEnter);
            callbackLobbyChatUpdate       = Callback<LobbyChatUpdate_t>.           Create(OnLobbyChatUpdate);
        }

        public void UnregisterCallbacks() {
            callbackGSAuthTicketResponse.Dispose();
            callbackP2PSessionRequest.Dispose();
            callbackP2PSessionConnectFail.Dispose();
            callbackLobbyCreated.Dispose();
            callbackLobbyEnter.Dispose();
            callbackLobbyChatUpdate.Dispose();
        }

        internal void JoinLobby(CSteamID m_steamIDLobby) {
            isConnected = false;
            SteamMatchmaking.JoinLobby(m_steamIDLobby);
            
            joinCallback = () => {
                ModManager.JoinServer(this);
            };
        }

        internal void LeaveLobby() {
            if(isConnected) {
                if((ulong) currentLobby.lobbySteamId > 0) {
                    Log.Debug($"Requested Steam to leave lobby {currentLobby.lobbySteamId}.");
                    SteamMatchmaking.LeaveLobby(currentLobby.lobbySteamId);
                }
            }
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback) {
            if( pCallback.m_ulSteamIDUserChanged == (ulong) SteamIntegration.Instance.mySteamId &&
                (  pCallback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeLeft
                || pCallback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeDisconnected)
                ) {
                Log.Debug(Defines.STEAM_API, $"Lobby left: { pCallback.m_ulSteamIDLobby }");
                
                isConnected  = false;
                currentLobby = default(Lobby);
            } else {
                Log.Debug(Defines.STEAM_API, $"Lobby changed: { pCallback.m_ulSteamIDLobby }");
                UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
            }
        }

        private void OnLobbyEnter(LobbyEnter_t pCallback) {
            if(pCallback.m_ulSteamIDLobby > 0) {
                Log.Debug(Defines.STEAM_API, $"Lobby joined: { pCallback.m_ulSteamIDLobby }");
                UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
                isConnected = true;

                joinCallback?.Invoke();
            }
        }

        void OnLobbyCreated(LobbyCreated_t pCallback) {
            if(pCallback.m_eResult != EResult.k_EResultOK) {
                Log.Err(Defines.STEAM_API, "OnLobbyCreated encountered an Failure");
                return;
            }

            Log.Debug(Defines.STEAM_API, $"Lobby created: { pCallback.m_ulSteamIDLobby }");

            UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
        }

        public void UpdateLobbyInfo(CSteamID steamIDLobby, ref Lobby outLobby) {
            outLobby.lobbySteamId = steamIDLobby;
            outLobby.ownerSteamId = SteamMatchmaking.GetLobbyOwner(steamIDLobby);
            outLobby.members      = new LobbyMembers[SteamMatchmaking.GetNumLobbyMembers(steamIDLobby)];
            outLobby.memberLimit  = SteamMatchmaking.GetLobbyMemberLimit(steamIDLobby);

            for(int i = 0; i < outLobby.members.Length; i++) {
                outLobby.members[i].steamId = SteamMatchmaking.GetLobbyMemberByIndex(steamIDLobby, i);
                if(IsHost) {
                    if(ModManager.serverInstance != null && !ModManager.serverInstance.isRunning) continue;

                    long playerId = (long)(ulong)outLobby.members[i].steamId;
                    if(!ModManager.serverInstance.clients.ContainsKey(playerId)) {
                        SteamSocket reliableSocket = new SteamSocket(outLobby.members[i].steamId, EP2PSend.k_EP2PSendReliable, Defines.STEAM_RELIABLE_CHANNEL);
                        // Socket is only for sending, Steam is not differing on packet read between different users, but sends the user id with each packet
                        reliableSocket.StopAwaitData();

                        ModManager.serverInstance.EstablishConnection(playerId, playerId + "", reliableSocket);

                        if(ModManager.serverInstance.clients.ContainsKey(playerId)) {
                            SteamSocket unreliableSocket = new SteamSocket(outLobby.members[i].steamId, EP2PSend.k_EP2PSendUnreliableNoDelay, Defines.STEAM_RELIABLE_CHANNEL);
                            // Socket is only for sending, Steam is not differing on packet read between different users, but sends the user id with each packet
                            unreliableSocket.StopAwaitData();
                            unreliableSocket.onPacket += (packet) => ModManager.serverInstance.OnPacket(ModManager.serverInstance.clients[playerId], packet);
                            ModManager.serverInstance.clients[playerId].unreliable = unreliableSocket;
                        }
                    }
                }
            }


            if(IsHost) {
                foreach(ClientData cd in ModManager.serverInstance.clients.Values) {
                    bool found = false;
                    for(int i = 0; i < outLobby.members.Length; i++) {
                        if(cd.playerId == (long) (ulong) outLobby.members[i].steamId) {
                            found = true;
                            break;
                        }
                    }
                    if(!found) {
                        // Player left
                        ModManager.serverInstance.LeavePlayer(cd, reason: "Player left lobby");
                    }
                }
            }

            int nDataCount = SteamMatchmaking.GetLobbyDataCount(steamIDLobby);
            outLobby.data = new LobbyMetaData[nDataCount];
            for(int i = 0; i < nDataCount; ++i) {
                bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(steamIDLobby, i, out outLobby.data[i].key, Constants.k_nMaxLobbyKeyLength, out outLobby.data[i].value, Constants.k_cubChatMetadataMax);
                if(!lobbyDataRet) {
                    Log.Err(Defines.STEAM_API, "SteamMatchmaking.GetLobbyDataByIndex returned false.");
                    continue;
                }
            }

            if(reliableSocket   == null) {
                reliableSocket   = new SteamSocket(outLobby.ownerSteamId, EP2PSend.k_EP2PSendReliable,          Defines.STEAM_RELIABLE_CHANNEL  );
                reliableSocket.onPacketWithId   += HandlePacket;
            }
            if(unreliableSocket == null) {
                unreliableSocket = new SteamSocket(outLobby.ownerSteamId, EP2PSend.k_EP2PSendUnreliableNoDelay, Defines.STEAM_UNRELIABLE_CHANNEL);
                unreliableSocket.onPacketWithId += HandlePacket;
            }
        }

        private void HandlePacket(ulong playerId, NetPacket p) {
            long clientId = (long) playerId;
            if(IsHost) {
                if(ModManager.serverInstance.clients.ContainsKey(clientId)) {
                    ModManager.serverInstance?.OnPacket(ModManager.serverInstance.clients[clientId], p);
                }
            } else {
                ModManager.clientInstance?.OnPacket(p);
            }
        }

        void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pResponse) {
            Log.Debug(Defines.STEAM_API, "OnValidateAuthTicketResponse Called steamID: " + pResponse.m_SteamID);

            if(pResponse.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK) {
                
            } else {
                
            }
        }

        void OnP2PSessionRequest(P2PSessionRequest_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSesssionRequest Called steamIDRemote: " + pCallback.m_steamIDRemote);

            // Check if the user trying to connect is part of the lobby (when user is host)
            // or the host (when user is client)
            bool allow = false;
            if(IsHost) { // User is the host
                foreach(LobbyMembers member in currentLobby.members) { // Check if the person trying to connect is a member of the lobby
                    if(pCallback.m_steamIDRemote == member.steamId) {
                        allow = true;
                        break;
                    }
                }
            } else { // User is a client,
                if(pCallback.m_steamIDRemote == currentLobby.ownerSteamId) { // Check if the person trying to connect is the host
                    allow = true;
                }
            }

            if(allow) {
                Log.Debug(Defines.STEAM_API, "Connection allowed from SteamId " + pCallback.m_steamIDRemote);
                SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
            } else {
                Log.Warn(Defines.STEAM_API, "Connection denied from unknown SteamId " + pCallback.m_steamIDRemote);
            }
        }

        void OnP2PSessionConnectFail(P2PSessionConnectFail_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSessionConnectFail Called steamIDRemote: " + pCallback.m_steamIDRemote);
        }

        internal override void SendReliable(NetPacket packet) {
            if(IsHost) {
                if(ModManager.serverInstance.clients.ContainsKey(ModManager.clientInstance.myPlayerId))
                    ModManager.serverInstance.clients[ModManager.clientInstance.myPlayerId].reliable?.onPacket.Invoke(packet);
            } else {
                reliableSocket?.QueuePacket(packet);
            }
        }

        internal override void SendUnreliable(NetPacket packet) {
            if(IsHost) {
                if(ModManager.serverInstance.clients.ContainsKey(ModManager.clientInstance.myPlayerId))
                    ModManager.serverInstance.clients[ModManager.clientInstance.myPlayerId].unreliable?.onPacket.Invoke(packet);
            } else {
                unreliableSocket?.QueuePacket(packet);
            }
        }
    }
}
