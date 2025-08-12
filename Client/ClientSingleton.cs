using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClientSingleton : MonoBehaviour
{
    public ClientGameManager clientGameManager{get; private set;}
    
    private static ClientSingleton instance;
    public static ClientSingleton Instance
    {
        get
        {
            if (instance != null) return instance;
            
            instance = FindObjectOfType<ClientSingleton>();
            if (instance == null)
            {
                Debug.LogError("<ClientSingleton> instance not found>");
                return null;
            }
            
            return instance;
        }
    }

    //클라이언트 생성
    public async Task<bool> CreateClient()
    {
        clientGameManager = new ClientGameManager();
        return await clientGameManager.InitAsync();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
