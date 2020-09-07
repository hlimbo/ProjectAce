using System;
using UnityEngine;

[Serializable]
public class ServerConfigs
{
    public ushort tcpPort;
    public int websocketPort;
    public int startingCardCountPerPlayer;
    public int initialTimeLeftPerPlayer;

    public static ServerConfigs GenerateConfigs()
    {
        string serverConfigsPath = $"{Application.streamingAssetsPath}/serverConfigs.json";
        string jsonString = System.IO.File.ReadAllText(serverConfigsPath);
        return JsonUtility.FromJson<ServerConfigs>(jsonString);
    }
}
