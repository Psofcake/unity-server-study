using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ConnectServer : MonoBehaviour
{
    public void ConnectedToServer()
    {
        NetworkManager.Singleton.StartClient();
    }
}
