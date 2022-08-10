using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using UnityEditor;


/**
 * Some utils functions 
 */
public class Utils : MonoBehaviour
{
    /**
     * get the straight line distance between two GameObject 
     */
    public static double directDistance(GameObject obj1, GameObject obj2) {

        var position1 = obj1.transform.position;
        var position2 = obj2.transform.position;

        return Sqrt(Pow(position1.x - position2.x, 2) 
                    + Pow(position1.y - position2.y, 2) 
                    + Pow(position1.z - position2.z, 2));

    }



    /**
     * connect to server base on provided ip and port number 
     * return the connected socket 
     * return null if fail 
     */
    public static Socket connectToServer(String ip, int port, int timeOut = 3) {

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipe = new IPEndPoint(IPAddress.Parse(ip), port);

        var startTime = DateTime.Now;

        while (true) {

            try {
                socket.Connect(ipe);
                return socket;
            } catch (System.Exception) {
                if (new TimeSpan(DateTime.Now.Ticks - startTime.Ticks).TotalSeconds >= timeOut) { break; }
            }

        }

        return null;

    } 



    /**
     * receive byte array of certain length from the server
     * if the provided size is smaller than 0, get as much as it can
     * otherwise, get specified num of bytes 
     */
    public static byte[] getBytesFromServer(int size, Socket socket, int timeOut = 3) {

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

            return result.ToArray();

        }

    }



    /**
     * try to get and check the response from the server that is for acknowledgement 
     * the acknowledgement message is "success" by default 
     * return false if the message does not match or fail to get the message for a while 
     */
    public static bool acknowledge(Socket socket, String expectedMessage = "success", int timeOut = 5) {

        socket.ReceiveTimeout = timeOut * 1000;
        var buffer = new byte[1024];

        try {
            socket.Receive(buffer, buffer.Length, 0);
        } catch (SocketException err) {
            Debug.Log("Acknowledge time out!");
            return false;
        }

        var expectedResponse = Encoding.UTF8.GetBytes(expectedMessage);
        if (byteArrEquals(buffer, expectedResponse, expectedResponse.Length)) {
            return true;
        }
        return false;

    }



    /**
     * compare the first n bytes of two byte array 
     */
    public static bool byteArrEquals(byte[] arr1, byte[] arr2, int length) {
        for (int i = 0; i < length; i++) {
            if (arr1[i] != arr2[i]) { return false; }
        }
        return true;
    }



    /**
     * Class for recording one position 
     * with informative `ToString()` method and `Equals()` method implemented 
     * it will be build from the json message from the server
     */
    [DataContract]
    public class Position {
        [DataMember]
        public float x;
        [DataMember]
        public float y;
        [DataMember]
        public float height;
        [DataMember]
        public int floor;
        public override String ToString() {
            return "(x=" + this.x + ", y=" + this.y + ", height=" + this.height + ", floor=" + this.floor + ")";
        }
        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) { return false; }
            var obj_converted = (Position) obj;
            return this.x == obj_converted.x && this.y == obj_converted.y && this.height == obj_converted.height;
        }
    }



    /**
     * Class for storing the position & path response from the server 
     * it will be built from the json message from the server 
     */
    [DataContract]
    public class PathResponse {

        [DataMember]
        public Position position;

        [DataMember]
        public List<Position> path;

        // override object.Equals
        public override bool Equals(object obj){     
            if (obj == null || GetType() != obj.GetType()) { return false; }
            var obj_converted = (PathResponse) obj;
            if (this.path.Count != obj_converted.path.Count) { return false; }
            for (int i = 0; i < this.path.Count; i++) {
                if (!this.path[i].Equals(obj_converted.path[i])) { return false; }
            }
            return this.position.Equals(obj_converted.position);
        }

        public int pathLength() {
            return this.path.Count;
        }

        public Vector3[] getVector3Path() {
            var result = new Vector3[pathLength()];
            for (int i = 0; i < result.Length; i++) {
                var p = this.path[i];
                result[i] = new Vector3(p.x, p.height, p.y);
            }
            return result;
        }

        public override String ToString() {
            var pathStr = "";
            for (int i = 0; i < path.Count; i++) {
                pathStr += path[i];
                if (i != path.Count - 1) { pathStr += " --> "; }
            }
            return "PathResponse(position=" + this.position + ", path=[" + pathStr + "])";
        }

    }



    /**
     * parse the json message of position & path from the server 
     * and build a PathResponse class 
     */
    public static PathResponse parsePathResponse(byte[] message) {

        var stream = new MemoryStream(message);
        var deseralizer = new DataContractJsonSerializer(typeof(PathResponse));

        var result = (PathResponse)deseralizer.ReadObject(stream);

        return result;

    }



    /**
     * Check whether the response from the server is "success" when trying to fetch the position & path
     * if it is, we have reached the destination 
     */
    public static bool isReached(byte[] message) {
        var expectedResponse = Encoding.UTF8.GetBytes("success");
        if (byteArrEquals(message, expectedResponse, expectedResponse.Length)) {
            return true;
        }
        return false;
    }



    /**
     * quit whole application 
     */
    public static void quit() {
        Application.Quit();
        EditorApplication.isPlaying = false;
    }
}
