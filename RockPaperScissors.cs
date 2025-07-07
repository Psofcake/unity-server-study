using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent; // ConcurrentDictionary 사용
using System.Threading.Tasks;

public class RockPaperScissors : MonoBehaviour  //클라이언트 연결 관리, 선택 수신, 승/패 판정, 결과 전송
{
    [SerializeField] private int port = 8888;
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    
    // 클라이언트 정보 저장을 위한 구조체 (예시)
    private struct ClientInfo
    {
        public TcpClient client;
        public NetworkStream stream;
        //public Thread thread; // 필요한 경우 스레드도 저장
    }
    
    //멀티스레드 전용 딕셔너리 ConcurrentDictionary 사용
    private ConcurrentDictionary<int,ClientInfo> clients = new ConcurrentDictionary<int, ClientInfo>();
    private ConcurrentDictionary<int, string> choices = new ConcurrentDictionary<int, string>(); //<clientID,선택한가위바위보>

    private int nextClientID = 1;
    private int round = 1;
    private int maxRounds = 3; //최대 라운드 수
    
    // Start is called before the first frame update
    void Start()
    {
        tcpListenerThread = new Thread(ListenForClients);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    void ListenForClients()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            Debug.Log("Listening for connections...");

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();   //클라이언트 연결 수락
                int clientID = nextClientID++;  //ID 할당
                // 클라이언트 정보 저장
                ClientInfo clientInfo = new ClientInfo(){ client = client, stream = client.GetStream() };
                
                if (clients.TryAdd(clientID, clientInfo)) // 딕셔너리에 추가
                {
                    Debug.Log("Client "+clientID+" accepted: " + client.Client.RemoteEndPoint);
                    
                    SendClientID(clientInfo, clientID); //클라이언트에게 ID 전송
                    
                    //클라이언트 통신 스레드 시작
                    Thread clientThread = new Thread(() => HandleClientCommunication(clientID));
                    clientThread.IsBackground = true;
                    clientThread.Start();
    
                    //두명의 클라이언트가 접속하면
                    if (clients.Count >= 2)
                    {
                        BroadcastMessage("StartGame"); //게임 시작
                    }
                }
                else
                {
                    Debug.LogError("Failed to add client to dictionary.");
                    client.Close(); // 딕셔너리 추가 실패 시 연결 종료
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Server error: " + e.Message);
        }
        finally
        {
            if(tcpListener!=null)tcpListener.Stop();
        }
    }

    private async void SendClientID(ClientInfo clientInfo, int clientID)
    {
        try
        {
            byte[] idData = Encoding.UTF8.GetBytes(clientID.ToString());
            
            // 메시지 타입 ("I") . 1바이트
            byte[] typeData = Encoding.UTF8.GetBytes("I");

            // 메시지 길이 (idData의 길이). int는 4바이트이므로 BitConverter.GetBytes(int)는 byte[4]을 반환한다.
            byte[] lengthData = BitConverter.GetBytes(idData.Length);
            
            // 타입 + 길이 + 데이터.
            byte[] combined = new byte[typeData.Length + lengthData.Length + idData.Length];
            
            Buffer.BlockCopy(typeData, 0, combined, 0, typeData.Length);
            Buffer.BlockCopy(lengthData, 0, combined, typeData.Length, lengthData.Length);
            Buffer.BlockCopy(idData, 0, combined, typeData.Length + lengthData.Length, idData.Length);
            // Buffer.BlockCopy : 메모리 블록 수준에서 동작하여 배열 간의 빠르고 저수준의 복사를 수행.
            // Array.Copy보다 더 빠르고 정확한 바이트 단위 복사. 서버 코드이므로 더 빠르게 데이터를 처리하기 위한 방법으로 사용됨.
            // (설명) Buffer.BlockCopy(
            //     Array src,       // 원본 배열
            //     int srcOffset,   // 원본에서 시작할 위치 (바이트 단위)
            //     Array dst,       // 대상 배열
            //     int dstOffset,   // 대상에서 시작할 위치 (바이트 단위)
            //     int count        // 복사할 바이트 수
            // );
            
            await clientInfo.stream.WriteAsync(combined, 0, combined.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("ID Send Error: " + e.Message);
        }
    }

    private void HandleClientCommunication(int clientID)
    {
        if (!clients.ContainsKey(clientID)) //클라이언트 정보가 있는지 확인
        {
            Debug.LogError($"ClientInfo Not Found, ID:{clientID}");
            return;
        }
        ClientInfo clientInfo = clients[clientID]; //클라이언트 정보 가져오기.
        NetworkStream stream = clientInfo.stream;
        
        // using 문 제거: 클라이언트 연결을 스레드 시작 시 바로 닫지 않음
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (true)
        {
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.Log("Client disconnected: " + clientID);
                    //연결 종료 시 클라이언트 제거
                    clients.TryRemove(clientID, out _);
                    choices.TryRemove(clientID, out _);
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Receive from client " + clientID + ": " + message);

                //클라이언트의 선택 저장. AddOrUpdate:존재하지 않으면 추가, 있으면 업데이트
                choices.AddOrUpdate(clientID, message, (key, oldValue) => message);

                //두 플레이어 모두 선택했으면 판정
                if (choices.Count == 2)
                {
                    DetermineWinner();
                }
            }
            catch (Exception e)
            {
                Debug.Log("Client communication error: " + clientID + "-" + e.Message);
                //클라이언트 연결 문제 발생 시 클라이언트 제거
                clients.TryRemove(clientID, out _);
                choices.TryRemove(clientID, out _);
                break;
            }
        }
        clientInfo.client.Close(); //여기서 using을 대신하여 Close()
    }

    private void DetermineWinner()
    {
        //choices 딕셔너리에서 클라이언트 ID와 선택 가져오기(List로 변환)
        List<int> clientIDs = new List<int>(choices.Keys);
        
        //플레이어 1과 2의 선택을 가져옴
        string choice1 = choices[clientIDs[0]];
        string choice2 = choices[clientIDs[1]];

        string result = "";

        if (choice1 == choice2) result = "Draw";
        else if ((choice1 == "Rock" && choice2 == "Scissors") ||
                 (choice1 == "Scissors" && choice2 == "Paper") ||
                 (choice1 == "Paper" && choice2 == "Rock"))
        {
            result = "Player1 Wins";}
        else
        {
            result = "Player2 Wins";
        }
        
        //결과 브로드캐스트 + 상대방의 선택
        BroadcastMessage($"{result}, {choice1}, {choice2}"); //결과, 선택1, 선택2
        
        //다음 라운드 준비 또는 게임 종료
        choices.Clear(); //선택 초기화
        round++;
        if(round > maxRounds)
        {
            BroadcastMessage("EndGame");
            //필요하면 여기서 서버를 중지하거나 새 게임을 시작하는 로직 추가
        }
        else
        {
            BroadcastMessage("StartGame"); //다음 라운드 시작
        }
    }

    private void BroadcastMessage(string message)
    {
        string type = "";

        switch (message)
        {
            case "StartGame":
                type = "S";
                break;
            case "EndGame":
                type = "E";
                break;
            default:
                type = "R"; //결과
                break;
        }
        
        byte[] typeData = Encoding.UTF8.GetBytes(type);
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        // 메시지 길이
        byte[] lengthData = BitConverter.GetBytes(data.Length);
        
        //타입+길이+데이터
        byte[] combined = new byte[typeData.Length + lengthData.Length + data.Length];
        Buffer.BlockCopy(typeData, 0, combined, 0, typeData.Length);
        Buffer.BlockCopy(lengthData, 0, combined, typeData.Length, lengthData.Length);
        Buffer.BlockCopy(data, 0, combined, typeData.Length + lengthData.Length, data.Length);

        //모든 클라이언트에게 메시지 전송
        foreach (var clientInfo in clients.Values)
        {
            try
            {
                NetworkStream stream = clientInfo.stream;
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Debug.LogError("Broadcast error: " + e.Message);
            }
        }
    }

    private void OnDestry()
    {
        if (tcpListenerThread != null && tcpListenerThread.IsAlive)
        {
            tcpListenerThread.Abort();
        }

        foreach (var clientInfo in clients.Values)
        {
            clientInfo.client.Close();
        }
        if (tcpListener != null) 
        {
            tcpListener.Stop();
        }
    }
}
