from datetime import datetime
from email import message
import socket 
import json
import sys 
from scipy.optimize import fsolve
import struct


all_sensors: dict[str, dict[str, float]] = {
    "Sensor_livingRoom_1": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_2": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_3": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_4": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_5": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_6": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_7": {"x": 1, "y": 1, "height": 1},
    "Sensor_livingRoom_8": {"x": 1, "y": 1, "height": 1},
}


def distanceCalculationEquationGenenerator(sensorDataList: list[dict[str, float]]) : 

    def equation(i) :
        x, y, z = i[0], i[1], i[2]
        return [
            (sensorDataList[0]["x"] - x) ** 2 + (sensorDataList[0]["y"] - y) ** 2 + (sensorDataList[0]["height"] - z) - sensorDataList[0]["distance"] ** 2,
            (sensorDataList[1]["x"] - x) ** 2 + (sensorDataList[1]["y"] - y) ** 2 + (sensorDataList[1]["height"] - z) - sensorDataList[1]["distance"] ** 2,
            (sensorDataList[2]["x"] - x) ** 2 + (sensorDataList[2]["y"] - y) ** 2 + (sensorDataList[2]["height"] - z) - sensorDataList[2]["distance"] ** 2,
            (sensorDataList[3]["x"] - x) ** 2 + (sensorDataList[3]["y"] - y) ** 2 + (sensorDataList[3]["height"] - z) - sensorDataList[3]["distance"] ** 2
        ]
    
    return equation



def floatToBytes(f: float) -> bytes :
    return struct.pack("d", f)



def getStrFromClient(conn: socket.socket, timeOut: int) -> str :

    message = ""
    startTime = datetime.now()
    buffer_size = 1024

    while True :
        byte_buffer = conn.recv(buffer_size)

        if len(byte_buffer) == 0 and message == "" :
            if (datetime.now() - startTime).seconds >= timeOut : raise TimeoutError()
            continue

        message += byte_buffer.decode("utf-8")
        if len(byte_buffer) < buffer_size : break
    
    return message



def parseStrFromClient(message: str) -> list[dict[str, float]] :

    data: dict[str, float] = json.loads(message)
    sensor_data_list: list[dict[str, int]] = []

    for sensor_id in data :
        all_sensors[sensor_id]["distance"] = data[sensor_id]
        sensor_data_list.append(all_sensors[sensor_id])

    return sensor_data_list



def serverInit() :
    
    server = socket.socket()

    server.bind(("127.0.0.1", 8081))
    server.listen(5)

    print("waiting for data to init the sensors")

    conn, addr = server.accept()

    message = ""
    try :
        message = getStrFromClient(conn, 5)
    except TimeoutError :
        conn.close()
        sys.exit(1)
    
    print(message)

    global all_sensors 
    all_sensors = json.loads(message)

    print("sensor data:", all_sensors)

    conn.close()
    server.close()



def serverMain() :

    server = socket.socket()

    server.bind(("127.0.0.1", 8080))
    server.listen(5)

    print("server started")

    position_last: list[float] = []

    while True :

        conn, addr = server.accept()

        message = ""
        try : 
            message = getStrFromClient(conn, 5)
        except TimeoutError: 
            conn.close()
            continue
        
        sensor_data_list = parseStrFromClient(message)
            
        equation = distanceCalculationEquationGenenerator(sensor_data_list[:4])
        result = [e for e in fsolve(equation, [0,0,0,0])]

        if not (result == position_last) :
            print(all_sensors)
            for i in range(4) :
                print(sensor_data_list[i])
            print("position: (x={}, y={}, height={})".format(result[0], result[1], result[2]))
            position_last = result


        response = floatToBytes(result[0]) \
                    + floatToBytes(result[1]) \
                    + floatToBytes(result[2])

        conn.send(response)

        conn.close()



if __name__ == "__main__" :
    serverInit()
    serverMain()
