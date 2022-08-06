import socket 
import json 

server = socket.socket()

server.bind(("127.0.0.1", 8080))
server.listen(5)

print("server started")

while True :

    conn, addr = server.accept()

    message = ""
    while True :
        byte_buffer = conn.recv(1024)
        message += byte_buffer.decode("utf-8")
        if len(byte_buffer) != 0 :
            break
    
    data = json.loads(message)
    for sensor_id in data :
        


    response = (2).to_bytes(4, byteorder='little', signed=True) \
                + (2).to_bytes(4, byteorder='little', signed=True) \
                + (2).to_bytes(4, byteorder='little', signed=True)
    print(response)

    conn.send(response)

    conn.close()
