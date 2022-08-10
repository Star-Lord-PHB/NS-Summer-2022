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

    /**
     * fields that store the calculation result from the server
     */
    [HideInInspector]
    public double x_calculated = 0;
    [HideInInspector]
    public double y_calculated = 0;
    [HideInInspector]
    public double height_calculated = 0;
    [HideInInspector]
    public int floorNum_calcualted = 1;

    /**
     * Some global fields for the User class
     * for holding data between each update
     */
    // hold the connection to the server during one navigation process 
    [HideInInspector]
    private Socket socket;
    // hold the destination during one navigation process       
    [HideInInspector]
    private Vector3 dest;       
    // hold the last path received from server so that the console log won't update if the User does not move 
    [HideInInspector]
    private Utils.PathResponse lastPathResponse = null;    
    // hold the last time we fetch path from server to control the update frequency 
    [HideInInspector]
    private DateTime lastUpdateTime;

    /**
     * Some predefined setting for the Demo 
     */
    // the maximum distance for a sensor to be linked to the User
    [HideInInspector]
    private int max_sensor_distance = 15;
    [HideInInspector]
    private String serverIP = "127.0.0.1";
    [HideInInspector]
    private int serverPort = 8080;
    
    /**
     * Setting exposed to user 
     */
    // the x coordinate of the destination
    public float dest_x = 0;
    // the y coordinate of the destination
    public float dest_y = 0;
    // the height coordinate of the destination
    public float dest_height = 0;
    // set the destination with the name of some predefined marks
    // if this field is set, it will ignore the coordinates before
    public String dest_mark = "";
    // the frequency for updating the path from server (times per second)
    public int updateFrequency = 1;


    // Start is called before the first frame update
    void Start()
    {
        // connect to the server
        this.socket = Utils.connectToServer(this.serverIP, this.serverPort);
        if (socket == null) {
            Debug.Log("Fail to connect to server!");
            Utils.quit();
            return;
        }

        // Assemble the destination info 
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

        // send the navigation request and the destination to the server 
        if (!sendNavigationRequest(socket, this.dest.x, this.dest.z, this.dest.y)) {
            Debug.Log("Fail to send navigation request to server!");
            Utils.quit();
            return;
        }

        this.lastUpdateTime = DateTime.Now;
    }



    // Update is called once per frame
    void Update()
    {
        // control the update frequency 
        if (DateTime.Now.Ticks - this.lastUpdateTime.Ticks < (1.0 / this.updateFrequency) * 10000000L) { return; }

        // fetch path from the server
        Utils.PathResponse path = null;
        try {
            path = getPathFromServer(this.socket, createJsonMessage(filterCurrentFloor(linkSensors())));
        } catch (Exception err) {
            Debug.Log(err);
            this.socket.Close();
            Utils.quit();
        }
        // if the response path is null, it means that the User has reached the distination 
        if (path == null) {
            Debug.Log("Reached!");
            this.socket.Close();
            Utils.quit();
            return;
        }

        // if the User does not move since last time the path is update, do nothing
        // otherwise, update the new calculated path & position info to the console 
        if (!path.Equals(this.lastPathResponse)) {
            var actual_position = gameObject.transform.position;
            Debug.Log("calculated position: " + path.position + " -- actual: (x=" + actual_position.x + ", y=" + actual_position.z + ", height=" + actual_position.y + ")");
            Debug.Log(path);
            this.lastPathResponse = path;
        }

        // draw the path as a white line in the unity scene
        drawPath(path);

        this.lastUpdateTime = DateTime.Now;
    }



    /**
     * Send the navigation request and destination info to the server
     * Once the server get this information, it will require distances info to different sensors to calculate the path
     * That means this function only need to be called once at the beginning of the navigation process 
     */
    private bool sendNavigationRequest(Socket socket, double x, double y, double height) {
        var message_byte = Encoding.UTF8.GetBytes("{\"x\": " + x + ", \"y\": " + y + ", \"height\": " + height + "}");
        socket.Send(message_byte, message_byte.Length, 0);
        return Utils.acknowledge(socket);
    }



    /**
     * get the surrounding sensors within the predefined maximum distance 
     */
    private GameObject[] linkSensors() {

        var result = new List<GameObject>();
        
        var sensorList = GameObject.FindGameObjectsWithTag("sensor");
        int count = 0;

        for (int i = 0; i < sensorList.Length; i++) {

            var sensor = sensorList[i];
            if (Utils.directDistance(gameObject, sensor) <= this.max_sensor_distance) {
                result.Add(sensor);
                count++;
            }

        }
        
        return result.ToArray();

    }


    /**
     * estimate the floor num of the User and filter out all the sensors of that floor
     * (we only calculate the position using the sensors of the same floor)
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

        return result.ToArray();

    }



    /**
     * A comparator for sorting the sensors with distance to User
     * mainly used in the `filterCurrentFloor` function 
     */
    private int compareByDistanceToUser(GameObject obj1, GameObject obj2) {
        return (int) Ceiling(Utils.directDistance(gameObject, obj1) - Utils.directDistance(gameObject, obj2));
    }



    /**
     * build the json message that will be send to the server to calculate the position and the path
     * the format is {"SensorID": distanceToUser, ...}
     * example: {"Sensor_LivingRoom_1": 5.1, "Sensor_LivingRoom_2": 3.4, ...}
     */
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



    /**
     * send the built json message to the server and get the position & path response 
     * the response from the server may be the position & path or a message "success"
     * if the response is "success", that means we have reached the destination 
     */
    private Utils.PathResponse getPathFromServer(Socket socket, String message) {

        var message_byte = Encoding.UTF8.GetBytes(message);
        socket.Send(message_byte, message_byte.Length, 0);

        var response_byte = Utils.getBytesFromServer(0, socket);
        if (response_byte == null) { throw new Exception("fail to get path from server"); }

        if (Utils.isReached(response_byte)) {
            return null;
        }

        return Utils.parsePathResponse(response_byte);

    }



    /**
     * draw the path on the unity scene as a white line base on the path response from the server 
     */
    private void drawPath(Utils.PathResponse path) {

        var lineRenderer = GameObject.Find("/UserContex/Line").GetComponent<LineRenderer>();
        lineRenderer.positionCount = path.pathLength();
        lineRenderer.SetPositions(path.getVector3Path());

    }

}
