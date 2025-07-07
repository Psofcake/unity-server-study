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
        
        SetButtonsActive(false); // 버튼 비활성화 (서버 연결 전)

        await ConnectToServer(); // 버튼 비활성화 (서버 연결 전)
    }

    private async Task ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server!");

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
            await stream.WriteAsync(data, 0, data.Length); // 선택 전송
            SetButtonsActive(false); // 선택 후 버튼 비활성화
        }
        catch (Exception e)
        {
            Debug.LogError("Send Error: " + e);
        }
    }

    private async void ReceiveMessages()
    {
        while (true) 
        {
            try
            {
                // 1. 메시지 타입 읽기 (1바이트)
                byte[] typeBuffer = new byte[1];
                int typeBytesRead = await stream.ReadAsync(typeBuffer, 0, 1);
                if (typeBytesRead == 0)
                {
                    Debug.Log("Disconnected from server.");
                    break;
                }
                string messageType = Encoding.UTF8.GetString(typeBuffer);
                
                // 2. 메시지 길이 읽기 (4바이트, int)
                byte[] lengthBuffer = new byte[4];
                int lengthBytesRead = await stream.ReadAsync(lengthBuffer, 0, 4);
                if (lengthBytesRead == 0)
                {
                    Debug.Log("Disconnected from server.");
                    break;
                }
                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                // 3. 메시지 데이터 읽기 (messageLength 만큼)
                byte[] messageBuffer = new byte[messageLength];
                int bytesRead = await stream.ReadAsync(messageBuffer, 0, messageLength);
                
                if (bytesRead == 0)
                {
                    Debug.Log("Disconnected from server");
                    break;
                }
                string message = Encoding.UTF8.GetString(messageBuffer);
                Debug.Log("Received: " + message);
                
                // 서버 메시지 처리
                if (messageType == "I") // 클라이언트 ID 수신
                {
                    if (int.TryParse(message, out int receivedClientId))
                    {
                        clientId = receivedClientId;
                        Debug.Log("My client ID: " + clientId);
                        resultText.text = "Connected! ID: " + clientId;
                    }
                    else
                    {
                        Debug.LogError("Invalid client ID received: " + message);
                        // 적절한 오류 처리 (연결 종료 등)
                    }
                }
                else // 다른 메시지 ("S", "R" 등)
                {
                    ProcessServerMessage(message);
                }
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
                resultText.text = "You win!";
                playerScore++;
            }
            else
            {
                resultText.text = "You lose!";
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
