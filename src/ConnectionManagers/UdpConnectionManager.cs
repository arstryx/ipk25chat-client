using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using ipk25_chat.Messages;

namespace ipk25_chat.ConnectionManagers;

public class UdpConnectionManager: ConnectionManager
{
    UdpClient UdpClient;
    
    private int MaxRetries = 3;
    private int TimeoutMs = 250;

    private int ServerPort;
    private string ServerIp;
    
    private Queue<ConfirmM> IncomingConfirms = new Queue<ConfirmM>();
    private Queue<ReplyM> IncomingReplies = new Queue<ReplyM>();     
    
    // Add list of processed ids
    //private List<ushort> ReceivedIds = new List<ushort>();
    
    
    
    public UdpConnectionManager(string ip, int port, int maxRetries, int timeoutMs)
    {
        ServerPort = port;
        ServerIp = ip;
        UdpClient = new UdpClient();
        UdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        MaxRetries = maxRetries;
        TimeoutMs = timeoutMs;
    }

    
    public override async Task ProcessConnection()
    {
        Task sending = SendMessages();
        Task receiving = ReceiveMessages();
        await Task.WhenAll(receiving, sending);
    }
    
    protected override async Task ReceiveMessages()
    {
        while (true)
        {
            UdpReceiveResult result = await UdpClient.ReceiveAsync();
            Message? message;
            
            try 
            {
                message = IncomingMessagesParser.TranslateIncomingUdpMessage(result.Buffer);
            }
            catch (Exception)
            {
                await SendErr("Failed to parse incoming message.");
                return;
            }
            
            ConnectionMutex.WaitOne();
            if (message is ConfirmM confirm)
            {
                IncomingConfirms.Enqueue(confirm);
            }
            
            else if (message is ReplyM reply)
            {
                if (CurrentState == FsmStateEnum.START)
                {
                     ServerPort = result.RemoteEndPoint.Port;    
                }
                IncomingReplies.Enqueue(reply);
                await SendConfirm(reply.MessageId);
            }
            
            else if (message is MsgM msg)
            {
                Console.WriteLine(msg.ToFormattedOutput());
                await SendConfirm(msg.MessageId);
            }
            
            else if (message is PingM ping)
            {
                await SendConfirm(ping.MessageId);
            }
            
            else if (message is ByeM bye)
            {
                await SendConfirm(bye.MessageId);
                UdpClient.Close();
                Environment.Exit(0);
            }
            
            else if (message is ErrM err)
            {
                await SendConfirm(err.MessageId);
                Console.Error.WriteLine(err.ToFormattedOutput());
                UdpClient.Close();
                Environment.Exit(1);
            }
            else
            {
                await SendErr("Message type matching failed.");
                return;
            }
            ConnectionMutex.ReleaseMutex();
        }
    }

    protected override async Task SendMessages()
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            await SendBye();
        };
        
        while (true)
        {
            string? input = await Task.Run(() => Console.ReadLine());
            Message? message = await ProcessInput(input);
            if (message == null)
            {
                continue;
            }
            
            ConnectionMutex.WaitOne();
            switch (CurrentState)
            {
                case FsmStateEnum.START:
                    if (message is AuthM auth)
                    {
                        ConnectionMutex.ReleaseMutex();
                        await Send(message);
                        ReplyM? reply = await WaitReply();
                        if (reply == null)
                        {
                            await SendErr("No reply received");
                            return;
                        }     
                        Console.WriteLine(reply.ToFormattedOutput());
                        if (reply.Status)
                        {
                            DisplayName = auth.DisplayName;
                            CurrentState = FsmStateEnum.OPEN;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("ERROR: Invalid message type for START state!");
                    }
                    ConnectionMutex.ReleaseMutex();
                    break;
                
                case FsmStateEnum.OPEN:
                    if (message is MsgM msg)
                    {
                        ConnectionMutex.ReleaseMutex();
                        await Send(msg);
                        ConnectionMutex.WaitOne();
                    }
                    else if (message is JoinM)
                    {
                        ConnectionMutex.ReleaseMutex();
                        await Send(message);
                        ReplyM? reply = await WaitReply();
                        if (reply == null)
                        {
                            await SendErr("No reply received");
                            return;
                        }
                        Console.WriteLine(reply.ToFormattedOutput());
                    }
                    else
                    {
                        Console.Error.WriteLine("ERROR: Invalid message type for OPEN state!");
                    }
                    ConnectionMutex.ReleaseMutex();
                    break;
            }
            
        }
    }

    protected override async Task Send(Message message)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            await UdpClient.SendAsync(message.ToUdpBytes(), ServerIp, ServerPort);
            for (int j = 0; j < TimeoutMs; j += CheckIntervalMs)
            {
                await Task.Delay(CheckIntervalMs);
                ConnectionMutex.WaitOne();
                while (IncomingConfirms.Count > 0)
                {
                    ConfirmM queueMessage = IncomingConfirms.Dequeue();
                    if (queueMessage.RefMessageId == message.MessageId)
                    {
                        MessageId++;
                        ConnectionMutex.ReleaseMutex();
                        return;
                    }
                }
                ConnectionMutex.ReleaseMutex();
            }
        }
        Console.Error.WriteLine("ERROR: Failed to receive confirmation.");
        UdpClient.Close();
        Environment.Exit(1);
    }

    protected override async Task<ReplyM?> WaitReply()
    {
        for (int i = 0; i < ReplyTimeoutMs; i += CheckIntervalMs)
        {
            await Task.Delay(CheckIntervalMs);
            ConnectionMutex.WaitOne();
            while (IncomingReplies.Count > 0)
            {
                ReplyM reply = IncomingReplies.Dequeue();
                if (reply.RefMessageId == MessageId - 1)
                {
                    return reply;
                }
            }
            ConnectionMutex.ReleaseMutex();
        }
        return null;
    }

    private async Task SendConfirm(ushort refId)
    {
        ConfirmM confirm = new ConfirmM(refId);
        await UdpClient.SendAsync(confirm.ToUdpBytes(), ServerIp, ServerPort);
    }
    

    protected override async Task SendBye()
    {
        if (CurrentState != FsmStateEnum.START)
        {
            ByeM bye = new ByeM(this.DisplayName, MessageId);
            await Send(bye);
        }
        UdpClient.Close();
        Environment.Exit(0);
    }

    protected override async Task SendErr(string text)
    {
        Console.Error.WriteLine("ERROR: " + text);
        if (CurrentState != FsmStateEnum.START)
        {
            ErrM err = new ErrM(this.DisplayName, text, MessageId);
            await Send(err);
        }
        UdpClient.Close();
        Environment.Exit(1);
    }
}