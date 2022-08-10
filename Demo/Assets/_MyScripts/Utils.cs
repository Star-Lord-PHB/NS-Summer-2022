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


public class Utils : MonoBehaviour
{
    public static double directDistance(GameObject obj1, GameObject obj2) {

        var position1 = obj1.transform.position;
        var position2 = obj2.transform.position;

        return Sqrt(Pow(position1.x - position2.x, 2) 
                    + Pow(position1.y - position2.y, 2) 
                    + Pow(position1.z - position2.z, 2));

    }



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



    public static bool acknowledge(Socket socket, String expectedMessage = "success", int timeOut = 5) {

        socket.ReceiveTimeout = timeOut;
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



    public static bool byteArrEquals(byte[] arr1, byte[] arr2, int length) {
        for (int i = 0; i < length; i++) {
            if (arr1[i] != arr2[i]) { return false; }
        }
        return true;
    }



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



    public static PathResponse parsePathResponse(byte[] message) {

        var stream = new MemoryStream(message);
        var deseralizer = new DataContractJsonSerializer(typeof(PathResponse));

        var result = (PathResponse)deseralizer.ReadObject(stream);
        // var userObject = GameObject.Find("/UserContex/User");
        // result.position.floorNum = userObject.GetComponent<User>().floorNum_calcualted;

        return result;

    }



    public static bool isReached(byte[] message) {
        var expectedResponse = Encoding.UTF8.GetBytes("success");
        if (byteArrEquals(message, expectedResponse, expectedResponse.Length)) {
            return true;
        }
        return false;
    }



    public static void quit() {
        Application.Quit();
        EditorApplication.isPlaying = false;
    }
}
