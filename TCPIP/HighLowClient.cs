using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class HighLowClient : MonoBehaviour
{
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 5000;
    [SerializeField] private InputField inputField;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private Button sendButton;
    private TcpClient client;
    private NetworkStream stream;
    
    private async void Start()
    {
        if (inputField == null || sendButton == null || responseText == null)
        {
            Debug.LogError("Error: UI elements are not assigned!");
            return;
        }
        sendButton.onClick.AddListener(SendGuess);
        await ConnectToServer(); //서버에 연결 (비동기)
    }

    private async Task ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort); //비동기 연결
            stream = client.GetStream();
            responseText.text = "Connected to server!";
            Debug.Log("Connected to server!");
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: "+e.Message);
            responseText.text = "Connection failed.";
        }
    }

    public async void SendGuess()
    {
        if (client == null || !client.Connected)
        {
            Debug.LogWarning("Client not connected!");
            responseText.text = "Client not connected!";
            return;
        }
        
        string guessText = inputField.text;
        if (string.IsNullOrEmpty(guessText))
        {
            Debug.LogWarning("Please enter a number.");
            return;
        }

        inputField.text = ""; //입력창 비우기
        
        byte[] bytes = Encoding.UTF8.GetBytes(guessText);
        try
        {
            await Task.Run(() => stream.Write(bytes, 0, bytes.Length));

            bytes = new byte[1024];
            int bytesRead = stream.Read(bytes, 0, bytes.Length);
            string response = Encoding.UTF8.GetString(bytes, 0, bytesRead);
            responseText.text = response;
            Debug.Log("Server says: " + response);
        }
        catch (Exception e)
        {
            Debug.LogError("Communication Error : "+e.Message);
        }
    }

    private void OnDestroy()
    {
        if(stream!=null)
            stream.Close();
        if(client!=null)
            client.Close();
    }
}
