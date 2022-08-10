using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Linq;


/**
 * The script that will run at the very begining to init all the necessary info on the server 
 * In reality the server will hold all those info from the blue print of the building
 * However in this demo, data are stored in unity locally, so we have to send them to the server first
 */
public class SceneInitializer : MonoBehaviour
{
    [HideInInspector]
    private String serverIP = "127.0.0.1";
    [HideInInspector]
    private int serverPort = 8081;


    // Start is called before the first frame update
    void Start()
    {
        // connect to the server
        var socket = Utils.connectToServer(this.serverIP, this.serverPort, 1);
        if (socket == null) {
            Debug.Log("fail to connect to server for initialization, skipping this part...");
            return;
        }

        // init the information of height of each floor 
        if (!initFloorHeight(socket)) {
            Debug.Log("fail to init the floor height info of the building in the server");
            socket.Close();
            Utils.quit();
            return;
        }

        // init all the infomation of the sensors 
        if (!initSensors(socket)) {
            Debug.Log("fail to init sensor data in the server!");
            socket.Close();
            Utils.quit();
            return;
        }

        // init all map of all the land marks 
        if (!initLandMarkMap(socket)) {
            Debug.Log("fail to init land mark map in the server!");
            socket.Close();
            Utils.quit();
            return;
        }

        // init the information of all the land marks
        if (!initLandMarkInfo(socket)) {
            Debug.Log("fail to init the land mark info in the server!");
            socket.Close();
            Utils.quit();
            return;
        }

        socket.Close();
    }



    // Update is called once per frame
    void Update()
    {
        
    }



    private bool initFloorHeight(Socket socket) {

        // example: {"1": 1.0, "2": 5.2, ...}

        var floorHeightMessage = "{\"1\": " 
                                + GameObject.Find("/LandMarks/mark_Stair_1").transform.position.y 
                                + ",\"2\": " 
                                + GameObject.Find("/LandMarks/mark_Stair_3").transform.position.y
                                + "}";
        var message_byte = Encoding.UTF8.GetBytes(floorHeightMessage);

        socket.Send(message_byte, message_byte.Length, 0);

        return Utils.acknowledge(socket);
        
    }



    private bool initSensors(Socket socket) {

        var sensorList = GameObject.FindGameObjectsWithTag("sensor");
        var timeOut = 5;

        var message_byte = Encoding.UTF8.GetBytes(buildSensorsMessage(sensorList));
        socket.Send(message_byte, message_byte.Length, 0);

        return Utils.acknowledge(socket);

    }



    private String buildSensorsMessage(GameObject[] sensorList) {

        /* example: 
           {
                "Sensor_LivingRoom_1": {"x": 1.1, "y": 2.2, "height": 3.3, "floor": 1}, 
                "Sensor_LivingRoom_2": {"x": 1.2, "y": 2.3, "height": 3.4, "floor": 1}
            }
         */

        var message = "{";

        for (int i = 0; i < sensorList.Length; i++) {
            message += ("\"" + sensorList[i].name + "\": {");
            message += ("\"x\": " + sensorList[i].transform.position.x + ", ");
            message += ("\"y\": " + sensorList[i].transform.position.z + ", ");
            message += ("\"height\": " + sensorList[i].transform.position.y + ", ");
            message += ("\"floor\": " + sensorList[i].GetComponent<Sensor>().floorNum + "}");
            if (i != sensorList.Length - 1) { message += ", "; }
        }

        message += "}";

        return message;

    }



    private bool initLandMarkMap(Socket socket) {

        var message_byte = Encoding.UTF8.GetBytes(buildMap());
        var timeOut = 5;

        socket.Send(message_byte, message_byte.Length, 0);

        return Utils.acknowledge(socket);

    }



    private String buildMap() {

        var marks = GameObject.FindGameObjectsWithTag("mark");

        var marks_dict = new Dictionary<String, GameObject>();
        foreach (GameObject mark in marks) {
            marks_dict.Add(mark.name, mark);
        }

        return @"
        {""mark_LivingRoom_1"": {""mark_LivingRoom_2"": " + Utils.directDistance(marks_dict["mark_LivingRoom_1"], marks_dict["mark_LivingRoom_2"]) + @", ""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_LivingRoom_1"], marks_dict["mark_LivingRoom_4"]) + @", ""mark_LivingRoom_7"": " + Utils.directDistance(marks_dict["mark_LivingRoom_1"], marks_dict["mark_LivingRoom_7"]) + @"},
        ""mark_LivingRoom_2"": {""mark_LivingRoom_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_2"], marks_dict["mark_LivingRoom_1"]) + @", ""mark_LivingRoom_3"": " + Utils.directDistance(marks_dict["mark_LivingRoom_2"], marks_dict["mark_LivingRoom_3"]) + @"},
        ""mark_LivingRoom_3"": {""mark_LivingRoom_2"": " + Utils.directDistance(marks_dict["mark_LivingRoom_3"], marks_dict["mark_LivingRoom_2"]) + @", ""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_LivingRoom_3"], marks_dict["mark_LivingRoom_4"]) + @"},
        ""mark_LivingRoom_4"": {""mark_LivingRoom_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_LivingRoom_1"]) + @", ""mark_LivingRoom_3"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_LivingRoom_3"]) + @", ""mark_LivingRoom_5"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_LivingRoom_5"]) + @", ""mark_LivingRoom_6"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_LivingRoom_6"]) + @", ""mark_LivingRoom_7"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_LivingRoom_7"]) + @", ""mark_Stair_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_4"], marks_dict["mark_Stair_1"]) + @"},
        ""mark_LivingRoom_5"": {""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_LivingRoom_5"], marks_dict["mark_LivingRoom_4"]) + @", ""mark_LivingRoom_8"": " + Utils.directDistance(marks_dict["mark_LivingRoom_5"], marks_dict["mark_LivingRoom_8"]) + @", ""mark_LivingRoom_6"": " + Utils.directDistance(marks_dict["mark_LivingRoom_6"], marks_dict["mark_LivingRoom_5"]) + @"},
        ""mark_LivingRoom_6"": {""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_LivingRoom_6"], marks_dict["mark_LivingRoom_4"]) + @", ""mark_Kitchen_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_6"], marks_dict["mark_Kitchen_1"]) + @", ""mark_Stair_1"": " + Utils.directDistance(marks_dict["mark_Stair_1"], marks_dict["mark_LivingRoom_6"]) + @", ""mark_LivingRoom_5"": " + Utils.directDistance(marks_dict["mark_LivingRoom_5"], marks_dict["mark_LivingRoom_6"]) + @"},
        ""mark_LivingRoom_7"": {""mark_LivingRoom_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_7"], marks_dict["mark_LivingRoom_1"]) + @", ""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_LivingRoom_7"], marks_dict["mark_LivingRoom_4"]) + @", ""mark_Entry_1"": " + Utils.directDistance(marks_dict["mark_LivingRoom_7"], marks_dict["mark_Entry_1"]) + @"},
        ""mark_LivingRoom_8"": {""mark_LivingRoom_5"": " + Utils.directDistance(marks_dict["mark_LivingRoom_8"], marks_dict["mark_LivingRoom_5"]) + @", ""mark_LivingRoom_9"": " + Utils.directDistance(marks_dict["mark_LivingRoom_8"], marks_dict["mark_LivingRoom_9"]) + @"},
        ""mark_LivingRoom_9"": {""mark_LivingRoom_8"": " + Utils.directDistance(marks_dict["mark_LivingRoom_9"], marks_dict["mark_LivingRoom_8"]) + @", ""mark_LivingRoom_10"": " + Utils.directDistance(marks_dict["mark_LivingRoom_9"], marks_dict["mark_LivingRoom_10"]) + @"},
        ""mark_LivingRoom_10"": {""mark_LivingRoom_9"": " + Utils.directDistance(marks_dict["mark_LivingRoom_10"], marks_dict["mark_LivingRoom_9"]) + @"},
        ""mark_Kitchen_1"": {""mark_LivingRoom_6"": " + Utils.directDistance(marks_dict["mark_Kitchen_1"], marks_dict["mark_LivingRoom_6"]) + @", ""mark_Entry_2"": " + Utils.directDistance(marks_dict["mark_Kitchen_1"], marks_dict["mark_Entry_2"]) + @", ""mark_Kitchen_2"": " + Utils.directDistance(marks_dict["mark_Kitchen_1"], marks_dict["mark_Kitchen_2"]) + @", ""mark_Kitchen_3"": " + Utils.directDistance(marks_dict["mark_Kitchen_1"], marks_dict["mark_Kitchen_3"]) + @"},
        ""mark_Kitchen_2"": {""mark_Kitchen_1"": " + Utils.directDistance(marks_dict["mark_Kitchen_2"], marks_dict["mark_Kitchen_1"]) + @", ""mark_Kitchen_3"": " + Utils.directDistance(marks_dict["mark_Kitchen_2"], marks_dict["mark_Kitchen_3"]) + @"},
        ""mark_Kitchen_3"": {""mark_Kitchen_1"": " + Utils.directDistance(marks_dict["mark_Kitchen_3"], marks_dict["mark_Kitchen_1"]) + @", ""mark_Kitchen_2"": " + Utils.directDistance(marks_dict["mark_Kitchen_3"], marks_dict["mark_Kitchen_2"]) + @"},
        ""mark_Entry_1"": {""mark_LivingRoom_7"": " + Utils.directDistance(marks_dict["mark_Entry_1"], marks_dict["mark_LivingRoom_7"]) + @", ""mark_Entry_2"": " + Utils.directDistance(marks_dict["mark_Entry_1"], marks_dict["mark_Entry_2"]) + @"},
        ""mark_Entry_2"": {""mark_Entry_1"": " + Utils.directDistance(marks_dict["mark_Entry_2"], marks_dict["mark_Entry_1"]) + @", ""mark_Kitchen_1"": " + Utils.directDistance(marks_dict["mark_Entry_2"], marks_dict["mark_Kitchen_1"]) + @", ""mark_Entry_3"": " + Utils.directDistance(marks_dict["mark_Entry_2"], marks_dict["mark_Entry_3"]) + @"},
        ""mark_Entry_3"": {""mark_Entry_2"": " + Utils.directDistance(marks_dict["mark_Entry_3"], marks_dict["mark_Entry_2"]) + @"},
        ""mark_Stair_1"": {""mark_LivingRoom_4"": " + Utils.directDistance(marks_dict["mark_Stair_1"], marks_dict["mark_LivingRoom_4"]) + @", ""mark_LivingRoom_6"": " + Utils.directDistance(marks_dict["mark_Stair_1"], marks_dict["mark_LivingRoom_6"]) + @", ""mark_Stair_2"": " + Utils.directDistance(marks_dict["mark_Stair_1"], marks_dict["mark_Stair_2"]) + @"},
        ""mark_Stair_2"": {""mark_Stair_1"": " + Utils.directDistance(marks_dict["mark_Stair_2"], marks_dict["mark_Stair_1"]) + @", ""mark_Stair_3"": " + Utils.directDistance(marks_dict["mark_Stair_2"], marks_dict["mark_Stair_3"]) + @"},
        ""mark_Stair_3"": {""mark_Stair_2"": " + Utils.directDistance(marks_dict["mark_Stair_3"], marks_dict["mark_Stair_2"]) + @", ""mark_BedRoom2_1"": " + Utils.directDistance(marks_dict["mark_Stair_3"], marks_dict["mark_BedRoom2_1"]) + @", ""mark_BedRoom1_1"": " + Utils.directDistance(marks_dict["mark_Stair_3"], marks_dict["mark_BedRoom1_1"]) + @"},
        ""mark_BedRoom2_1"": {""mark_Stair_3"": " + Utils.directDistance(marks_dict["mark_BedRoom2_1"], marks_dict["mark_Stair_3"]) + @", ""mark_BedRoom2_2"": " + Utils.directDistance(marks_dict["mark_BedRoom2_1"], marks_dict["mark_BedRoom2_2"]) + @"},
        ""mark_BedRoom2_2"": {""mark_BedRoom2_1"": " + Utils.directDistance(marks_dict["mark_BedRoom2_2"], marks_dict["mark_BedRoom2_1"]) + @", ""mark_BedRoom2_3"": " + Utils.directDistance(marks_dict["mark_BedRoom2_2"], marks_dict["mark_BedRoom2_3"]) + @"},
        ""mark_BedRoom2_3"": {""mark_BedRoom2_2"": " + Utils.directDistance(marks_dict["mark_BedRoom2_3"], marks_dict["mark_BedRoom2_2"]) + @"},
        ""mark_BedRoom1_1"": {""mark_Stair_3"": " + Utils.directDistance(marks_dict["mark_BedRoom1_1"], marks_dict["mark_Stair_3"]) + @", ""mark_BedRoom1_2"": " + Utils.directDistance(marks_dict["mark_BedRoom1_1"], marks_dict["mark_BedRoom1_2"]) + @"},
        ""mark_BedRoom1_2"": {""mark_BedRoom1_1"": " + Utils.directDistance(marks_dict["mark_BedRoom1_2"], marks_dict["mark_BedRoom1_1"]) + @", ""mark_BedRoom1_3"": " + Utils.directDistance(marks_dict["mark_BedRoom1_2"], marks_dict["mark_BedRoom1_3"]) + @", ""mark_BedRoom1_4"": " + Utils.directDistance(marks_dict["mark_BedRoom1_2"], marks_dict["mark_BedRoom1_4"]) + @"},
        ""mark_BedRoom1_3"": {""mark_BedRoom1_2"": " + Utils.directDistance(marks_dict["mark_BedRoom1_3"], marks_dict["mark_BedRoom1_2"]) + @"},
        ""mark_BedRoom1_4"": {""mark_BedRoom1_2"": " + Utils.directDistance(marks_dict["mark_BedRoom1_4"], marks_dict["mark_BedRoom1_2"]) + @", ""mark_BedRoom1_5"": " + Utils.directDistance(marks_dict["mark_BedRoom1_4"], marks_dict["mark_BedRoom1_5"]) + @"},
        ""mark_BedRoom1_5"": {""mark_BedRoom1_4"": " + Utils.directDistance(marks_dict["mark_BedRoom1_5"], marks_dict["mark_BedRoom1_4"]) + @"}}
        ";

    }



    private bool initLandMarkInfo(Socket socket) {

        var marks = GameObject.FindGameObjectsWithTag("mark");
        var timeOut = 5;

        var message = "{";
        for (int i = 0; i < marks.Length; i++) {
            var markPosition = marks[i].transform.position;
            message += ("\"" + marks[i].name + "\"" + ": {\"x\": " + markPosition.x + ", \"y\": " + markPosition.z + ", \"height\": " + markPosition.y + "}");
            if (i != marks.Length - 1) { message += ","; }
        }
        message += "}";

        var message_byte = Encoding.UTF8.GetBytes(message);
        socket.Send(message_byte, message_byte.Length, 0);

        return Utils.acknowledge(socket);

    }
    
}
