using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;


public static class AuthenticateWrapper
{
    public static AuthState authState { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> DoAuth(int triedCount = 5)
    {
        if (authState == AuthState.Authenticated) return authState; //이미 인증이 되어있으면 아래 과정 스킵

        authState = AuthState.Authenticating;   //인증 진행중으로 상태 변경.
        int count = 0;

        while (authState == AuthState.Authenticating && count < triedCount)
        {
            /* UGS를 통한 인증 과정 */
            await AuthenticationService.Instance.SignInAnonymouslyAsync();  //Unity의 AuthenticationService를 이용해 익명 로그인을 시도. try-catch추가 필요
            //await AuthenticationService.Instance.SignInWith
            if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)   //인증 성공 && UGS에 대해 유효한 액세스 토큰을 가지고 있는지(서버와 통신 가능한 인증상태) = 사용자 세션이 완전히 유효한지 더블체크
            {
                //Debug.Log("Auth Success");
                authState = AuthState.Authenticated;
                break;
            }

            count++;
            await Task.Delay(1000);
        }

        return authState;
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