using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;

public class TcpClientAsync : MonoBehaviour
{
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 8888;
    
    [SerializeField] private InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text receivedText;
    
    private TcpClient client;
    private NetworkStream stream;
    
    private async void Start()
    {
        //UI 요소가 유효한 지 확인
        if (inputField == null||sendButton == null||receivedText == null)
        {
            Debug.Log("UI elements are not assigned");
            return;
        }
        
        sendButton.onClick.AddListener(SendData);   //버튼클릭 이벤트에 SendData 연결

        try
        {
            await ConnectToServer(); //비동기로 서버에 연결
        }
        catch (Exception ex)
        {
            Debug.LogError("Connection error: "+ex.Message);
            receivedText.text = "Connection failed.";
        }
    }
    private async Task ConnectToServer()
    {
        client = new TcpClient();
        await client.ConnectAsync(serverIP, serverPort); //비동기 연결
        stream = client.GetStream();
        receivedText.text = "Connected to server";
        Debug.Log("Connected to server");
    }
    public async void SendData()
    {
        if (client == null || !client.Connected)
        {
            Debug.LogError("Client is not connected");
            receivedText.text = "Not connected";
            return;
        }
        
        string msg = inputField.text;

        if (string.IsNullOrEmpty(msg)) //빈 문자열 방지
        {
            Debug.LogWarning("Msg is null or empty");
            return;
        }
        
        byte[] data = Encoding.UTF8.GetBytes(msg);

        try
        {
            //await stream.WriteAsync(data, 0, data.Length);  // 서버에 데이터 전송(비동기) <.NET 비동기 소켓 API
            await Task.Run(() => stream.Write(data, 0, data.Length));   //동기함수를 쓰레드로 감싸 비동기 처리 (쓰레드 생성으로 CPU 리소스 더 소비)
            Debug.Log("Sent: " + msg);

            byte[] buffer = new byte[1024];
            //int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); //서버 응답 수신(비동기)
            int bytesRead = await Task.Run(() => stream.Read(buffer, 0, buffer.Length));  //별도 쓰레드로 비동기 처리
            string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            receivedText.text = "Server says: " + received;
            Debug.Log("Received: " + received);
        }
        catch (Exception ex)
        {
            Debug.LogError("Send/Receive error: " + ex.Message);
            receivedText.text = "Error: " + ex.Message;
        }
    }

    private void OnDestroy()    //객체 파괴 시
    {
        //연결 종료
        if(stream!= null)
            stream.Close();
        if(client!=null)
            client.Close();
    }
}
