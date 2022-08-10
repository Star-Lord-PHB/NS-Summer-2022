import socket 
import json
import sys
from turtle import position 
from scipy.optimize import fsolve
from AStar import aStarSearch
from Utils import Mark, Position, getStrFromClient, distanceCalculationEquationGenenerator, log



all_sensors: dict[str, dict[str, float]] = {}
mark_list: dict[str, Mark] = {}
floor_height: dict[int, float] = {}



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



def navigation(start: Position, dest: Position) -> list[Mark] :
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

    log("waiting for data to init the sensors")

    conn, addr = server.accept()


    # init the floor height info #####################
    message = ""
    try :
        message = getStrFromClient(conn, 5)
    except TimeoutError :
        log("floor height info initialization time out!", "error")
        conn.close()
        sys.exit(1)

    global floor_height
    for floor_str, height in json.loads(message).items() :
        floor_height[int(floor_str)] = height

    log("floor height info: \n", floor_height)
    conn.send("success".encode("utf-8"))


    # init the sensors info ###########################
    message = ""
    try :
        message = getStrFromClient(conn, 5)
    except TimeoutError :
        log("sensors info initialization time out!", "error")
        conn.close()
        sys.exit(1)

    global all_sensors 
    all_sensors = json.loads(message)

    log("sensor data:\n", all_sensors)
    conn.send("success".encode("utf-8"))


    # init the mark map info ##########################
    try :
        message = getStrFromClient(conn, 5)
    except : 
        log("mark map info initialization time out!", "error")
        conn.close()
        sys.exit(1)
    
    mark_map = json.loads(message)
    log("mark map: \n", mark_map)

    conn.send("success".encode("utf-8"))


    # init the marks info #############################
    try :
        message = getStrFromClient(conn, 5)
    except :
        log("all marks info initialization time out!", "error")
        conn.close()
        sys.exit(1)
    
    all_marks = json.loads(message)

    landMarksInit(all_marks, mark_map)
    log("all marks: ")
    for mark_name in mark_list :
        print(mark_list[mark_name])

    conn.send("success".encode("utf-8"))


    conn.close()
    server.close()



def serverMain() :

    server = socket.socket()

    server.bind(("127.0.0.1", 8080))
    server.listen(5)

    log("server started")
    log("waiting for new navigation request...")

    while True :

        # get navigation request and the destination from client #######################
        conn, addr = server.accept()

        message = ""
        try : 
            message = getStrFromClient(conn, 5)
        except TimeoutError: 
            conn.close()
            continue
        
        destination = parseDestination(message)

        conn.send("success".encode("utf-8"))

        # keep getting distance info, positioning and calculate the path #####################
        position_last: Position = None
        while True :

            # get the distance info 
            message = ""
            try : message = getStrFromClient(conn, 5)
            except TimeoutError: 
                log("sensors distance update time out for the current navigation, stopping navigation...", "warning")
                log("waiting for new navigation request...")
                break 
            
            sensor_data_list, floorNum = parseStrFromClient(message)
                
            # calculate the position 
            equation = distanceCalculationEquationGenenerator(sensor_data_list[:4])
            result = [e for e in fsolve(equation, [0,0,0,0])]
            currentPosition = Position(result[0], result[1], result[2])
            fixHeight(currentPosition, floorNum)

            # navigation 
            if currentPosition.distanceTo(destination) < 1.0 :
                log("Reach destination!")
                conn.send("success".encode("utf-8"))
                break 

            path = navigation(currentPosition, destination)

            # response to client 
            response = buildNavigationResponse(path, currentPosition, floorNum, destination)
            conn.send(response.encode("utf-8"))

            # print logs 
            if not (currentPosition == position_last) :
                log("chosen sensors info: ")
                for i in range(4) :
                    print(sensor_data_list[i])
                log("calculated position: (x={}, y={}, height={}, floor={})".format(result[0], result[1], result[2], floorNum))
                position_last = currentPosition
                log("passed marks in the calculated path: ")
                for mark in path :
                    print(mark)

        conn.close()



if __name__ == "__main__" :
    serverInit()
    serverMain()
