# ProxyServerForPC
A proxy server that can be run on a personal computer.  
You can find the original version from [www.mentalis.org](http://www.mentalis.org/soft/projects/proxy/).  
To implement the proxy function at least two people need to run this program at the same time.  
It is used to solve the problem of high online latency, but you need to find someone with low latency to be the server.  
It has not been designed GUI until now.  
  
*Client please enter* `exit` *to exit, otherwise closing directly will result in no internet access.*

## Display
`server or client?`  
By entering `server` you will become the server and the network will not be affected.  
By entering `client` you will become the client and access the other network, your IP will become the IP of the other party and your latency will always be slightly higher than the other party.  
  
`Command not understood.`  
The command you entered is incorrect.  
  
`Please enter a port：`  
Please enter a local port (0\~65536) to listen on.  
The second time this line diaplays, please enter the port of the other party (server).  
  
`Please enter a host:`  
Please enter the IP of the other party (server).  

`Please connnect to the Internet`  
Please connect to the Internet.  
  
`Can not map the port,please map it manually` or  
`Can not map the port,the use of the UPnP feature in router,please map it manually`  
The port mapping failed, you are asked to port map manually, which usually occurs on the server. For how to manually port map refer to [this](https://www.hellotech.com/guide/for/how-to-port-forward).  

`Map to <IP>:<Port> success`  
The port mapping is successful. `<IP>` is your server IP, `<Port>` is your server port, let the client enter these to access you.  

`Please enter a message:`  
Please enter a message that will be sent to the entire proxy network.  
  
`Please enter a host and access it:`  
Please enter a target server address for testing.  
  
`Message from <IP>:<Message>`  
Received a `<Message>` from `<IP>`.  
  
`Latency test from <IP>:[proxy:<1>]direct:<2>`  
Latency test results were received from `<IP>`, with `<2>` for direct connection latency and `<1>` for proxy latency. When the latency result is -1 it means that the test timed out and could not be connected. Usually there is not just one test result, as there are at least two devices in the network.  
  
`Switched to a client|server`  
Successfully switch to a client or server.  
  
`Access to <Host>:latency <1>`  
This device has access to `<Host>` and the latency is `<1>`.  
  
## Command
`showtopo`  
Show the current server IP and the IP of all clients accessing the server, can be used to check your IP in case of port mapping failure.  
  
`exit`  
Destroy all configurations and exit.

`chat`  
Chat, the next input will be sent to the entire proxy network.  
  
`portmap`  
Performing port mapping, used to allow clients to access the device when it is a client, or to remap if the port mapping fails.  
  
`package`
Display of all request packets passing through the device, second entry to turn off display.  
  
`latency`  
A latency test, which will be performed on the first request packet that matches the address entered by the user, will be a direct connection latency test and a proxy latency test, the results will be sent to the entire proxy network.  
  
`switch`  
With a mode switch, the client will become the server and disconnect from the original server; the server will become the client, but retain all sub-client connections.  
  
`userule`  
Enabling Proxy Rules, test all requests for latency and if the latency of the direct connection is lower than the proxy latency, the request will not be proxied. If the request to the destination address has already been tested no test will be performed. The second entry is to turn this feature off.  

`showrulelist`  
Displays the contents of the proxy rules table.  
  
### How do I connect my console to the opposite server?
1. Press `Win+R`, enter `cmd`, press `Enter`
2. Enter `ipconfig` and press `Enter`
3. Find the ipv4 line, that is your LAN IP, please remember it
4. Inside the console network settings, find proxy setting, fill in your LAN IP in the first line, fill in your custom local port in the second line, save
  
Please ensure that your client and the server are both on and that the computer does not sleep while the program is working.
