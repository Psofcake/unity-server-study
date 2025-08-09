using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    //서버의 권한을 구현한다.
    protected override bool OnIsServerAuthoritative()
    {
        return false;   //서버권한이 아닌 클라이언트 권한으로 자신의 오브젝트 트랜스폼을 서버에 전송한다.
    }

    public override void OnNetworkSpawn() //네트워크 오브젝트가 생성될 때 호출
    {
        base.OnNetworkSpawn();
        
        //클라이언트 객체(Player)가 스폰되었을때 Transform 정보를 업데이트 할 수 있도록 권한 부여.
        CanCommitToTransform = IsOwner;
        // CanCommitToTransform : 이 클라이언트가 트랜스폼을 서버에 보내도 되는지 여부
        // IsOwner : 현재 클라이언트가 이 오브젝트의 소유자인지? true/false
        // true:내 캐릭터나 로컬 오브젝트 등 내가 소유자(Owner)일때만 오브젝트 트랜스 폼을 동기화 할 수 있도록 설정.
        // false:타인의 캐릭터나 공용 오브젝트 등은 내 소유가 아님.(서버가 보내주는 동기화 정보만 수신해서 반영.)
        // 즉, 오너만 수정(전송)가능, 나머지는 트랜스폼 수신만.
    }

    protected override void Update()
    {
        //매 프레임마다 소유 여부를 재확인, true일때만 업데이트가능
        CanCommitToTransform = IsOwner;
        base.Update();

        //싱글톤 조건에서 업데이트 진행 (네트워크매니저 인스턴스가 준비가 되어있을때만 커밋)
        if (NetworkManager != null)
        {
            //네트워크매니저가 클라이언트와 연결중이거나 서버구동중일때, 즉, 통신이 가능한 상태에서만 커밋
            if (NetworkManager.IsConnectedClient || NetworkManager.IsListening)
            {
                if (CanCommitToTransform) //내가 이 오브젝트의 소유자이면
                {
                    //클라이언트 트랜스폼 정보와 로컬 타임을 전송 시도.
                    //Try가 붙은 이유 - 기존과 변동된 점이 없으면 전송하지 않고 대역폭을 아낌
                    TryCommitTransformToServer(transform,NetworkManager.LocalTime.Time);
                }
            }
        }
    }
}
