using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;


public static class AuthenticateWrapper
{
    public static AuthState authState { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> DoAuth(int triedCount = 5)
    {
        if (authState == AuthState.Authenticated) return authState; //이미 인증이 되어있으면 아래 과정 스킵

        if (authState == AuthState.Authenticating)
        {
            //인증 중일때 대기
            Debug.LogWarning("이미 인증 중입니다.");
            await Authenticating();
            return authState;
        }
        
        await SignInAnonymousAsync(triedCount);
        
        return authState;
    }

    private static async Task<AuthState> Authenticating()
    {
        while(authState == AuthState.Authenticating||authState == AuthState.NotAuthenticated) await Task.Delay(300);
        return authState;
    }

    private static async Task SignInAnonymousAsync(int triedCount)
    {
        authState = AuthState.Authenticating;   //인증 진행중으로 상태 변경.
        int count = 0;

        while (authState == AuthState.Authenticating && count < triedCount)
        {
            /* UGS를 통한 인증 과정 */
            try
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync(); //Unity의 AuthenticationService를 이용해 익명 로그인을 시도. try-catch 필요
                //await AuthenticationService.Instance.SignInWith

                if (AuthenticationService.Instance.IsSignedIn &&
                    AuthenticationService.Instance
                        .IsAuthorized) //인증 성공 && UGS에 대해 유효한 액세스 토큰을 가지고 있는지(서버와 통신 가능한 인증상태) = 사용자 세션이 완전히 유효한지 더블체크
                {
                    //Debug.Log("Auth Success");
                    authState = AuthState.Authenticated;
                    break;
                }
            }
            catch (AuthenticationException e)
            {
                Debug.LogError(e.Message);
                authState = AuthState.Failed;
            }
            catch (RequestFailedException e)
            {
                Debug.LogError(e.Message);
                authState = AuthState.Failed;
            }

            count++;
            await Task.Delay(1000);
        }
        //모든 시도가 실패
        if (authState != AuthState.Authenticated)
        {
            Debug.LogWarning($"플레이어 인증 실패: {count} 번 시도.");
            authState = AuthState.Timeout; 
        }
    }
}

public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Failed,
    Timeout
}