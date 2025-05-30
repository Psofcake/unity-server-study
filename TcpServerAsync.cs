using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


public class TcpServerASync : MonoBehaviour
{

    private TcpListener tcpListener;
    private Thread listenerThread;

    // Start is called before the first frame update
    void Start()
    {
        listenerThread = new Thread(ListenForIncomingRequests);
        listenerThread.IsBackground = true;
        listenerThread.Start();

    }

    void ListenForIncomingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7077);
            tcpListener.Start();
            Debug.Log("Server is listening!");

            while (true)
            {
                TcpClient connectedTcpClient = tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClientComm(connectedTcpClient));
                clientThread.IsBackground = true;
                clientThread.Start();
            }

        }
        catch (SocketException e)
        {
            Debug.Log("SocketException:" + e);
        }
        finally
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }

    }

    void HandleClientComm(TcpClient client)
    {

        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Received:" + dataReceived);

                    string response = "Server received:" + dataReceived;
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);

                    if (dataReceived.Trim().ToLower() == "exit")
                    {
                        break;

                    }

                }
            }
            catch (Exception e)
            {
                Debug.Log("Communication error:" + e);
            }

        }


    }

    void OnApplicationQuit()
    {
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Abort();
        }

    }

}