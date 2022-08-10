from cmath import sqrt
from datetime import datetime
from email import message
import socket 
import json
import sys
from turtle import position 
from scipy.optimize import fsolve
import struct
from AStar import Node, aStarSearch



class Mark(Node) :
    
    def __init__(self, name: str, x: float, y: float, height: float) -> None:
        self.name = name
        self.position = Position(x, y, height)
        self.neibours: dict[Mark, float] = {}
        self.parent = None
        self.distanceToDest: float = 0.0
        self.pathCost: float = 0.0
        self.visitedNodes: set[Mark] = set([self])
    
    def __eq__(self, __o: object) -> bool:
        if not isinstance(__o, Mark): return False
        return self.name == __o.name and self.x == __o.x and self.y == __o.y and self.height == __o.height
    
    def __hash__(self) -> int:
        return hash(self.name) + hash(self.x) + hash(self.y) + hash(self.height)

    def __str__(self) -> str:
        return "Mark({}, x={}, y={}, height={})".format(self.name, self.x, self.y, self.height)

    def h(self) -> float :
        return self.distanceToDest + self.pathCost

    def distanceTo(self, dest) -> float :
        return self.position.distanceTo(dest.position)

    @property
    def x(self) :
        return self.position.x
    
    @x.setter
    def x(self, value: float) :
        self.position.x = value

    @property
    def y(self) :
        return self.position.y
    
    @y.setter
    def y(self, value: float) :
        self.position.y = value

    @property
    def height(self) :
        return self.position.height
    
    @height.setter
    def height(self, value: float) :
        self.position.height = value



class Position :

    def __init__(self, x: float, y: float, height: float) -> None:
        self.x = x 
        self.y = y
        self.height = height
    
    def distanceTo(self, p) -> float :
        return sqrt((self.x - p.x) ** 2 + (self.y - p.y) ** 2 + (self.height - p.height) ** 2).real

    def __eq__(self, __o: object) -> bool:
        if not isinstance(__o, Position): return False
        return self.x == __o.x and self.y == __o.y and self.height == __o.height


all_sensors: dict[str, dict[str, float]] = {}
mark_list: dict[str, Mark] = {}
floor_height: dict[int, float] = {}



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



def parseStrFromClient(message: str) -> tuple[list[dict[str, float]], int] :

    data: dict[str, float] = json.loads(message)
    sensor_data_list: list[dict[str, float]] = []

    for sensor_id in data :
        all_sensors[sensor_id]["distance"] = data[sensor_id]
        sensor_data_list.append(all_sensors[sensor_id])
    
    floorNum = sensor_data_list[0]["floor"]

    return sensor_data_list, int(floorNum)



def fixHeight(position: Position, floorNum: int) :
    position.height = floor_height[floorNum]



def landMarksInit(all_marks: dict[str, dict[str, float]], mark_map: dict[str, dict[str, float]]) :
    
    global mark_list

    for mark_name in all_marks :
        mark_list[mark_name] = Mark(mark_name, all_marks[mark_name]["x"], all_marks[mark_name]["y"], all_marks[mark_name]["height"])
    
    for mark_name in mark_list :
        mark = mark_list[mark_name]
        for neibour_name in mark_map[mark_name] :
            mark.neibours[mark_list[neibour_name]] = mark_map[mark_name][neibour_name]
        mark_list[mark_name] = mark



def parseDestination(message: str) :
    temp = json.loads(message)
    return Position(temp["x"], temp["y"], temp["height"])



def closestMarkTo(p: Position) -> Mark :
    closestMark: Mark = None 
    smallestDistance = float('inf')
    for mark_name in mark_list :
        distance = p.distanceTo(mark_list[mark_name])
        if distance <= smallestDistance :
            closestMark = mark_list[mark_name]
            smallestDistance = distance
    return closestMark



def navigation(start: Position, dest: Position) -> list[Node] :
    for mark_name in mark_list :
        mark_list[mark_name].distanceToDest = mark_list[mark_name].position.distanceTo(dest)
    
    startMark = closestMarkTo(start)
    endMark = closestMarkTo(dest)

    return aStarSearch(startMark, endMark)



def buildNavigationResponse(path: list[Mark], start: Position, floorNum: int, dest: Position) -> str :

    message = "{\"position\": {\"x\": " + str(start.x) + ", \"y\": " + str(start.y) + ", \"height\": " + str(start.height) + ", \"floor\": " + str(floorNum) + "}, \"path\": ["
    message += "{\"x\": " + str(start.x) + ", \"y\": " + str(start.y) + ", \"height\": " + str(start.height) + "}, "
    for i, mark in enumerate(path) :
        message += "{\"x\": " + str(mark.x) + ", \"y\": " + str(mark.y) + ", \"height\": " + str(mark.height) + "}, "
    message += "{\"x\": " + str(dest.x) + ", \"y\": " + str(dest.y) + ", \"height\": " + str(dest.height) + "}]}"

    return message



def serverInit() :
    
    server = socket.socket()

    server.bind(("127.0.0.1", 8081))
    server.listen(5)

    print("waiting for data to init the sensors")

    conn, addr = server.accept()


    # init the floor height info #####################
    message = ""
    try :
        message = getStrFromClient(conn, 5)
    except TimeoutError :
        print("floor height info initialization time out!")
        conn.close()
        sys.exit(1)

    global floor_height
    for floor_str, height in json.loads(message).items() :
        floor_height[int(floor_str)] = height

    print("floor height info: \n", floor_height)
    conn.send("success".encode("utf-8"))


    # init the sensors info ###########################
    message = ""
    try :
        message = getStrFromClient(conn, 5)
    except TimeoutError :
        print("sensors info initialization time out!")
        conn.close()
        sys.exit(1)

    global all_sensors 
    all_sensors = json.loads(message)

    print("sensor data:\n", all_sensors)
    conn.send("success".encode("utf-8"))


    # init the mark map info ##########################
    try :
        message = getStrFromClient(conn, 5)
    except : 
        conn.close()
        sys.exit(1)
    
    mark_map = json.loads(message)
    print("mark map: \n", mark_map)

    conn.send("success".encode("utf-8"))


    # init the marks info #############################
    try :
        message = getStrFromClient(conn, 5)
    except :
        conn.close()
        sys.exit(1)
    
    all_marks = json.loads(message)

    landMarksInit(all_marks, mark_map)
    print("all marks: ")
    for mark_name in mark_list :
        print(mark_list[mark_name])

    conn.send("success".encode("utf-8"))


    conn.close()
    server.close()



def serverMain() :

    server = socket.socket()

    server.bind(("127.0.0.1", 8080))
    server.listen(5)

    print("server started")

    while True :

        conn, addr = server.accept()

        message = ""
        try : 
            message = getStrFromClient(conn, 5)
        except TimeoutError: 
            conn.close()
            continue
        
        destination = parseDestination(message)

        conn.send("success".encode("utf-8"))

        #______________positioning________________
        position_last: Position = None
        while True :

            message = ""
            try : message = getStrFromClient(conn, 5)
            except TimeoutError: 
                print("time out")
                break
            
            sensor_data_list, floorNum = parseStrFromClient(message)
                
            equation = distanceCalculationEquationGenenerator(sensor_data_list[:4])
            result = [e for e in fsolve(equation, [0,0,0,0])]
            currentPosition = Position(result[0], result[1], result[2])
            fixHeight(currentPosition, floorNum)

            if currentPosition.distanceTo(destination) < 1.0 :
                print("Reach destination!")
                conn.send("success".encode("utf-8"))
                break 

            path = navigation(currentPosition, destination)

            response = buildNavigationResponse(path, currentPosition, floorNum, destination)

            if not (currentPosition == position_last) :
                for i in range(4) :
                    print(sensor_data_list[i])
                print("position: (x={}, y={}, height={}, floor={})".format(result[0], result[1], result[2], floorNum))
                position_last = currentPosition
                for mark in path :
                    print(mark)
                print(response)


            conn.send(response.encode("utf-8"))

        conn.close()



if __name__ == "__main__" :
    serverInit()
    serverMain()
