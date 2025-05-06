# IPK25-CHAT Client Application: Project Documentation

>This text briefly describes the process and results of **IPK Project 2** development.
It was required to create a client console application implementing given
[**IPK25-CHAT**](https://git.fit.vutbr.cz/NESFIT/IPK-Projects/src/branch/master/Project_2) 
protocol.

Author: **Arsenii Zakharenko** (xzakha02)

License: **MIT** (see `LICENSE.md`)


## Contents

- [IPK25-CHAT Client Application: Project Documentation](#ipk25-chat-client-application-project-documentation)
  - [Installation and Running](#installation-and-running)
  - [Theoretical Knowledge Required](#theoretical-knowledge-required)
    - [Transmission Control Protocol](#transmission-control-protocol)
    - [User Datagram Protocol](#user-datagram-protocol)
    - [IPK25CHAT Protocol](#ipk25chat-protocol)
  - [Implementation Details](#implementation-details)
    - [Message Representation](#message-representation)
    - [Connection](#connection)
    - [Asynchronous Communication Issues](#asynchronous-communication-issues)
    - [Incoming Messages Parsing](#incoming-messages-parsing)
    - [Implementation Summaries](#implementation-summaries)
  - [Testing](#testing)
    - [Environment](#environment)
    - [Local Testing](#local-testing)
      - [No reply to auth message](#no-reply-to-auth-message)
      - [Correct auth reply](#correct-auth-reply)
      - [Longer conversation with join](#longer-conversation-with-join-)
      - [Error handling](#error-handling)
      - [UDP variant](#udp-variant)
    - [Testing via Discord server](#testing-via-discord-server)
      - [TCP](#tcp)
      - [UDP](#udp)
    - [Summaries](#summaries)
  - [Bibliography](#bibliography)

## Installation and Running

> Application was developed for the 
> [Reference Environment](#environment).
> Workability on other Linux OS is highly probable, but can not be guaranteed.

In the root application folder, just run `make` to build executable file `ipk25chat-client`. 

Now you can start using the application simply by e.g. `./ipk25chat-client -h`, that will print
help message.

> Usage details will not be described here since they completely correspond to the
> [task](https://git.fit.vutbr.cz/NESFIT/IPK-Projects/src/branch/master/Project_2).


Provided make file also contains other targets:

| Command           | Description                                                      |
|-------------------|------------------------------------------------------------------|
| `make run-ds-tcp` | Starts the program and connects it to the discord server via TCP |
| `make run-ds-udp` | Starts the program and connects it to the discord server via UDP |
| `make clean`      | Returns application folder to initial state                      |
| `make zip`        | Creates archive ready to be submitted                            |


## Theoretical Knowledge Required

TCP and UDP variants of chat had to be implemented for this project.

### Transmission Control Protocol

TCP is a reliable, connection-based protocol, ensuring that all data arrive in order. 
It uses a handshake to start, and acknowledgments to confirm delivery. 
Lost packets are resent. It's e.g. used for email, file transfers. 
Though slower than UDP, it's accurate and dependable.
Ordinary strings are sent and received during communication.

>These facts make implementation look quite simple, of course if we are using some high-level
abstractions to set up the connection. Protocol itself will do much work for us.

### User Datagram Protocol

UDP is a fast, connectionless protocol that sends data without guarantees. 
Packets may be lost, arrive out of order, or be duplicated. 
It has no flow control or retransmission. 
This makes it ideal for real-time apps like video calls and games.
UDP trades accuracy for speed and low latency.
And now simple strings are not enough, 
byte arrays are needed for communication.

> As wee see, reliability here must be handled by the application, if we want it. The task 
offers us to use identifiers and confirm messages, that is possible solution, even though it noticeably
complicates the implementation.


### IPK25CHAT Protocol

This protocol specifies the format of messages that will be sent between client and server for
both variants. It defines possible message types, how connection should be started and ended, how
possible errors should be processed. We were given only the description of client side, not server.

## Implementation Details

> C# language and Dotnet 8.0 were used to create the needed program. Only native abstractions of the
> mentioned technology were used, so no NuGet packets have to be installed additionally.


### Message Representation

Each message type is represented by its own class (with the postfix `M` at the end), 
inherited from the common abstract class `Message`. It declares following methods, that have to
be defined in its ancestors, specifically for each type:

| Method                       | Description                                                    |
|------------------------------|----------------------------------------------------------------|
| `string ToFormattedOutput()` | Returns message as user readable string for application output |
| `string ToTcpString()`       | Returns message as IPK25-CHAT TCP compatible string            |
| `byte[] ToUdpBytes()`        | Returns message as IPK25-CHAT UDP compatible byte array        |

If specific message type does not have to implement some method, `NotImplementedException` will
be called when trying to access it.

All messages also contain common `ushort MessageId` field. It is widely used in UDP protocol variant,
e.g. for receiving right confirmations and replies.

Only specific message arguments like `DisplayName` or `MessageContent` 
are different for every message class.

>Message classes are used only for storing and translating data to needed forms,
sending is expected to be implemented somewhere else.


### Connection

Whole connection is completely done by either `TcpConnectionManager`
or `UdpConnectionManager` classes, that implement main Finite State Machine of application. For unification, they both inherit abstract class 
`ConnectionManager`.

First of all, it contains many variant-independent fields like `DisplayName` or `ReplyTimeoutMs`.

It also declares async methods (or Tasks, in terms of C#) to be defined in child classes:

| Method                  | Description                                                                       |
|-------------------------|-----------------------------------------------------------------------------------|
| `ProcessConnection()`   | Starts and completely processes new chat session                                  |
| `SendMessages()`        | Starts and manages a thread responsible for sending user messages (FSM)           |
| `Send(Message message)` | Sends specified message to the server. In case of UDP also waits for confirmation |
| `ReplyM? WaitReply()`   | Waits 5 seconds for a reply from the server                                       |
| `ReceiveMessages()`     | Starts and manages a thread responsible for receiving server messages (FSM)       |
| `SendBye()`             | Sends `Bye` message and finishes the program with success                         |
| `SendErr(string text)`  | Sends `Err` message, prints it locally and finishes the program with error        | 

Using 2 different threads to send and receive messages helps to avoid issues connected to active waiting in
e.g. `Console.ReadLine()`. 

Mentioned FSM is noticeably simplified in comparison to its original
specification. For example, intermediate states `AUTH` and `JOIN` became redundant and were changed
by single `WaitReply()`, and it was also no need in `END` state.

Also, in UDP variant, 2 additional queues are used for incoming `ReplyM` and `ConfirmM`.
It is important since we know that messages may be duplicated - so, waiting for right Reply/Confirmation,
we check all received messages of appropriate type.


### Asynchronous Communication Issues

Since 2 main processes operate some shared fields, we need to control their access to them.
It is done by `Mutex`, single for all variants. It works as classic semaphore, blocking
and unblocking needed thread by using `WaitOne` and `Release()` methods in it. This way, only 1 action
with data race vulnerable data is possible at a time.

> Usage of single one mutex even for UDP case (with more sensitive data) may seem as a bad idea
> due to performance (not so obviously though),
> but it was easy to understand and test, that appeared to be more important.

### Incoming Messages Parsing

Incoming messages may be in form of a string or byte array. We will have to convert them into
appropriate messages using `Message TranslateIncomingTcpMessage()` and `Message TranslateIncomingUdpMessage()` methods
from static class `IncomingMessagesParser`

### Implementation Summaries

Described implementation seems simple, reliable and extendable. It actively uses OOP features
like inheritance and polymorphism and is easy to understand because of its logical structure.

> More specific information can be found in the source code comments

## Testing

### Environment

To test our solution, we were provided with
[reference virtual machine](https://nextcloud.fit.vutbr.cz/s/N5fM3Njwm6yfbeZ/download?path=%2F&files=IPK25_Ubuntu24.ova)
and
[custom reference environment](https://git.fit.vutbr.cz/NESFIT/dev-envs). 

>Nevertheless, tests there
and on our local machine with Linux Fedora showed no visible difference, so in this part you will see latter.

>Wireshark program was widely used for testing. It allowed us to read incoming and outcoming
> network packets for specific protocols and ports, no valuable debugging would be possible without
> this.
 


### Local Testing

Before accessing a real public server (in our case Discord) and most probably breaking its rules,
it will be a good idea to test at least basic functionality locally.

We do not have access to server implementation, but we can imitate its expected behaviour manually.
It is more complicated, but it will give us full control, that is quite good, especially for testing
edge cases, that would rarely appear in reality.

To open local server, we use `netcat` program. Command `nc -4 -c -l -v 127.0.0.1 12345`
will open local port 12345 for TCP. Now, we can read incoming strings and send our own back.

Following is few examples of test cases for TCP variant with brief descriptions. For better readability,
order of operations was added.

#### No reply to auth message

```
=> ipk25chat-client
/auth xzakha02 password arstryx [1]
ERROR: No reply received. [3]

=> netcat
AUTH xzakha02 AS arstryx USING password [2]
ERR FROM DefaultName IS No reply received. [4]
```
Works as expected, but as you can notice, error message was sent from the default
name. It is correct, because name would be set to 
specified "arstryx" only after receiving a success reply, that has not happened here.
And in case of error messages, name is not really important for the server.


#### Correct auth reply

```
=> ipk25chat-client
/auth xzakha02 password arstryx [1]
Action Success: OK [3]
Server: Hello [5]
Hello server [6]
(Ctrl+D) [8]

=> netcat
AUTH xzakha02 AS arstryx USING password [2]
REPLY OK IS OK [3]
MSG FROM Server IS Hello [4]
MSG FROM arstryx IS Hello server [7]
BYE FROM arstryx [9]
```

Here everything works as expected, we see successful authorisation and a short conversation ending with 
`Ctrl+D`.


#### Longer conversation with join 

```
=> ipk25chat-client
/auth xzakha02 password arstryx [1]
Action Success: OK [4]
Hello [5]
jack: Hello arstryx, how are you? [8]
I am fine, Jack, thank you. [9]
/join newChannel [11]
Action Success: OK [14]
newJack: Welcome [16]
Good bye [17]
(Ctrl+D) [19]

=> netcat
AUTH xzakha02 AS arstryx USING password [2]
REPLY OK IS OK [3]
MSG FROM arstryx IS Hello [6]
MSG FROM jack IS Hello arstryx, how are you? [7]
MSG FROM arstryx IS I am fine, Jack, thank you. [10]
JOIN newChannel AS arstryx [12]
REPLY OK IS OK [13]
MSG FROM newJack IS Welcome [15]
MSG FROM arstryx IS Good bye [18]
BYE FROM arstryx [20]
```

Again, everything is fine. Basically, tests with netcat are 
limited to something like this - basics are tested,
but no stress-testing or asynchronous challenges.

#### Error handling

```
=> ipk25chat-client
Hello [1]
ERROR: Unsupported message type for START state [2]
/auth [3]
ERROR: Invalid amount of arguments for /auth. [4]
/auth xzakha02 password [5]
ERROR: Invalid amount of arguments for /auth. [6]
/auth xzakha02 password arstryx [7]
Action Success: OK [10]
ahoj [11]
/join [12]
ERROR: Invalid amount of arguments for /join. [13]
(Ctrl+D) [14]

=> netcat
AUTH xzakha02 AS arstryx USING password [8]
REPLY OK IS OK [9]
MSG FROM arstryx IS ahoj [12]
BYE FROM arstryx [15]
```

Local errors do not break anything and are not sent to the server.


#### UDP variant

> UDP variant testing is much more complicated. We still can read incoming messages via `netcat`,
> but it is now impossible to write our messages straight to the console. We will have to convert strings
> to specified byte format using e.g. `xxd`, and control the identifiers as well.

Almost the same things were tested for UDP, so we will not add this to save some place.



### Testing via Discord server

Since basic application functionality is approved, we can connect to the reference discord server and test
our code in real conditions. Testing will be done in the form of more or less realistic chat session.

#### TCP

```
/auth xzakha02 e8f41acb-5d35-4366-bb74-40277392175d arstryx
Action Success: Authentication successful.
Server: arstryx has joined `discord.general` via TCP.
Server: Beauty has joined `discord.general` via UDP.
Beauty: Hello guys 
Hi 
Jak se mas?
Beauty: De to 
Doufam ze ano
Beauty: A co ty 
+- 
Server: bomboclat has joined `discord.general` via UDP.
Beauty: tak to se taky da 
Beauty: tak zatim, ahoj 
Server: Beauty has left `discord.general`.
/join discord.superchannel
Server: arstryx has switched from `discord.general` to `discord.superchannel`.
Action Success: Channel discord.superchannel successfully joined.
Server: arstryx has joined `discord.superchannel` via TCP.
Hi 
Is there anybody?
/join
ERROR: Invalid amount of arguments for /join.
Tak cau
(Ctrl+D)
```   

#### UDP

```
/auth xzakha02 e8f41acb-5d35-4366-bb74-40277392175d arstryx
Action Success: Authentication successful.
Server: arstryx has joined `discord.general` via UDP.
Server: teddy has joined `discord.general` via UDP.
ahoj
Server: bomboclat has joined `discord.general` via UDP.
Server: adam has joined `discord.general` via UDP.
Server: stellka has joined `discord.general` via UDP.
Server: XXX has joined `discord.general` via TCP.
XXX: Hello
stellka: ahoooj
XXX: My name is Arina
My name is Arsenii
Server: speedrun has joined `discord.general` via TCP.
stellka: hello Arina
XXX: asd
stellka: how are you today?
Server: XXX has left `discord.general`.
Server: hu has joined `discord.general` via UDP.
Server: clienttest has joined `discord.general` via TCP.
Server: clienttest has switched from `discord.general` to `test`.
speedrun: hello :)))
stellka: anyone wanna hop on fortnite?
meno: hello
(Ctrl+D)
```

### Summaries

As we can see, program works as it is supposed to, at least during basic use cases.
Of course, it is hard to predict the behavior in more extreme cases, e.g. very big online and
lots of messages being sent. Complications with this also seem quite possible because of
the [synchronization](#asynchronous-communication-issues) method used.

> It is also worth mentioning that no Unit testing was done, mostly due to the lack of time.
> Nevertheless, functionality of each component was tested quite well during other tests, so it
> is not really important in this case.




## Bibliography

1. [IPK Lectures](https://www.fit.vut.cz/study/course/IPK/.cs)
2. [Microsoft Dotnet Documentation](https://dotnet.microsoft.com/en-us/)
3. [ChatGPT AI](https://chatgpt.com) - mostly for theoretical questions

> No specific academic literature was used, 
> except for that utilized by the neural network assistant to generate the responses.