using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math;
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
        // getPositionFromServer(createJsonMessage(filterCurrentFloor(linkSensors())));
        // makeCompare();
    }



    // Update is called once per frame
    void Update()
    {
        getPositionFromServer(createJsonMessage(filterCurrentFloor(linkSensors())));
        if (updateCount == 0) {
            makeCompare();
        }
        updateCount++;
    }



    private GameObject[] linkSensors() {

        var result = new List<GameObject>();
        
        var sensorList = GameObject.FindGameObjectsWithTag("sensor");
        int count = 0;

        for (int i = 0; i < sensorList.Length; i++) {

            var sensor = sensorList[i];
            if (directDistance(gameObject, sensor) <= this.max_sensor_distance) {
                // Debug.Log("sensor " + sensor.name + " --> (" + directDistance(gameObject, sensor) + ", " + sensor.GetComponent<MyVariable>().height + ", " + sensor.GetComponent<MyVariable>().floorNum + ")");
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
            counter.TryAdd(sensor.GetComponent<MyVariable>().floorNum, 0);
            counter[sensor.GetComponent<MyVariable>().floorNum] += 1;
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
            if (sensor.GetComponent<MyVariable>().floorNum == floorNum) {
                result.Add(sensor);
            }
        }

        if (updateCount == 0) {
            foreach (GameObject sensor in result) {
                Debug.Log(sensor.name + " --> " + directDistance(gameObject, sensor));
            }
        }

        return result.ToArray();

    }



    private String createJsonMessage(GameObject[] sensors) {

        var message = "{";

        for (int i = 0; i < sensors.Length; i++) {
            message += ("\"" + sensors[i].name + "\"");
            message += ": ";
            message += directDistance(sensors[i], gameObject);
            if (i != sensors.Length - 1) { message += ", "; }
        }
        message += "}";

        return message;

    }



    private void getPositionFromServer(String message) {

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipe = new IPEndPoint(IPAddress.Parse(this.serverIP), this.serverPort);

        if (!connectServer(socket, ipe, 5)) {
            Debug.Log("fail to connect to server");
            return;
        }

        var message_byte = Encoding.UTF8.GetBytes(message);
        socket.Send(message_byte, message_byte.Length, 0);

        var response_byte = getBytesFromServer(24, socket);
        if (response_byte == null) { return; }

        this.x_calculated = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(response_byte, 0)); 
        this.y_calculated = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(response_byte, 8)); 
        this.height_calculated = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(response_byte, 16));

        socket.Close();

    }



    private byte[] getBytesFromServer(int size, Socket socket) {

        var oneByte = new byte[1];
        var byteBuffer = new byte[size];
        var timeOut = 5;

        for (int i = 0; i < size; i++) {
            var startTime = DateTime.Now;
            while (socket.Receive(oneByte, 1, 0) == 0) {
                var currentTime = DateTime.Now;
                if (new TimeSpan(currentTime.Ticks - startTime.Ticks).TotalSeconds > timeOut) { return null; }
            }
            byteBuffer[i] = oneByte[0];
        }

        return byteBuffer;

    }


    private bool connectServer(Socket socket, IPEndPoint ipe, int timeOut) {

        var startTime = DateTime.Now;

        while (new TimeSpan(DateTime.Now.Ticks - startTime.Ticks).TotalSeconds < timeOut) {

            try {
                socket.Connect(ipe);
                return true;
            } catch (System.Exception) {
                continue;
            }

        }

        return false;

    }



    private double directDistance(GameObject obj1, GameObject obj2) {

        var position1 = obj1.transform.position;
        var position2 = obj2.transform.position;

        return Sqrt(Pow(position1.x - position2.x, 2) 
                    + Pow(position1.y - position2.y, 2) 
                    + Pow(position1.z - position2.z, 2));

    }



    private void makeCompare() {

        var actialPosition = gameObject.transform.position;

        Debug.Log("calculated position: (x=" + this.x_calculated + ", y=" + this.y_calculated + ", height=" + this.height_calculated + ", floor=" + this.floorNum_calcualted + ")");
        Debug.Log("actual position: (x=" + actialPosition.x + ", y=" + actialPosition.z + ", height=" + actialPosition.y + ", floor=<check it yourselves!>" + ")");

    }

}
