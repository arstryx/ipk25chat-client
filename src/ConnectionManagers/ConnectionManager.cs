using ipk25_chat.Messages;

namespace ipk25_chat.ConnectionManagers;


/**
 * <summary>
 * Base class for TCP and UDP variants of connection managers,
 * main parts of the program.
 * </summary>
 */
public abstract class ConnectionManager
{
    /**
     * <summary>
     * Identifier for a new message that is about to be sent (not received).
     * Respectfully, increases by 1 with each message sent (and confirmed in case of udp).
     * </summary>
     */
    protected ushort MessageId = 0;
    
    protected FsmStateEnum CurrentState = FsmStateEnum.START;
    
    protected readonly int MessageLengthLimit = 60000;
    
    protected readonly int ReplyTimeoutMs = 5000; 
    
    protected readonly int CheckIntervalMs = 25;
    
    protected string DisplayName = "DefaultName";
    
    
    /**
     * <remarks>
     * This single mutex is used for all synchronization in the program, be careful.
     * </remarks>
     */
    protected Mutex ConnectionMutex = new Mutex();
    
    

    /**
     * <summary>
     * Converts user input to corresponding message or command
     * </summary>
     * 
     * <returns>
     * Message of appropriate type or null in case of somehow wrong input,
     * that shows that it should be read once again.
     * </returns>
     */
    protected async Task<Message?> ProcessInput(string? input)
    {
        Message? message;
        if (input == null)
        {
            await SendBye();
            return null;
        }
        try
        {
            if (input.StartsWith("/")) // local command
            {
                string[] words = input.Split(' ');
                if (words[0] == "/auth")
                {
                    if (words.Length != 4) throw new Exception("Invalid amount of arguments for /auth.");
                    message = new AuthM(words[1], words[3], words[2], MessageId);
                }
                else if (words[0] == "/join")
                {
                    if (words.Length != 2) throw new Exception("Invalid amount of arguments for /join.");
                    message = new JoinM(words[1], this.DisplayName, MessageId);
                }
                else if (words[0] == "/rename")
                {
                    if (words.Length != 2) throw new Exception("Invalid amount of arguments for /rename.");
                    DisplayName = words[1];
                    return null;
                }
                else if (words[0] == "/help")
                {
                    ChatHelp();
                    return null;
                }
                else
                {
                    throw new Exception("Unknown command.");
                }
            }
            else
            {
                if (input.Length > MessageLengthLimit)
                {
                    Console.Error.WriteLine("ERROR: Message is too long and was truncated to 60000 symbols.");
                    input = input.Substring(MessageLengthLimit);
                }
                message = new MsgM(DisplayName, input, MessageId);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("ERROR: " + e.Message);
            return null;
        }
        return message;
    }
    
    
    
    /**
     * <summary>
     * Starts new chat session
     * </summary>
     *
     * <remarks>
     * Nothing more should be done after this task is called.
     * All possible exits and error parsing are fully covered.
     * </remarks>
     */
    public abstract Task ProcessConnection();
    
    
    /**
     * <summary>
     * Responsible for sending user messages
     * </summary>
     */
    protected abstract Task SendMessages();
    
    
    
    /**
     * <summary>
     * Send specified message to server, and in case of UDP also waits for confirmation.
     * </summary>
     */
    protected abstract Task Send(Message message);


    /**
     * <returns>
     * Reply object or null if nothing appropriate was received.
     * </returns>
     */
    protected abstract Task<ReplyM?> WaitReply();
    
    /**
     * <summary>
     * Responsible for receiving messages from server
     * </summary>
     */
    protected abstract Task ReceiveMessages();
    
    
    /**
     * <summary>
     * If possible, sends ByeM to server, and then exits the program with success.
     * </summary>
     */
    protected abstract Task SendBye();
    
    
    /**
     * <summary>
     * Prints local error message, then, if possible, sends ErrM to server, and then exits the program with error.
     * </summary>
     */
    protected abstract Task SendErr(string text);
    
    
    
    protected static void ChatHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("/help                                            Print this message");
        Console.WriteLine("/auth {login} {password} {display_name}          To authorize");
        Console.WriteLine("/rename {display_name)                           To change display name");
        Console.WriteLine("/join {channel_id}                               To join selected channel");
    }
    
    
}