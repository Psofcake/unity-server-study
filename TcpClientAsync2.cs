using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TcpClientAsync2 : MonoBehaviour
{
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 8888;
    
    [SerializeField] private InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text receivedText;
    
    private TcpClient client;
    private NetworkStream stream;
    
    async void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
        await ConnectToServer();
    }

    async Task ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server");
            receivedText.text = "Connected to server!";
        }
        catch (Exception ex)
        {
            Debug.LogError("Error connecting to the server: " + ex.Message);
            receivedText.text = "Failed to connect: "+ex.Message;
        }
    }

    async void SendMessage()
    {
        if (client == null || !client.Connected)
        {
            Debug.LogError("Client is not connected");
            receivedText.text = "Not connected";
            return;
        }
        
        try
        {
            string msg = inputField.text;
            byte[] data = Encoding.UTF8.GetBytes(msg);
            if (string.IsNullOrEmpty(msg)) //빈 문자열 방지
            {
                Debug.LogWarning("Msg is null or empty");
                return;
            }
            await stream.WriteAsync(data, 0, data.Length);  // 서버에 데이터 전송(비동기) <.NET 비동기 소켓 API
            Debug.Log("Sent: " + msg);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); //서버 응답 수신(비동기)

            if (bytesRead > 0)
            {
                string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                receivedText.text = "Server says: " + received;
                Debug.Log("Received: " + received);
            }
            else
            {
                Debug.Log("Server closed connection.");
                receivedText.text = "Server closed connection.";
                client.Close();
                client = null;
            }
            inputField.text = ""; //입력 필드 초기화
        }
        catch (Exception ex)
        {
            Debug.LogError("Send/Receive error: " + ex.Message);
            receivedText.text = "Error: " + ex.Message;

            if (client != null)
            {
                client.Close();
                client = null;
            }
        }
    }
}
