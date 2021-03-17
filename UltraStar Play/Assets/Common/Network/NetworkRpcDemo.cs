using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRpcDemo : NetworkedBehaviour
{
    public override void NetworkStart()
    {
        Debug.Log("NetworkStart");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("SendRandomInt"))
        {
            if (IsServer)
            {
                InvokeClientRpcOnEveryone(MyClientRPC, Random.Range(-50, 50));
            }
            else
            {
                InvokeServerRpc(MyServerRPC, Random.Range(-50, 50));
            }
        }
    }

    [ServerRPC]
    private void MyServerRPC(int number)
    {
        Debug.Log("The number received was: " + number);
        Debug.Log("This method ran on the server upon the request of a client");
    }

    [ClientRPC]
    private void MyClientRPC(int number)
    {
        Debug.Log("The number received was: " + number);
        Debug.Log("This method ran on the client upon the request of the server");
    }
}
