using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Websocket;
using System.Collections.Generic;

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
    private Button joinCardButton;

    [SerializeField]
    private Button hostCardButton;

    [SerializeField]
    private bool isAttemptingToConnectToServer;

    private void Start()
    {
        manager = FindObjectOfType<NetworkManager>();
#if !UNITY_STANDALONE && !UNITY_EDITOR
        DisableHostCardButton();
#endif
    }

    // Attached to an Button OnClick event handler
    public void BackButton()
    {
        //menuPanel.SetActive(true);
        joinCardButton.interactable = true;
        joinCardButton.GetComponent<CardMenu>().enabled = true;
        hostPanel.SetActive(false);
        joinPanel.transform.Find("ErrorText")?.gameObject.SetActive(false);
        joinPanel.SetActive(false);

#if UNITY_STANDALONE || UNITY_EDITOR
        hostCardButton.interactable = true;
        hostCardButton.GetComponent<CardMenu>().enabled = true;
#endif
    }

    public void JoinButton()
    {
        //menuPanel.SetActive(false);
        joinCardButton.interactable = false;
        joinCardButton.GetComponent<CardMenu>().enabled = false;
        joinPanel.SetActive(true);

#if UNITY_STANDALONE || UNITY_EDITOR
        hostCardButton.interactable = false;
        hostCardButton.GetComponent<CardMenu>().enabled = false;
#endif
    }

    public void CancelButton()
    {
        isAttemptingToConnectToServer = false;
        joinPanel.SetActive(true);
        joinPanel.transform.Find("ErrorText")?.gameObject.SetActive(false);
        joinInProgressPanel.SetActive(false);
        manager.StopClient();
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
        SetClientPort(port);

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

// Standalone Builds are only allowed to create games to host
#if UNITY_STANDALONE || UNITY_EDITOR
    public void HostButton()
    {
        //menuPanel.SetActive(false);
        joinCardButton.interactable = false;
        hostCardButton.interactable = false;
        joinCardButton.GetComponent<CardMenu>().enabled = false;
        hostCardButton.GetComponent<CardMenu>().enabled = false;
        hostPanel.SetActive(true);
    }

    public void StopServer()
    {
        Debug.Log("Stopping server....." + manager.networkAddress);
        manager.StopHost();
    }

    public void StartServer()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.Log("Server already started... disconnect first to start server again");
            return;
        }

        // TODO: remove this input field since multiplex transport is being used
        string port;
        var inputField = hostPanel.transform.Find("InputField").GetComponent<InputField>();
        if (inputField.text == null || inputField.text.Length == 0)
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

        // Read from server configs
        var pm = manager as ProjectAceNetworkManager;
        if(pm != null)
        {
            pm.InitServerPorts();
        }

        manager.StartHost();
    }
#endif

#if !UNITY_STANDALONE && !UNITY_EDITOR
    private void DisableHostCardButton()
    {
        if(hostCardButton != null)
        {
            hostCardButton.GetComponent<CardMenu>().enabled = false;
            hostCardButton.GetComponent<Button>().enabled = false;
            hostCardButton.transform.Find("Text")?.gameObject.SetActive(false);
        }
    }
#endif

    private void SetClientPort(string port)
    {
#if UNITY_STANDALONE
        var tcpTransport = manager?.GetComponent<TelepathyTransport>();
        ushort.TryParse(port, out tcpTransport.port);
#endif

#if UNITY_WEBGL
        var websocketTransport = manager?.GetComponent<WebsocketTransport>();
        int.TryParse(port, out websocketTransport.port);
#endif
    }
}
