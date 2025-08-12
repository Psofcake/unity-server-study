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
