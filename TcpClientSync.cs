using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net.Sockets;
using System.Text;


public class TcpClientSync : MonoBehaviour
{

    [SerializeField] private string serverIP = "127.0.0.1"; //192.168.0.0.1 
    [SerializeField] private int serverPort = 8888;

    [SerializeField] private InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text responseText;
    private TcpClient tcpClient;
    private NetworkStream stream;


    // Start is called before the first frame update
    void Start()
    {
        if (inputField == null || sendButton == null || responseText == null)
        {
            Debug.LogError("UI Error");
            return;
        }

        sendButton.onClick.AddListener(SendData);

        ConnectToServer();

    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            stream = tcpClient.GetStream();
            responseText.text = "Connect to server";
            Debug.Log("Connected to server");
            SendData();
            ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error :" + e.Message);
            responseText.text = "Connection Failed..";

        }
    }

    void SendData()
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Debug.LogError("Not connected to server.");
            responseText.text = "Not Connected";
            return;
        }


        if (inputField.text == null) inputField.text = "hi";

        string message = inputField.text;
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("Input is empty!");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log("sent:" + message);
            inputField.text = "";

        }
        catch (Exception e)
        {
            Debug.LogError("Send error :" + e.Message);
            responseText.text = "send error";

        }
    }

    void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        int byteRead;

        while (tcpClient != null && tcpClient.Connected)
        {
            try
            {
                byteRead = stream.Read(buffer, 0, buffer.Length);
                if (byteRead == 0)
                {
                    Debug.Log("server disconnected");
                    responseText.text = "serverIP disconnected";
                    break;
                }
                string receiveMessage = Encoding.UTF8.GetString(buffer, 0, byteRead);
                responseText.text = "server :" + receiveMessage;
                Debug.Log("received :" + receiveMessage);

            }
            catch (Exception e)
            {
                if (e is SocketException || e is ObjectDisposedException)
                {
                    Debug.Log("Server disconnected");
                }
                else
                {
                    Debug.LogError("Receive Error :" + e.Message);
                }
                responseText.text = "Receive error";
                break;
            }
        }
    }

    void onDestroy()
    {
        if (stream != null)
        {
            stream.Close();
        }

        if (tcpClient != null)
        {
            tcpClient.Close();
        }
    }



}