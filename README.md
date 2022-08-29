## Used tools

 - Asp.Net
 - Discord.Net
 - SignalR
 - SQLite
 - EntityFramework
## How to launch
**Build types**:

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
