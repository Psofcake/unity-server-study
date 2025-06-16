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
        listenerThread = new Thread(ListenForIncomingRequests); //연결 수신처리를 위한 별도의 스레드 생성. 메인 스레드의 멈춤을 방지.
        listenerThread.IsBackground = true; // 메인 스레드가 종료되면 함께 종료
        listenerThread.Start();
    }

    void ListenForIncomingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7077);  //지정된 IP와 포트에서 연결수신 대기
            tcpListener.Start();
            Debug.Log("Server is listening!");

            while (true)
            {
                TcpClient connectedTcpClient = tcpListener.AcceptTcpClient();   //새 클라이언트와 연결 수신될 때 까지 Blocking
                
                //각 클라이언트에 대해 별도의 HandleClientComm 메서드를 새 스레드에서 실행.
                Thread clientThread = new Thread(() => HandleClientComm(connectedTcpClient));
                clientThread.IsBackground = true;
                clientThread.Start();
            }

        }
        catch (SocketException e)   //예외 처리
        {
            Debug.Log("SocketException:" + e);
        }
        finally //리소스 정리
        {
            //서버 종료 시 리스너 중지
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }
    }

    void HandleClientComm(TcpClient client)
    {
        //using문을 사용하여 리소스를 관리.
        //스트림이 먼저 닫히고, TCP연결객체(클라이언트)를 그 후에 정리한다.
        
        // using() : using 선언, using declaration (C# 8.0 이상에서 가능)
        // 블록{}을 명시하지 않아도 변수가 속한 범위(scope)가 끝날 때 Dispose()를 자동으로 호출.
        using (client) //즉, client가 포함된 가장 가까운 중괄호 블록(HandleClientComm메서드)이 끝나는 시점에서 Dispose호출.
        using (NetworkStream stream = client.GetStream()) //위 using선언과는 다르게, 명시된 블록{} 끝에서 즉시 Dispose호출. 
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) //연결 종료가 아닐 때 루프
                {
                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Received:" + dataReceived);

                    string response = "Server received:" + dataReceived;
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length); //응답 메시지 전송

                    //종료조건
                    if (dataReceived.Trim().ToLower() == "exit")    //Trim(): 문자열 앞뒤의 개행문자, 공백, 탭 등 공백문자들을 모두 제거
                    {
                        break;  //"exit" 메시지를 받으면 루프를 종료하고 클라이언트와의 연결을 닫는다.
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Communication error:" + e);
            }
        }
    }

    void OnApplicationQuit()    //Unity 애플리케이션이 종료될 때 호출.
    {
        if (listenerThread != null && listenerThread.IsAlive)   //종료시점에도 리스너스레드가 살아있다면
        {
            listenerThread.Abort(); //리스너스레드 강제 종료
        }
    }
}