## Used tools

 - Asp.Net
 - Discord.Net
 - SignalR
 - SQLite
 - EntityFramework
## How to launch
**Build types**:

This console application can be compiled for any system: windows, linux - because it uses .Net Core 6 technology. 

There are two types of builds (Debug & Release) which differ in the presence of logging to the console.

For development it is more convenient to use *Debug* mode, and for production it is more convenient to use *Release* mode, which removes the logging in the console, which slows down the processing of requests.

**Setup steps**:

 - Run the application and get an exception. Everything is fine. Folders and files were generated for key configuration and url at launch location.
 - File *credentials/discord/token.txt* contains the bot token issued in the discord application center.
 - Directory *credentials/pachat/* contains files for the connection path (url) and api key/secret.
 - You need to fill out all the credentials.

**Connect channels (nodes)**:

Text Discord channels are called nodes. You can connect the node using bot commands. Commands list:

 - ***!transmit setnode*** general global. Signature is !transmit setnode *discord_channel* *pachat_channel* 1. "1" is used as mark for channel that can read&write to system.
 - ***!transmit deletenode*** general. Signature is !transmit deletenode *discord_channel*.
 
You can use any discord channel name that exist on your discord server.
 
Added nodes will be used immediately for forwarding messages. The added nodes will immediately be used to forward messages. Node data is stored in the sqlite database in the file *databases/sqlite/transmitnodes.db*

**Linux Setup (ubuntu)**
 - install dotnet6
 - put program in some directory, preferably in usr/
 - create patransmitter.service file in etc/systemd/system
 - paste text to created file and check ExecStart and Working Directory to be correct (change path to your program location)
 
```
[Unit]
Description=PaTransmitter bot console app.
[Service]
# systemd will run this executable to start the service
# if /usr/bin/dotnet doesn't work, use `which dotnet` to find correct dotnet executable path
WorkingDirectory=/usr/workroot/patransmitter
ExecStart=/usr/bin/dotnet /usr/workroot/patransmitter/PaTransmitter.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
KillSignal=SIGINT
TimeoutStopSec=30
# to query logs using journalctl, set a logical name here
SyslogIdentifier=patransmitter
# Use your username to keep things simple.
# If you pick a different user, make sure dotnet and all permissions are set correctly to run the app
# To update permissions, use 'chown yourusername -R /srv/HelloWorld' to take ownership of the folder and files,
#       Use 'chmod +x /srv/HelloWorld/HelloWorld' to allow execution of the executable file
User=root
# ensure the service restarts after crashing
Restart=always
# amount of time to wait before restarting the service                        
RestartSec=15   
# This environment variable is necessary when dotnet isn't loaded for the specified user.
# To figure out this value, run 'env | grep DOTNET_ROOT' when dotnet has been loaded into your shell.
Environment=DOTNET_ROOT=/usr/lib64/dotnet
[Install]
WantedBy=multi-user.target
```

- use commands from systemctl, google it

```
sudo systemctl enable patransmitter.service (enables startup on system launch)
sudo systemctl start patransmitter.service (starts the service)
sudo systemctl restart patransmitter.service (restarts the service)
sudo systemctl status patransmitter.service (show status of the service)
etc..
```

