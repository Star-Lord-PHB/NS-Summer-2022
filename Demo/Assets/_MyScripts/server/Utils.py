from AStar import Node 
import socket
from datetime import datetime
from math import sqrt


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



def log(info: str, type:str = "info") :
    print("[{}, {}]:".format(datetime.now(), type), info)