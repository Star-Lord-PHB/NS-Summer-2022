using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Math;


public class User : MonoBehaviour
{

    [HideInInspector]
    public double x_calculated = 0;
    [HideInInspector]
    public double y_calculated = 0;
    [HideInInspector]
    public double height_calculated = 0;
    [HideInInspector]
    public int floorNum_calcualted = 1;

    [HideInInspector]
    private Socket socket;
    [HideInInspector]
    private Vector3 dest;
    [HideInInspector]
    private Utils.PathResponse lastPathResponse = null;

    // some predefined properties used only for the demo
    [HideInInspector]
    private int max_sensor_distance = 15;
    [HideInInspector]
    private String serverIP = "127.0.0.1";
    [HideInInspector]
    private int serverPort = 8080;
    
    public float dest_x = 0;
    public float dest_y = 0;
    public float dest_height = 0;
    public String dest_mark = "";
    public int updateFrequency = 1;
    [HideInInspector]
    private DateTime lastUpdateTime;


    // Start is called before the first frame update
    void Start()
    {
        this.socket = Utils.connectToServer(this.serverIP, this.serverPort);
        if (socket == null) {
            Debug.Log("Fail to connect to server!");
            Utils.quit();
            return;
        }

        if (!this.dest_mark.Equals("")) {
            var obj = GameObject.Find("/LandMarks/" + dest_mark);
            if (obj == null) { 
                Debug.Log("The required destination mark does not exists! Using the coordinate instead!");
                this.dest = new Vector3(dest_x, dest_height, dest_y); 
            } else {
                var p = obj.transform.position;
                this.dest = new Vector3(p.x, p.y, p.z);
            }
        } else {
            this.dest = new Vector3(dest_x, dest_height, dest_y);
        }

        if (!sendNavigationRequest(socket, this.dest.x, this.dest.z, this.dest.y)) {
            Debug.Log("Fail to send navigation request to server!");
            Utils.quit();
        }

        this.lastUpdateTime = DateTime.Now;
    }



    // Update is called once per frame
    void Update()
    {
        if (DateTime.Now.Ticks - this.lastUpdateTime.Ticks < (1.0 / this.updateFrequency) * 10000000L) { return; }

        Utils.PathResponse path = null;
        try {
            path = getPathFromServer(this.socket, createJsonMessage(filterCurrentFloor(linkSensors())));
        } catch (Exception err) {
            Debug.Log(err);
            this.socket.Close();
            Utils.quit();
        }
        if (path == null) {
            Debug.Log("Reached!");
            this.socket.Close();
            Utils.quit();
            return;
        }

        if (!path.Equals(this.lastPathResponse)) {
            var actual_position = gameObject.transform.position;
            Debug.Log("calculated position: " + path.position + " -- actual: (x=" + actual_position.x + ", y=" + actual_position.z + ", height=" + actual_position.y + ")");
            Debug.Log(path);
            this.lastPathResponse = path;
        }

        drawPath(path);

        this.lastUpdateTime = DateTime.Now;
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

        var counter = new Dictionary<int, double>();
        var sensorList = new List<GameObject>(sensors);
        var result = new List<GameObject>();

        sensorList.Sort(compareByDistanceToUser);
        // var testMessage = "";
        // foreach (var sensor in sensorList) {
        //     testMessage += (sensor.name + "   ");
        // }
        // Debug.Log(testMessage);

        var i = 0;
        foreach (GameObject sensor in sensorList) {
            if (i > 5) {break;}
            counter.TryAdd(sensor.GetComponent<Sensor>().floorNum, 0);
            counter[sensor.GetComponent<Sensor>().floorNum] += 1 / Utils.directDistance(gameObject, sensor);
            i++;
        }

        var floorNum = 1;
        var count = 0.0;
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



    private int compareByDistanceToUser(GameObject obj1, GameObject obj2) {
        return (int) Ceiling(Utils.directDistance(gameObject, obj1) - Utils.directDistance(gameObject, obj2));
    }



    private String createJsonMessage(GameObject[] sensors) {

        // example: {"Sensor_LivingRoom_1": 5.1, "Sensor_LivingRoom_2": 3.4, ...}

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



    private Utils.PathResponse getPathFromServer(Socket socket, String message) {

        var message_byte = Encoding.UTF8.GetBytes(message);
        socket.Send(message_byte, message_byte.Length, 0);

        var response_byte = getBytesFromServer(0, socket);
        if (response_byte == null) { throw new Exception("fail to get path from server"); }

        if (Utils.isReached(response_byte)) {
            return null;
        }

        return Utils.parsePathResponse(response_byte);

    }



    private void drawPath(Utils.PathResponse path) {

        var lineRenderer = GameObject.Find("/UserContex/Line").GetComponent<LineRenderer>();
        lineRenderer.positionCount = path.pathLength();
        lineRenderer.SetPositions(path.getVector3Path());

    }



    private byte[] getBytesFromServer(int size, Socket socket, int timeOut = 3) {

        socket.ReceiveTimeout = timeOut * 1000;

        if (size > 0) {    

            var oneByte = new byte[1];
            var byteBuffer = new byte[size];

            for (int i = 0; i < size; i++) {
                var startTime = DateTime.Now;
                try {
                    socket.Receive(oneByte, 1, 0);
                } catch (SocketException err) {
                    Debug.Log("Receive time out!");
                    return null;
                }
                byteBuffer[i] = oneByte[0];
            }

            return byteBuffer;

        } else {

            var byteBuffer = new byte[1024];
            var result = new List<byte>();
            var length = 0;

            while (true) {

                try { length = socket.Receive(byteBuffer, byteBuffer.Length, 0); } 
                catch (SocketException err) {
                    if (result.Count == 0) {
                        Debug.Log("Receive time out!");
                        return null;
                    }
                    break;
                }

                for (int i = 0; i < length; i++) {
                    result.Add(byteBuffer[i]);
                }

                if (length < byteBuffer.Length) {
                    break;
                }

            }

            // while (true) {
            //     if (new TimeSpan(DateTime.Now.Ticks - startTime.Ticks).TotalSeconds > timeOut) { return null; }
            //     length = socket.Receive(byteBuffer);
            //     if (length != 0 || result.Count != 0) {
            //         for (int i = 0; i < length; i++) {
            //             result.Add(byteBuffer[i]);
            //         }
            //         break;
            //     }
            // }

            // while (length == byteBuffer.Length) {
            //     length = socket.Receive(byteBuffer);
            //     for (int i = 0; i < length; i++) {
            //         result.Add(byteBuffer[i]);
            //     }
            // }

            return result.ToArray();

        }

    }

}
