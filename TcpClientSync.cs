using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net.Sockets;
using System.Text;


public class TcpClientSync : MonoBehaviour
{
    //접속할 TCP 서버의 IP주소와 포트번호
    [SerializeField] private string serverIP = "127.0.0.1"; //localhost IP //192.168.0.0.1 
    [SerializeField] private int serverPort = 8888;

    [SerializeField] private InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text responseText; //또는 TextMeshProUGUI
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

        //UI가 준비되지 않은 경우 이하는 실행되지 않음.
        sendButton.onClick.AddListener(SendData);   //버튼 누르면 SendData() 호출
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);    //지정한 IP,포트로 TCP 연결을 시도.
            stream = tcpClient.GetStream(); //NetworkStream을 얻어 통신 준비
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

        byte[] data = Encoding.UTF8.GetBytes(message); //인풋필드의 텍스트를 UTF-8로 인코딩

        try
        {
            stream.Write(data, 0, data.Length); //NetworkStream을 통해 서버로 전송
            Debug.Log("sent:" + message);
            inputField.text = "";   //입력창 비우기
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

        // 서버 연결이 종료되면 루프 종료
        while (tcpClient != null && tcpClient.Connected)
        {
            try
            {
                byteRead = stream.Read(buffer, 0, buffer.Length); //서버에서 데이터가 도착할 때까지 대기하므로 blocking 발생가능
                if (byteRead == 0)
                {
                    Debug.Log("server disconnected");
                    responseText.text = "serverIP disconnected";
                    break;
                }
                string receiveMessage = Encoding.UTF8.GetString(buffer, 0, byteRead);
                responseText.text = "server :" + receiveMessage; //Text로 출력
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

    void OnDestroy()
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