using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TcpServerAsync2 : MonoBehaviour
{
	public int port = 7077;
    private TcpListener tcpListener;
    private bool isRunning = false;

    private async void Start()
    {
        tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        tcpListener.Start();
        isRunning = true;

        Debug.Log("Async TCP Server started...");

        // 비동기 클라이언트 수신 루프 시작
        await AcceptClientsAsync();
    }

    private async Task AcceptClientsAsync()
    {
        while (isRunning)
        {
            try
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync(); // 비동기 클라이언트 수신
                Debug.Log("Client connected!");
                _ = HandleClientAsync(client); // 클라이언트 처리 비동기 실행
            }
            catch (Exception ex)
            {
                Debug.LogError("Error accepting client: " + ex.Message);
                break;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];

        try
        {
            while (client.Connected)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0)
                    break; // 클라이언트 종료

                string received = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Debug.Log("Received: " + received);

                // 클라이언트에게 에코 응답
                byte[] response = Encoding.UTF8.GetBytes("Echo: " + received);
                await stream.WriteAsync(response, 0, response.Length);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Client handling error: " + ex.Message);
        }
        finally
        {
            Debug.Log("Client disconnected.");
            stream.Close();
            client.Close();
        }
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        tcpListener.Stop();
        Debug.Log("Server stopped.");
    }
}
