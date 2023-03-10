using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class SampleNeworkManager : NetworkManager
{

    public LobbySystem lobbySystem;

    public override void OnClientConnect()
    {
        lobbySystem.lobbyPanel.gameObject.SetActive(false);
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        lobbySystem.lobbyPanel.gameObject.SetActive(true);
        lobbySystem.RefreshServerList();
        base.OnClientDisconnect();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if(lobbySystem.LRMTransport.ClientConnected())
        lobbySystem.LRMTransport.DisconnectFromRelay();
        Application.LoadLevel("LobbySample");
        Destroy(this.gameObject);
    }
}
