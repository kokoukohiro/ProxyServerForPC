# ProxyServerForPC
A proxy server that can be run on a personal computer.  
You can find the original version from [www.mentalis.org](http://www.mentalis.org/soft/projects/proxy/).  
To implement the proxy function at least two people need to run the program at the same time.  
It is used to solve the problem of high online latency, but you need to find someone with low latency to be the server.  
  
*Client please enter* `exit` *to exit, otherwise closing directly will result in no internet access.*

### Display
`server or client?`  
By entering `server` you will become the server and the network will not be affected.  
By entering `client` you will become the client and access the other network, your IP will become the IP of the other party and your latency will always be slightly higher than the other party.  
  
`Command not understood.`  
The command you entered is incorrect.  
  
`Please enter a port：`  
Please enter a local port (0\~65536).  
The second time this line diaplays, please enter the port of the other party (server).  
  
`Please enter a host:`  
Please enter the IP of the other party (server).  

`Please connnect to the Internet`  
Please connect to the Internet.  
  
`Can not map the port,please map it manually` or `Can not map the port,the use of the UPnP feature in router,please map it manually`  
The port mapping failed, you are asked to port map manually, which usually occurs on the server. For how to manually port map refer to [this](https://www.hellotech.com/guide/for/how-to-port-forward).  

`Map to xxx.xxx.xxx.xxx:xxxx success`  
The port mapping is successful. The one in front of `:` is your server IP, the one after is your server port, let the client enter these to access you.  
  
### Command
`showtopo`  
Show the current server IP and the IP of all clients accessing the server, can be used to check your IP in case of port mapping failure.  
  
`exit`  
Destroy all configurations and exit.

`chat`  
Chat, non-English character compatibility is not very good, not very useful.  
  
### How do I connect my console to the opposite server?
1. Press `Win+R`, enter `cmd`, press `Enter`
2. Enter `ipconfig` and press `Enter`
3. Find the ipv4 line, that is your LAN IP, please remember it
4. Inside the console network settings, find proxy setting, fill in your LAN IP in the first line, fill in your custom local port in the second line, save
  
Please ensure that your client and the server are both on and that the computer does not sleep while the program is working.The server IP and your LAN IP will change every day, but there are ways to fix the LAN IP, please google it.
