using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace LightReflectiveMirror
{
    public class PlayerAccountInfo : NetworkBehaviour
    {
        public string playerName;
        public int groupID;
        public bool isModerator;
        [SyncVar(hook = nameof(SetPlayerData))]
        public int playerClientID;
        // Start is called before the first frame update
        void Start()
        {
            if (isServer)
            {
                LightReflectiveMirror.LightReflectiveMirrorTransport LRMTransport = FindObjectOfType<LightReflectiveMirror.LightReflectiveMirrorTransport>();
                if (netIdentity.connectionToClient.connectionId == 0)
                {
                    playerClientID = LRMTransport.clientID;
                }
                else
                {
                    playerClientID = LRMTransport._connectedRelayClients.GetBySecond(netIdentity.connectionToClient.connectionId);
                }
            }
        }

        public void SetPlayerInfoValues(string newPlayerName, int newGroupID, bool newIsModerator)
        {
            playerName = newPlayerName;
            groupID = newGroupID;
            isModerator = newIsModerator;
        }

        public void SetPlayerData(int oldClientID, int clientID)
        {
            if (playerName.Length > 0 || oldClientID != 0) return;
            LightReflectiveMirror.LightReflectiveMirrorTransport LRMTransport = FindObjectOfType<LightReflectiveMirror.LightReflectiveMirrorTransport>();
            LRMTransport.RequestPlayerInfo(clientID);
        }

    }
}
