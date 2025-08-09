using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HostSingleton : MonoBehaviour
{
    public HostGameManager hostGameManager;
    
    private static HostSingleton instance;
    public static HostSingleton Instance
    {
        get
        {
            if (instance != null) return instance;
            
            instance = FindObjectOfType<HostSingleton>();
            if (instance == null)
            {
                Debug.LogError("<HostSingleton> instance not found>");
                return null;
            }
            
            return instance;
        }
    }

    //클라이언트 생성
    public void CreateHost()    //호스트는 클라이언트처럼 어싱크가 필요없다.
    {
        hostGameManager = new HostGameManager();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
