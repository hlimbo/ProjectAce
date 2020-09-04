using UnityEngine;

[System.Serializable]
public class ServerConfigs
{
    public ushort tcpPort;
    public int websocketPort;
    public int startingCardCountPerPlayer;
    public int initialTimeLeftPerPlayer;

    public static ServerConfigs GenerateConfigs()
    {
        string serverConfigsPath = $"{Application.dataPath}/StreamingAssets/serverConfigs.json";
        string jsonString = System.IO.File.ReadAllText(serverConfigsPath);
        Debug.Log(jsonString);
        return JsonUtility.FromJson<ServerConfigs>(jsonString);
    }
}
