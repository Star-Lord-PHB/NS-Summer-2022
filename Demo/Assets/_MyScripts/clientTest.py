import socket 

client = socket.socket()

client.connect(("127.0.0.1", 8080))

client.send(r'''[{"1": 5, "2": 5, "3": 5, "4": 5}]'''.encode("utf-8"))

while True :
    buffer = client.recv(1024)
    if len(buffer) != 0 :
        print(buffer)
        client.close()
        break