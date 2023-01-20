using AMP.Data;
using AMP.Logging;
using AMP.Network.Handler;
using AMP.Network.Packets;
using Steamworks;
using System.Drawing;

namespace AMP.SteamNet {
    internal class SteamNetHandler : NetworkHandler {

        public struct LobbyMetaData {
            public string m_Key;
            public string m_Value;
        }

        public struct LobbyMembers {
            public CSteamID m_SteamID;
            public LobbyMetaData[] m_Data;
        }

        public struct Lobby {
            public CSteamID m_SteamID;
            public CSteamID m_Owner;
            public LobbyMembers[] m_Members;
            public int m_MemberLimit;
            public LobbyMetaData[] m_Data;
        }

        public Lobby m_CurrentLobby;

        // Various callback functions that Steam will call to let us know about whether we should
        // allow clients to play or we should kick/deny them.
        //
        // Tells us a client has been authenticated and approved to play by Steam (passes auth, license check, VAC status, etc...)
        protected Callback<ValidateAuthTicketResponse_t> m_CallbackGSAuthTicketResponse;

        // client connection state
        protected Callback<P2PSessionRequest_t> m_CallbackP2PSessionRequest;
        protected Callback<P2PSessionConnectFail_t> m_CallbackP2PSessionConnectFail;

        protected Callback<LobbyCreated_t> m_CallbackLobbyCreated;

        private const int RELIABLE_CHANNEL = 0;
        private const int UNRELIABLE_CHANNEL = 1;

        private SteamSocket reliableSocket;
        private SteamSocket unreliableSocket;

        public SteamNetHandler() {
            SteamNetworking.AllowP2PPacketRelay(true);

            RegisterCallbacks();
        }

        internal override void Connect(string password = "") {
            
        }

        public void CreateLobby(uint maxClients) {
            Log.Debug(Defines.STEAM_API, "CreateLobby");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, (int) maxClients);
        }

        public void RegisterCallbacks() {
            m_CallbackGSAuthTicketResponse  = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicketResponse);
            m_CallbackP2PSessionRequest     = Callback<P2PSessionRequest_t>.         Create(OnP2PSessionRequest);
            m_CallbackP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.     Create(OnP2PSessionConnectFail);
            m_CallbackLobbyCreated          = Callback<LobbyCreated_t>.              Create(OnLobbyCreated);

        }


        void OnLobbyCreated(LobbyCreated_t pCallback) {
            if(pCallback.m_eResult != EResult.k_EResultOK) {
                Log.Err(Defines.STEAM_API, "OnLobbyCreated encountered an Failure");
                return;
            }

            Log.Debug("[" + LobbyCreated_t.k_iCallback + " - LobbyCreated] - " + pCallback.m_eResult + " -- " + pCallback.m_ulSteamIDLobby);

            UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref m_CurrentLobby);
        }

        void UpdateLobbyInfo(CSteamID steamIDLobby, ref Lobby outLobby) {
            outLobby.m_SteamID = steamIDLobby;
            outLobby.m_Owner = SteamMatchmaking.GetLobbyOwner(steamIDLobby);
            outLobby.m_Members = new LobbyMembers[SteamMatchmaking.GetNumLobbyMembers(steamIDLobby)];
            outLobby.m_MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(steamIDLobby);

            for(int i = 0; i < outLobby.m_Members.Length; i++) {
                outLobby.m_Members[i].m_SteamID = SteamMatchmaking.GetLobbyMemberByIndex(steamIDLobby, i);
                Log.Debug(Defines.STEAM_API, outLobby.m_Members[i].m_SteamID);
            }

            int nDataCount = SteamMatchmaking.GetLobbyDataCount(steamIDLobby);
            outLobby.m_Data = new LobbyMetaData[nDataCount];
            for(int i = 0; i < nDataCount; ++i) {
                bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(steamIDLobby, i, out outLobby.m_Data[i].m_Key, Constants.k_nMaxLobbyKeyLength, out outLobby.m_Data[i].m_Value, Constants.k_cubChatMetadataMax);
                if(!lobbyDataRet) {
                    Log.Err(Defines.STEAM_API, "SteamMatchmaking.GetLobbyDataByIndex returned false.");
                    continue;
                }
            }

            reliableSocket   = new SteamSocket(outLobby.m_Owner, EP2PSend.k_EP2PSendReliable,            RELIABLE_CHANNEL);
            unreliableSocket = new SteamSocket(outLobby.m_Owner, EP2PSend.k_EP2PSendUnreliableNoDelay, UNRELIABLE_CHANNEL);
        }

        void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pResponse) {
            Log.Debug(Defines.STEAM_API, "OnValidateAuthTicketResponse Called steamID: " + pResponse.m_SteamID);

            if(pResponse.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK) {
                
            } else {
                
            }
        }

        void OnP2PSessionRequest(P2PSessionRequest_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSesssionRequest Called steamIDRemote: " + pCallback.m_steamIDRemote);

            // we'll accept a connection from anyone
            SteamGameServerNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
        }

        void OnP2PSessionConnectFail(P2PSessionConnectFail_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSessionConnectFail Called steamIDRemote: " + pCallback.m_steamIDRemote);
        }

        internal override void SendReliable(NetPacket packet) {
            reliableSocket?.SendPacket(packet);
        }

        internal override void SendUnreliable(NetPacket packet) {
            unreliableSocket?.SendPacket(packet);
        }
    }
}
