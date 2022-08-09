using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;


public class User : MonoBehaviour
{

    [HideInInspector]
    public double x_calculated = 0;
    [HideInInspector]
    public double y_calculated = 0;
    [HideInInspector]
    public double height_calculated = 0;
    [HideInInspector]
    public double floorNum_calcualted = 1;

    // some predefined properties used only for the demo
    [HideInInspector]
    private int max_sensor_distance = 15;
    [HideInInspector]
    private String serverIP = "127.0.0.1";
    [HideInInspector]
    private int serverPort = 8080;
    [HideInInspector]
    private int updateCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        var socket = Utils.connectToServer(this.serverIP, this.serverPort);
        if (socket == null) {
            Debug.Log("Fail to connect to server!");
            return;
        }

        var testMark = GameObject.Find("/LandMarks/mark_Entry_3").transform.position;

        if (!sendNavigationRequest(socket, testMark.x, testMark.z, testMark.y)) {
            Debug.Log("Fail to send navigation request to server!");
            return;
        }

        getPathFromServer(socket, createJsonMessage(filterCurrentFloor(linkSensors())));

        socket.Close();
    }



    // Update is called once per frame
    void Update()
    {
        // var socket = Utils.connectToServer(this.serverIP, this.serverPort);
        // if (socket == null) {
        //     Debug.Log("Fail to connect to server!");
        //     return;
        // }

        // var testMark = GameObject.Find("/LandMarks/mark_Kitchen_3").transform.position;

        // if (!sendNavigationRequest(socket, testMark.x, testMark.z, testMark.y)) {
        //     Debug.Log("Fail to send navigation request to server!");
        //     return;
        // }

        // getPathFromServer(socket, createJsonMessage(filterCurrentFloor(linkSensors())));

        // socket.Close();
    }



    private bool sendNavigationRequest(Socket socket, double x, double y, double height) {
        var message_byte = Encoding.UTF8.GetBytes("{\"x\": " + x + ", \"y\": " + y + ", \"height\": " + height + "}");
        socket.Send(message_byte, message_byte.Length, 0);
        return Utils.acknowledge(socket);
    }



    private GameObject[] linkSensors() {

        var result = new List<GameObject>();
        
        var sensorList = GameObject.FindGameObjectsWithTag("sensor");
        int count = 0;

        for (int i = 0; i < sensorList.Length; i++) {

            var sensor = sensorList[i];
            if (Utils.directDistance(gameObject, sensor) <= this.max_sensor_distance) {
                // Debug.Log("sensor " + sensor.name + " --> (" + Utils.directDistance(gameObject, sensor) + ", " + sensor.GetComponent<Sensor>().height + ", " + sensor.GetComponent<Sensor>().floorNum + ")");
                result.Add(sensor);
                count++;
            }

        }
        
        return result.ToArray();

    }


    /**
     * 
     */
    private GameObject[] filterCurrentFloor(GameObject[] sensors) {

        var counter = new Dictionary<int, int>();
        var result = new List<GameObject>();

        foreach (GameObject sensor in sensors) {
            counter.TryAdd(sensor.GetComponent<Sensor>().floorNum, 0);
            counter[sensor.GetComponent<Sensor>().floorNum] += 1;
        }

        var floorNum = 1;
        var count = 0;
        foreach (var pair in counter) {
            if (pair.Value > count) {
                count = pair.Value;
                floorNum = pair.Key;
            }
        }

        this.floorNum_calcualted = floorNum;

        foreach (GameObject sensor in sensors) {
            if (sensor.GetComponent<Sensor>().floorNum == floorNum) {
                result.Add(sensor);
            }
        }

        // if (updateCount == 0) {
        //     foreach (GameObject sensor in result) {
        //         Debug.Log(sensor.name + " --> " + Utils.directDistance(gameObject, sensor));
        //     }
        // }

        return result.ToArray();

    }



    private String createJsonMessage(GameObject[] sensors) {

        var message = "{";

        for (int i = 0; i < sensors.Length; i++) {
            message += ("\"" + sensors[i].name + "\"");
            message += ": ";
            message += Utils.directDistance(sensors[i], gameObject);
            if (i != sensors.Length - 1) { message += ", "; }
        }
        message += "}";

        return message;

    }



    private void getPathFromServer(Socket socket, String message) {

        var message_byte = Encoding.UTF8.GetBytes(message);
        socket.Send(message_byte, message_byte.Length, 0);

        Debug.Log("test");

        var response_byte = getBytesFromServer(0, socket);
        if (response_byte == null) { return; }

        Debug.Log(Encoding.UTF8.GetString(response_byte));

        var path = Utils.parsePathResponse(response_byte);

        var lineRenderer = GameObject.Find("/UserContex/Line").GetComponent<LineRenderer>();
        lineRenderer.positionCount = path.pathLength();
        lineRenderer.SetPositions(path.getVector3Path());

    }



    private byte[] getBytesFromServer(int size, Socket socket, int timeOut = 3) {

        if (size > 0) {    

            var oneByte = new byte[1];
            var byteBuffer = new byte[size];

            for (int i = 0; i < size; i++) {
                var startTime = DateTime.Now;
                while (socket.Receive(oneByte, 1, 0) == 0) {
                    var currentTime = DateTime.Now;
                    if (new TimeSpan(currentTime.Ticks - startTime.Ticks).TotalSeconds > timeOut) { return null; }
                }
                byteBuffer[i] = oneByte[0];
            }

            return byteBuffer;

        } else {

            var byteBuffer = new byte[1024];
            var result = new List<byte>();
            var length = 0;

            var startTime = DateTime.Now;
            while (true) {
                if (new TimeSpan(DateTime.Now.Ticks - startTime.Ticks).TotalSeconds > timeOut) { return null; }
                length = socket.Receive(byteBuffer);
                if (length != 0 || result.Count != 0) {
                    for (int i = 0; i < length; i++) {
                        result.Add(byteBuffer[i]);
                    }
                    break;
                }
            }

            while (length == byteBuffer.Length) {
                length = socket.Receive(byteBuffer);
                for (int i = 0; i < length; i++) {
                    result.Add(byteBuffer[i]);
                }
            }

            return result.ToArray();

        }

    }



    private void makeCompare() {

        var actialPosition = gameObject.transform.position;

        Debug.Log("calculated position: (x=" + this.x_calculated + ", y=" + this.y_calculated + ", height=" + this.height_calculated + ", floor=" + this.floorNum_calcualted + ")");
        Debug.Log("actual position: (x=" + actialPosition.x + ", y=" + actialPosition.z + ", height=" + actialPosition.y + ", floor=<check it yourselves!>" + ")");

    }

}
