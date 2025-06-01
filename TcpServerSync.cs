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

            while(true) //동기 서버이므로 while 루프를 돌며 항상 대기하는 상태를 유지한다. 
            {
                //클라이언트가 연결 요청을 보낼 때까지 대기. 연결이 올 때까지 블로킹이 발생한다. 연결을 수락한 후 연결된 TcpClient 객체를 connectedClient에 반환.
                connectedClient = tcpListener.AcceptTcpClient();   //tcpListener.Stop() 호출로 서버 소켓이 강제로 닫혔다면 루프를 중단하고 catch-finally로 가서 자원을 정리한다.
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
            if(tcpListener != null) //포트가 이미 사용중이거나, 시스템에 네트워크 리소스가 부족한 경우 TcpListener 생성 중 예외가 발생할 수 있다. (tcpListener가 null이 될 수 있다.)
            {
                tcpListener.Stop(); // tcpListener가 null이 아니라면, 클라이언트 연결 수신을 중지한다. (NullReferenceException을 방지하기 위함)
            }
        }
    }
    
    private void HandleClient(TcpClient client)
    { //using : 자원을 자동으로 해제하기 위한 구문. 특히 스트림, 파일, 소켓 등 외부 자원을 사용할 때 매우 중요하다.
      using(NetworkStream stream = client.GetStream())  //using (...) 에서 선언된 NetworkStream 객체 stream은, 블록이 끝날 때 자동으로 .Dispose()가 호출되어 자원을 정리한다.
      {
         byte[] buffer = new byte[1024];    //한 번의 통신에서 가져올 수 있는 데이터의 크기 (1KB)
         int bytesRead; //읽은 바이트의 수를 저장

         while(true)
         {
            try
            {
                //.Read() : buffer.Length만큼 데이터를 읽어서 배열(buffer)에 인덱스 0번부터 채워 넣는다. 그리고 실제로 읽힌 바이트 수(읽은 데이터의 길이)를 반환
                bytesRead = stream.Read(buffer, 0, buffer.Length);

                if(bytesRead ==0)   //상대가 정상적으로 소켓을 닫은 상태. 더 이상 받을 데이터가 없음(연결 종료 신호)
                {
                    Debug.Log("Client Disconnected");
                    break;
                }
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead); // buffer에 저장된 데이터를 UTF8 인코딩 기준으로 문자열로 변환
                Debug.Log("Received :" + message);

                stream.Write(buffer, 0, bytesRead);  //server to client (실제로 읽은 바이트 수 만큼만 전송. 즉, 유효한 데이터만 보냄)
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
                break;  //예외가 발생하였으므로 while 루프를 빠져나간다.
            }
         }
      }
      client.Close();
    }

    private void OnApplicationQuit() 
    {
        if(serverThread != null && serverThread.IsAlive)    //스레드가 종료되지 않고 실행중이라면 true, 시작되지 않았거나, 예외로 중단되었거나, 이미 종료되었으면 false.
        {
            serverThread.Abort();   //스레드가 실행중이던 작업을 강제로 종료.
        } 
        
        if(tcpListener != null)
        {
            tcpListener.Stop(); //블로킹을 해제하기 위해 내부적으로 소켓을 닫고 SocketException을 발생시켜 루프에서 빠져나오게 함
        }

        if(connectedClient != null)
        {
            connectedClient.Close();    //클라이언트와의 연결을 명시적으로 종료.
        }
    }

}