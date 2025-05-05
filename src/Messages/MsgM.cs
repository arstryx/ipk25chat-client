

using System.Text;

namespace ipk25_chat.Messages;

public class MsgM: Message
{
    private string DisplayName;
    private string MessageContent;
    
    public MsgM(string displayName, string messageContent,  ushort messageId = 0)
    {
        GrammarCheck.CheckDisplayName(displayName);
        GrammarCheck.CheckMessageContent(messageContent);
        
        this.DisplayName = displayName;
        this.MessageContent = messageContent;

        base.Type = MessageTypeEnum.Msg;
        
        this.MessageId = messageId;
    }
    
    public override string ToTcpString()
    {
        return "MSG FROM " + this.DisplayName + " IS " + this.MessageContent + "\r\n";
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] id = BitConverter.GetBytes(MessageId);
        Array.Reverse(id);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        byte[] messageContent = Encoding.ASCII.GetBytes(this.MessageContent);
        
        return type.Concat(id).Concat(displayName).Concat([(byte)0]).Concat(messageContent).Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        return this.DisplayName + ": " + this.MessageContent;
    }
}