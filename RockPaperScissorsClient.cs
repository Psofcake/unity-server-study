using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.UI;

public class RockPaperScissorsClient : MonoBehaviour    //서버 연결, 버튼 입력 처리, 서버에 선택 전송, 결과 수신 및 표시
{
    // UI -버튼 3개(Rock/Paper/Scissors), Text, 상대방 선택 표시창, 
    // 라운드 결과(승/패/무승부) 표시창, 점수, 게임 종료 메시지
    [SerializeField]private string serverIP = "127.0.0.1";
    [SerializeField]private int serverPort = 7777;
    [SerializeField] private Button rock;
    [SerializeField] private Button paper;
    [SerializeField] private Button scissors;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI opponentChoiceText;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    private TcpClient client;
    private NetworkStream stream;

    private int clientId;
    private int playerScore = 0;
    private int opponentScore = 0;

    private async void Start()
    {
        rock.onClick.AddListener(()=>SendChoice("Rock"));
        paper.onClick.AddListener(()=>SendChoice("Paper"));
        scissors.onClick.AddListener(()=>SendChoice("Scissors"));
        
        SetButtonsActive(false);

        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server!");

            //서버로부터 클라이언트의 ID를 수신함
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            clientId = int.Parse(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            Debug.Log("My Client ID: " + clientId);

            //비동기 메시지 수신 시작
            ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection Error: "+e.Message);
            resultText.text = "Connection failed.";
        }
    }

    private async void SendChoice(string choice)
    {
        if (client == null || !client.Connected)
            return;
        byte[] data = Encoding.UTF8.GetBytes(choice);
        try
        {
            await stream.WriteAsync(data, 0, data.Length);
            SetButtonsActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError("Send Error: " + e);
        }
    }

    private async void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (true) 
        {
            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.Log("Disconnected from server");
                    break;
                }
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Received: " + message);
                
                ProcessServerMessage(message);
            }
            catch(Exception e)
            {
                Debug.LogError("Error: " + e);
                break;  //연결 문제 발생 시 루프 종료
            }
        }
    }

    private void ProcessServerMessage(string message)
    {
        //게임 시작 메시지
        if (message == "StartGame")
        {
            resultText.text = "Starting game...";
            SetButtonsActive(true);
            opponentChoiceText.text = ""; //상대 선택 초기화
        }
        else if (message == "EndGame")
        {
            resultText.text = "Ending game...";
            SetButtonsActive(false);
        }
        else
        {
            string[] parts = message.Split(',');
            string result = parts[0];
            string myChoice;
            string opponentChoice;

            if (clientId == 1) //내가 클라이언트 1
            {
                myChoice = parts[1];
                opponentChoice = parts[2];
            }
            else
            {
                myChoice = parts[2];
                opponentChoice = parts[1];
            }
            
            opponentChoiceText.text = "Opponent: "+opponentChoice;

            if (result == "Draw")
            {
                resultText.text = "Draw!";
            }
            else if (result == "Player1 Wins" && clientId == 1 || result == "Player2 Wins" && clientId == 2)
            {
                resultText.text = "You wins!";
                playerScore++;
            }
            else
            {
                resultText.text = "You loses!";
                opponentScore++;
            }
            scoreText.text = "Score: " + playerScore+" - "+opponentScore;
        }
    }

    private void SetButtonsActive(bool active)
    {
        rock.interactable = active;
        paper.interactable = active;
        scissors.interactable = active;
    }

    private void OnDestroy()
    {
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}
