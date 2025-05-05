using System.Net;
using System.Net.Sockets;
using System.Text;
using ipk25_chat.Messages;

namespace ipk25_chat.ConnectionManagers;

public class TcpConnectionManager: ConnectionManager
{  
    private TcpClient TcpClient;
    private NetworkStream NetworkStream;
    private ReplyM? GotReply = null;
    
    // Constructor
    public TcpConnectionManager(string ip, int port)
    {    
        try
        {
            this.TcpClient = new TcpClient(ip, port);
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"ERROR: Unable to initialise TCP client.");
            Environment.Exit(1);
        }
        this.NetworkStream = this.TcpClient.GetStream();
    }
    
    public override async Task ProcessConnection()
    {
        
        Task ReceiveTask = ReceiveMessages();
        Task SendTask = SendMessages();

        await Task.WhenAll(ReceiveTask, SendTask);
        TcpClient.Close();
        
    }

    protected override async Task SendMessages()
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            ConnectionMutex.WaitOne();
            await SendBye();
            ConnectionMutex.ReleaseMutex();
        };
        
        while (true)
        {
            string? input = await Task.Run(() => Console.ReadLine());
            
            ConnectionMutex.WaitOne();
            Message? message = await ProcessInput(input);
            if (message == null)
            {
                ConnectionMutex.ReleaseMutex();
                continue;
            }
            switch (this.CurrentState)
            {
                case FsmStateEnum.START:
                    if (message is AuthM auth)
                    {
                        await Send(auth);
                        ConnectionMutex.ReleaseMutex();
                        ReplyM? reply = await WaitReply();
                        if (reply == null)
                        {
                            await SendErr("No reply received.");
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
                        Console.Error.WriteLine($"ERROR: Unsupported message type for START state");
                    }
                    ConnectionMutex.ReleaseMutex();
                    break;
                
                case FsmStateEnum.OPEN:
                    if (message is MsgM msg)
                    {
                        await Send(msg);
                    }
                    else if (message is JoinM)
                    {
                        await Send(message);
                        ConnectionMutex.ReleaseMutex();
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

    protected override async Task ReceiveMessages()
    {
        byte[] buffer = new byte[60033];
        while (true)
        {
            int bytesRead = await NetworkStream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            
            
            Message? message;
            ConnectionMutex.WaitOne();
            try 
            {
                message = IncomingMessagesParser.TranslateIncomingTcpMessage(receivedMessage);
            }
            catch (Exception)
            {
                await SendErr("Failed to parse incoming message.");  
                return;
            }
            if (message is ErrM err)
            {
                Console.Error.WriteLine(err.ToFormattedOutput());
                TcpClient.Close();
                Environment.Exit(1);
            }
            else if (message is ByeM bye)
            {
                TcpClient.Close();
                Environment.Exit(0);
            }
            else if (message is MsgM msg)
            {
                if (CurrentState == FsmStateEnum.START)
                {
                    await SendErr("Unexpected message received.");
                }
                Console.WriteLine(message.ToFormattedOutput());
            }
            else if (message is ReplyM reply)
            {
                GotReply = reply;
            }
            ConnectionMutex.ReleaseMutex();
        }
        
    }

    protected override async Task SendBye()
    {

        ByeM bye = new ByeM(this.DisplayName);
        await Send(bye);
    
        TcpClient.Close();
        Environment.Exit(0);
    }

    protected override async Task SendErr(string text)
    {
        Console.Error.WriteLine("ERROR: " + text);
  
        ErrM err = new ErrM(this.DisplayName, text);
        await Send(err);
        TcpClient.Close();
        Environment.Exit(1);
    }

    protected override async Task Send(Message message)
    {
        await NetworkStream.WriteAsync(Encoding.ASCII.GetBytes(message.ToTcpString()));
    }

    protected override async Task<ReplyM?> WaitReply()
    {
        for (int i = 0; i < ReplyTimeoutMs; i += CheckIntervalMs)
        {
            await Task.Delay(CheckIntervalMs);
            ConnectionMutex.WaitOne();  
            if (GotReply != null)
            {
                ReplyM reply = GotReply;
                GotReply = null;
                return reply;
            }
            ConnectionMutex.ReleaseMutex();
        }
        return null;
    }
}