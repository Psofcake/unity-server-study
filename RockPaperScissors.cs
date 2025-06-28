using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class RockPaperScissors : MonoBehaviour  //클라이언트 연결 관리, 선택 수신, 승/패 판정, 결과 전송
{
    [SerializeField] private int port = 8888;
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private Dictionary<int,TcpClient> clients = new Dictionary<int, TcpClient>();
    private Dictionary<int, string> choices = new Dictionary<int, string>(); //<clientID,가위바위보>

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
                TcpClient client = tcpListener.AcceptTcpClient();
                int clientID = nextClientID++;
                clients.Add(clientID, client);

                Debug.Log("Client accepted: " + client.Client.RemoteEndPoint);

                SendClientID(client, clientID); //클라이언트에게 ID 전송

                //클라이언트 통신 스레드 시작
                Thread clientThread = new Thread(() => HandleClientCommunication(client, clientID));
                clientThread.IsBackground = true;
                clientThread.Start();

                //두명의 클라이언트가 접속하면
                if (clients.Count >= 2)
                {
                    BroadcastMessage("StartGame"); //게임 시작
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

    private void SendClientID(TcpClient client, int clientID)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] idData = Encoding.UTF8.GetBytes(clientID.ToString());
            stream.Write(idData, 0, idData.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("ID Send Error: " + e.Message);
        }
    }

    private void HandleClientCommunication(TcpClient client, int clientID)
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
                        Debug.Log("Client disconnected: " + clientID);
                        clients.Remove(clientID);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Receive from client " + clientID + ": " + message);

                    //클라이언트의 선택 저장
                    choices[clientID] = message;

                    //두 플레이어 모두 선택했으면
                    if (choices.Count == 2)
                    {
                        DetermineWinner();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Client communication error: " + clientID+"-"+e.Message);
                    clients.Remove(clientID);   //클라이언트 연결 문제 발생 시 제거
                    break;
                }
            }
        }
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
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients.Values)
        {
            try
            {
                NetworkStream stream = client.GetStream();
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

        foreach (var client in clients.Values)
        {
            client.Close();
        }
        if (tcpListener != null) 
        {
            tcpListener.Stop();
        }
    }
}
