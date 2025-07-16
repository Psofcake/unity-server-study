using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class HighLowServer : MonoBehaviour
{
    private TcpListener tcpListener;
    private Thread listenerThread;
    private TcpClient connectedClient;
    [SerializeField] private int port = 8888;

    private int secretNumber;
    
    // Start is called before the first frame update
    void Start()
    {
        listenerThread = new Thread(ListenForClients); //연결 수신처리를 위한 별도의 스레드 생성. 메인 스레드의 멈춤을 방지.
        listenerThread.IsBackground = true; // 메인 스레드가 종료되면 함께 종료
        listenerThread.Start();
    }

    private void ListenForClients()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            tcpListener.Start();
            Debug.Log("Server started. Listening on port " + port);

            while (true)
            {
                connectedClient = tcpListener.AcceptTcpClient();
                Debug.Log("Client accepted: " + connectedClient.Client.RemoteEndPoint);

                StartNewGame();
                
                Thread clientThread = new Thread(()=>HandleClient(connectedClient));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Server error: "+e.Message);
        }
        finally
        {
            if(tcpListener != null)
                tcpListener.Stop();
        }
    }

    private async void StartNewGame()
    {
        //1~100 사이 랜덤 숫자 생성
        System.Random random = new System.Random();
        secretNumber = random.Next(1, 101);
        //secretNumber = UnityEngine.Random.Range(1,101);
        //(설명)유니티엔진 라이브러리의 랜덤함수는 메인스레드에서만 안전하게 사용되므로 다른스레드 사용은 비추천. 따라서 System.Random활용
        
        Debug.Log("Secret number has been generated.");
    }
    private void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log("Client disconnected");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Received from client: " + message);

                    if (int.TryParse(message, out int guess))   //문자열을 숫자로 변환(실패 시 false반환, guess는 0)
                    {
                        string response = CheckGuess(guess);    //클라이언트에서 온 추측을 판정한다.
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);   //응답 전송

                        if (response == "Correct!")
                        {
                            StartNewGame(); //정답을 맞춤. 새 게임 시작(연결을 끊지 않음)
                        }
                        else
                        {
                            Debug.Log("Wrong guess! ::: "+guess);
                        }
                    }
                    else
                    {   //유효한 추측(숫자)이 아닐 때
                        byte[] responseBytes = Encoding.UTF8.GetBytes("Invalid input.");
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Client communication error: " + e.Message);
                    break;
                }
            }
        }
    }

    private string CheckGuess(int guess)
    {
        if (guess < secretNumber)
        {
            return "Too low";
        }
        else if (guess > secretNumber)
        {
            return "Too high";
        }
        else
        {
            return "Correct!";
        }
    }

    private void OnDestroy()
    {
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Abort();
        }

        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
    }
}
