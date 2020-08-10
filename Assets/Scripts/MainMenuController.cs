using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject menuPanel;
    [SerializeField]
    private GameObject hostPanel;
    [SerializeField]
    private GameObject joinPanel;
    [SerializeField]
    private GameObject joinInProgressPanel;

    [SerializeField]
    private NetworkManager manager;
    [SerializeField]
    private TelepathyTransport tcpTransport;

    [SerializeField]
    private bool isAttemptingToConnectToServer;

    private void Start()
    {
        manager = FindObjectOfType<NetworkManager>();
        tcpTransport = FindObjectOfType<TelepathyTransport>();
    }

    // Attached to an Button OnClick event handler
    public void BackButton()
    {
        menuPanel.SetActive(true);
        hostPanel.SetActive(false);
        joinPanel.transform.Find("ErrorText")?.gameObject.SetActive(false);
        joinPanel.SetActive(false);
    }


    public void HostButton()
    {
        menuPanel.SetActive(false);
        hostPanel.SetActive(true);
    }

    public void JoinButton()
    {
        menuPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    public void StopServer()
    {
        Debug.Log("Stopping server....." + manager.networkAddress);
        manager.StopHost();
    }

    public void CancelButton()
    {
        isAttemptingToConnectToServer = false;
        joinPanel.SetActive(true);
        joinPanel.transform.Find("ErrorText")?.gameObject.SetActive(false);
        joinInProgressPanel.SetActive(false);
        manager.StopClient();
    }

    public void StartServer()
    {
        if(NetworkServer.active || NetworkClient.active)
        {
            Debug.Log("Server already started... disconnect first to start server again");
            return;
        }

        string port;
        var inputField = hostPanel.transform.Find("InputField").GetComponent<InputField>();
        if(inputField.text == null || inputField.text.Length == 0)
        {
            string placeholderText = inputField.transform.Find("Placeholder").GetComponent<Text>().text;
            Debug.Log("placeholder port: " + placeholderText);
            port = placeholderText;
        }
        else
        {
            Debug.Log("port inputted: " + inputField.text);
            port = inputField.text;
        }

        ushort.TryParse(port, out tcpTransport.port);

        manager.StartHost();
    }

    // Attempt to Join Server
    public void JoinServer()
    {
        var ipAddressField = joinPanel.transform.Find("IpAddressField").Find("InputField").GetComponent<InputField>();
        var portNumberField = joinPanel.transform.Find("PortField").Find("InputField").GetComponent<InputField>();

        string ipAddress;
        string port;
        if(ipAddressField.text == null || ipAddressField.text.Length == 0)
        {
            string placeholderText = ipAddressField.transform.Find("Placeholder").GetComponent<Text>().text;
            Debug.Log("Placeholder ip: " + placeholderText);
            ipAddress = placeholderText;
        }
        else
        {
            Debug.Log("ip address: " + ipAddressField.text);
            ipAddress = ipAddressField.text;
        }

        if (portNumberField.text == null || portNumberField.text.Length == 0)
        {
            string placeholderText = portNumberField.transform.Find("Placeholder").GetComponent<Text>().text;
            Debug.Log("Placeholder port: " + placeholderText);
            port = placeholderText;
        }
        else
        {
            Debug.Log("port number: " + portNumberField.text);
            port = portNumberField.text;
        }

        manager.networkAddress = ipAddress;
        ushort.TryParse(port, out tcpTransport.port);

        manager.StartClient();

        joinPanel.SetActive(false);
        joinInProgressPanel.SetActive(true);
        isAttemptingToConnectToServer = true;
    }

    // Unfortunate Hack since Mirror has no way of intercepting a server connection that fails due to server being full
    private void Update()
    {
        if(isAttemptingToConnectToServer)
        {
            // check if client stops attempting to connect (connection attempt to server times-out)
            isAttemptingToConnectToServer = NetworkClient.active;
            if(!NetworkClient.active)
            {
                joinPanel.SetActive(true);
                joinPanel.transform.Find("ErrorText")?.gameObject.SetActive(true);
                joinInProgressPanel.SetActive(false);
            }
        }
    }
}
