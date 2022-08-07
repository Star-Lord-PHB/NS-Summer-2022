using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;


public class SceneInitializer : MonoBehaviour
{

    [HideInInspector]
    private String serverIP = "127.0.0.1";
    [HideInInspector]
    private int serverPort = 8081;

    // Start is called before the first frame update
    void Start()
    {
        var sensorList = GameObject.FindGameObjectsWithTag("sensor");

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipe = new IPEndPoint(IPAddress.Parse(this.serverIP), this.serverPort);

        try { socket.Connect(ipe); } 
        catch (System.Exception) { 
            socket.Close();
            return;
        }

        var message_byte = Encoding.UTF8.GetBytes(buildMessage(sensorList));
        socket.Send(message_byte, message_byte.Length, 0);

        socket.Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    private String buildMessage(GameObject[] sensorList) {

        var message = "{";

        for (int i = 0; i < sensorList.Length; i++) {
            message += ("\"" + sensorList[i].name + "\": {");
            message += ("\"x\": " + sensorList[i].transform.position.x + ", ");
            message += ("\"y\": " + sensorList[i].transform.position.z + ", ");
            message += ("\"height\": " + sensorList[i].transform.position.y + "}");
            if (i != sensorList.Length - 1) { message += ", "; }
        }

        message += "}";

        return message;

    }
}
