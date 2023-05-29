using LightReflectiveMirror.Endpoints;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace LightReflectiveMirror
{
    class LoginResponse
    {
        public int Response;
        public string Message;
        public string Username;
        public int GroupID;
        public bool IsModerator;
    }

    public partial class RelayHandler
    {
        static bool TryParseJson<T>(string input, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(input, settings);
            return success;
        }

        /// <summary>
        /// Invoked when a client connects to this LRM server.
        /// </summary>
        /// <param name="clientId">The ID of the client who connected.</param>
        public void ClientConnected(int clientId)
        {
            _pendingAuthentication.Add(clientId);
            var buffer = _sendBuffers.Rent(1);
            int pos = 0;
            buffer.WriteByte(ref pos, (byte)OpCodes.AuthenticationRequest);
            Program.transport.ServerSend(clientId, 0, new ArraySegment<byte>(buffer, 0, pos));
            _sendBuffers.Return(buffer);
        }

        /// <summary>
        /// Handles the processing of data from a client.
        /// </summary>
        /// <param name="clientId">The client who sent the data</param>
        /// <param name="segmentData">The binary data</param>
        /// <param name="channel">The channel the client sent the data on</param>
        string[] authenticationData;
        bool useLoginAuthentication = false;
        public void HandleMessage(int clientId, ArraySegment<byte> segmentData, int channel)
        {
            try
            {
                var data = segmentData.Array;
                int pos = segmentData.Offset;
                OpCodes opcode = (OpCodes)data.ReadByte(ref pos);

                if (_pendingAuthentication.Contains(clientId))
                {

                    if (opcode == OpCodes.AuthenticationResponse)
                    {
                        authenticationData = JsonConvert.DeserializeObject<string[]>(data.ReadString(ref pos));
                        string authResponse = authenticationData[0];
                        if (authResponse == Program.conf.AuthenticationKey)
                        {
                            useLoginAuthentication = (Program.conf.AccountAuthenticationUrl.Length > 1);
                            if (useLoginAuthentication) {
                               Task.Run(() => AttemptLoginAuthentication(authenticationData[1], authenticationData[2], clientId));
                            }
                            else
                            {
                                _pendingAuthentication.Remove(clientId);
                                int writePos = 0;
                                var sendBuffer = _sendBuffers.Rent(2);
                                sendBuffer.WriteByte(ref writePos, (byte)OpCodes.Authenticated);
                                sendBuffer.WriteInt(ref writePos, clientId);
                                Program.transport.ServerSend(clientId, 0, new ArraySegment<byte>(sendBuffer, 0, writePos));

                                _sendBuffers.Return(sendBuffer);
                            }
                            
                        }
                        else
                        {
                            Program.WriteLogMessage($"Client {clientId} sent wrong auth key! Removing from LRM node.");
                            Program.transport.ServerDisconnect(clientId);
                        }
                    }
                    return;
                }

                switch (opcode)
                {
                    case OpCodes.CreateRoom: // bruh
                        CreateRoom(clientId, data.ReadInt   (ref pos), 
                                             data.ReadString(ref pos), 
                                             data.ReadBool  (ref pos), 
                                             data.ReadString(ref pos),
                                             data.ReadString(ref pos),
                                             data.ReadBool  (ref pos), 
                                             data.ReadString(ref pos), 
                                             data.ReadBool  (ref pos), 
                                             data.ReadInt   (ref pos));
                        break;
                    case OpCodes.RequestID:
                        SendClientID(clientId);
                        break;
                    case OpCodes.LeaveRoom:
                        LeaveRoom(clientId);
                        break;
                    case OpCodes.JoinServer:
                        JoinRoom(clientId, data.ReadString(ref pos), data.ReadBool(ref pos), data.ReadString(ref pos));
                        break;
                    case OpCodes.KickPlayer:
                        LeaveRoom(data.ReadInt(ref pos), clientId);
                        break;
                    case OpCodes.SendData:
                        ProcessData(clientId, data.ReadBytes(ref pos), channel, data.ReadInt(ref pos));
                        break;
                    case OpCodes.UpdateRoomData:
                        var plyRoom = _cachedClientRooms[clientId];

                        if (plyRoom == null || plyRoom.hostId != clientId)
                            return;

                        if (data.ReadBool(ref pos))
                            plyRoom.serverName = data.ReadString(ref pos);

                        if (data.ReadBool(ref pos))
                            plyRoom.serverData = data.ReadString(ref pos);

                        if (data.ReadBool(ref pos))
                            plyRoom.isPublic = data.ReadBool(ref pos);

                        if (data.ReadBool(ref pos))
                            plyRoom.maxPlayers = data.ReadInt(ref pos);

                        Endpoint.RoomsModified();
                        break;
                    case OpCodes.RequestPlayerInfo:
                        int playerClientID = data.ReadInt(ref pos);
                        System.Collections.Generic.List<Program.ClientData> playerList = Program.instance._currentConnections;
                        for (int i = 0; i < playerList.Count; i++)
                        {
                            if(playerList[i].clientID == playerClientID)
                            {
                                SendPlayerAccountInfo(clientId, playerClientID, playerList[i].accountName, playerList[i].groupID, playerList[i].isModerator);
                                i = playerList.Count;
                            }
                        }
                        break;
                }
            }
            catch
            {
                // sent invalid data, boot them hehe
                Program.WriteLogMessage($"Client {clientId} sent bad data! Removing from LRM node.");
                Program.transport.ServerDisconnect(clientId);
            }
        }
        void AttemptLoginAuthentication(string username, string password, int clientId)
        {
            bool result = false;
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["user"] = username;
                data["password"] = password;

                string url = Program.conf.AccountAuthenticationUrl;
                var response = wb.UploadValues(url, "POST", data);
                string responseInString = Encoding.UTF8.GetString(response);
                result = (responseInString.Contains("Response") && responseInString.Contains("Username") && responseInString.Contains("GroupID"));

                if (result)
                {
                    LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseInString);
                    if (loginResponse.Response == 2)
                    {
                        System.Collections.Generic.List<Program.ClientData> playerList = Program.instance._currentConnections;
                        for (int i = 0; i < playerList.Count; i++)
                        {
                            if (playerList[i].clientID == clientId)
                            {
                                playerList[i].accountName = loginResponse.Username;
                                playerList[i].groupID = loginResponse.GroupID;
                                playerList[i].isModerator = loginResponse.IsModerator;
                            }
                        }

                        _pendingAuthentication.Remove(clientId);
                        int writePos = 0;
                        var sendBuffer = _sendBuffers.Rent(2);
                        sendBuffer.WriteByte(ref writePos, (byte)OpCodes.Authenticated);
                        sendBuffer.WriteInt(ref writePos, clientId);
                        Program.transport.ServerSend(clientId, 0, new ArraySegment<byte>(sendBuffer, 0, writePos));
                        Program.WriteLogMessage($"Client {clientId} signed in as '"+username+"'.");

                        _sendBuffers.Return(sendBuffer);
                    }
                    else
                    {
                        Program.WriteLogMessage($"Client {clientId} entered wrong login credentials! Removing from LRM node.");
                        Program.transport.ServerDisconnect(clientId);
                    }
                }
                else
                {
                    Program.WriteLogMessage($"Client {clientId} could not access login URL! Removing from LRM node.");
                    Program.transport.ServerDisconnect(clientId);
                }
            }
        }

        /// <summary>
        /// Invoked when a client disconnects from the relay.
        /// </summary>
        /// <param name="clientId">The ID of the client who disconnected</param>
        public void HandleDisconnect(int clientId) => LeaveRoom(clientId);
    }
}
