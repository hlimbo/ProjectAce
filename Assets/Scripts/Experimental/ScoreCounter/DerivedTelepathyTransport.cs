using UnityEngine;
using Mirror;
using System.Threading;

// Used to notify server client can't connect since room is full....
public class DerivedTelepathyTransport : TelepathyTransport
{
    public override bool ServerDisconnect(int connectionId)
    {
        Debug.LogFormat("Kicking out client {0}..... Server full with {1} players connected", connectionId, NetworkServer.connections.Count);
        var clientMessage = new ScoreCounterNetworkManager.FullServerMessage();
        clientMessage.maxConnectionCount = NetworkServer.connections.Count;
        // Can't send message to clients as the client will be kicked anyways :(
        // Unfortunate that Mirror doesn't have a way to send a message to 1 client only :( --> something that can possibly improved on the next release of Mirror if I decide to contribute
        NetworkServer.SendToAll(clientMessage);

        //return base.ServerDisconnect(connectionId);
        return true;
    }
}
