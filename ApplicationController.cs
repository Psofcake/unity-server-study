using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    // ~~~~~~~~  계층 구조  ~~~~~~~~
    // (1) AppController    :: 최상위 컨트롤러로, 어떤 모드(서버/호스트/클라이언트)로 실행할 지를 결정하고 각 모드의 Singleton을 호출하여 진입.
    // |
    // ├────────────────────────┬──────────────────┐
    // ↓                        ↓                  ↓
    // (2) ServerSingleton  HostSingleton  ClientSingleton      :: 각 모드별 진입점이자 단일 인스턴스를 보장하는 관리 객체(싱글톤).
    // |                        |                  |
    // ↓                        ↓                  ↓
    // (3) ServerGameManager  HostGameManager  ClientGameManager    :: 모드별 핵심 게임 로직 처리(게임 흐름, 룰, 상태 등). 인증,매치메이킹,통신,저장 등 온라인/클라우드 관련 각 기능을 UGS (Unity Gaming Services)와 연동한다.
    // |                                           |
    // ↓                                           ↓
    // (4) NetworkServer                       NetworkClient    :: 실제 통신 담당, 클라이언트+서버 정보 저장. 서버 네트워크 처리 / 클라이언트 네트워크 처리
    
    // ServerGameManager
    //     매치메이킹 결과로 서버를 생성
    //     UGS Relay 서버와 연결
    //     인증된 플레이어만 입장 허용
    //     게임 중 데이터 Cloud Save
    //
    // ClientGameManager
    //     UGS Authentication으로 로그인
    //     Lobby 및 Matchmaker 사용해 매치 요청
    //     Relay 통해 서버 접속
    //     게임 종료 후 Cloud Save
    //         
    // HostGameManager
    //     클라이언트처럼 인증 및 로비 참여
    //     동시에 서버처럼 Relay 호스트 역할 수행

    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    
    private async void Start()
    {
        DontDestroyOnLoad(gameObject);  //씬이 바뀌어도 파괴되지 않게 함.(게임 전반에 걸쳐 살아남는 전역 컨트롤러)
        
        //for dedicated server (그래픽 디바이스 타입이 NULL이면 그래픽이 없는 환경. 즉, 전용 서버를 의미)
        await LaunchMode(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null);
    }
    
    //전용 서버인지 클라이언트인지 구분
    private async Task LaunchMode(bool isDedicateServer)
    {
        if (isDedicateServer)
        {
            Debug.Log("<Dedicated server>");
        }
        else
        {
            //클라이언트 생성 (순서는 호스트 먼저)
            Debug.Log("<Client>");
            
            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();
            
            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();

			//인증 성공 시 메뉴 시작
			if(authenticated)
				clientSingleton.clientGameManager.StartMenu();
        }
    }
}
