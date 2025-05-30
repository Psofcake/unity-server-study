using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TcpServerSync : MonoBehaviour //Unity 컴포넌트로 동작하기 위해 MonoBehavior를 상속받음
{
    [SerializeField] private int port = 8888;
    private TcpListener tcpListener;    
    private Thread serverThread;    //연결 수신을 처리하기 위한 별도의 스레드. 
    private TcpClient connectedClient;  


    // Start is called before the first frame update
    void Start()
    {
        serverThread = new Thread(RunServer); //RunServer메소드를 실행하는 serverThread를 생성. 메인 스레드(Unity의 주 스레드)가 멈추는 것을 최소화하기 위해 별도의 스레드에서 처리.
        serverThread.IsBackground = true;   //백그라운드 스레드로 설정하면 메인 스레드가 종료될 때 자동으로 함께 종료된다.
        serverThread.Start();   //serverThread를 시작.

    }

    private void RunServer()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port); //TCP 연결을 수신하는 리스너 객체를 생성
            tcpListener.Start();    // 클라이언트 연결 수신을 시작.
            Debug.Log("Server Start!:"+port);
            /*
             IPAddress.Any는 "모든 네트워크 인터페이스(IP주소)"를 의미한다.
             보안상 의도적으로 특정 IP만 받도록 제한하려면 모든 연결을 받은 후 허용된 IP인지 검사해서 연결할지 거부할지 결정하는 방식으로 구현한다.
             또는, 특정 IP에서만 수신하고 싶다면 IPAddress.Parse("192.168.0.10")처럼 명시할 수도 있다.
             아주 민감한 시스템이라면 더 복잡한 인증 방식이나 방화벽 설정으로 사전 차단하는 것이 더 좋을 수도 있다. 
             */

            while(true) //동기 서버이므로 while 루프를 돌며 항상 대기하는 상태를 유지한다.
            {
                connectedClient = tcpListener.AcceptTcpClient();    //연결 수신 후, 연결된 TcpClient 객체를 connectedClient에 저장.
                Debug.Log("Client connected");

                HandleClient(connectedClient);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Server Err"+e.Message);
        }
        finally
        {
            if(tcpListener != null) //포트가 이미 사용중이거나, 시스템에 네트워크 리소스가 부족한 경우 TcpListener 생성 중 예외가 발생할 수 있다.
            {
                tcpListener.Stop(); // 이에 해당하는 경우 클라이언트 연결 수신을 중지한다.
            }
        }
    }
    
    private void HandleClient(TcpClient client)
    {
      using(NetworkStream stream = client.GetStream())
      {
         byte[] buffer = new byte[1024];
         int bytesRead;

         while(true)
         {
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);

                if(bytesRead ==0)
                {
                    Debug.Log("Client Disconnected");
                    break;
                }
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Received :" + message);

                stream.Write(buffer, 0, bytesRead);  //server to client


            }
            catch(Exception e)
            {
                if (e is SocketException || e is ObjectDisposedException)
                {
                    Debug.Log("Client Disconnected");
                }
                else
                {
                    Debug.LogError("Client conn error");
                }
                break;

            }

         }

      }

     client.Close();
    
    }

    private void OnApplicationQuit() 
    {
        if(serverThread != null && serverThread.IsAlive)
        {
            serverThread.Abort();
        }   

        if(tcpListener != null)
        {
            tcpListener.Stop();
        }

        if(connectedClient != null)
        {
            connectedClient.Close();
        }
    }

}