using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;

public class ClientGameManager : MonoBehaviour
{
	private const string MenuSceneName = "Start";

    public async Task<bool> InitAsync()
    {
		await UnityServices.InitializeAsync();	//UGS의 핵심 기능을 사용하기 전에 가장 먼저 이 메서드를 호출하여 초기화 작업을 수행해야 한다.
		/* UnityServices.InitializeAsync가 수행하는 작업:
		1. Unity 프로젝트와 연결된 환경(environment)을 확인
		2. 로컬 설정(Unity 프로젝트 설정)과 클라우드 환경을 동기화
		3. Unity Gaming Services의 모듈들(Authentication, Cloud Save 등)을 초기화
		4. 기본 사용자 세션 준비	*/
		
        //플레이어 인증 처리
		AuthState authState = await AuthenticateWrapper.DoAuth();
		if (authState == AuthState.Authenticated){
			Debug.Log("Auth Success");
			return true;
		}
        
		Debug.Log("Auth Fail");
		return false;
    }
    
	public void StartMenu(){
		SceneManager.LoadScene(MenuSceneName);
	}
}
