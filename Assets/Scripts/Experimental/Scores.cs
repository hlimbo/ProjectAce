using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;

public class Scores : MonoBehaviour
{
    public class ScoreMessage : MessageBase
    {
        public int score;
        public int lives;
        public bool flag;
    }

    // Wrapper method that allows us to send score updates over the network
    public void SendScore(int score, int lives, bool flag)
    {
        ScoreMessage msg = new ScoreMessage()
        {
            score = score,
            lives = lives,
            flag = flag
        };

        // Sends to all connected clients
        NetworkServer.SendToAll(msg);
    }

    public void SendFixedScore()
    {
        SendScore(5, 3, true);
    }

    // Can a handler be registered on client connection? yes
    public void OnScore(NetworkConnection conn, ScoreMessage msg)
    {
        Debug.Log("Connection Id: " + conn.connectionId);
        Debug.Log("OnScore: " + msg.score);
    }


}
