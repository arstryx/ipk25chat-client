

using System.Text;

namespace ipk25_chat.Messages;

public class ErrM: Message
{
    private string DisplayName;
    private string MessageContent;

    public ErrM(string displayName, string messageContent,  ushort messageId = 0)
    {
        GrammarCheck.CheckDisplayName(displayName);
        GrammarCheck.CheckMessageContent(messageContent);
        
        this.DisplayName = displayName;
        this.MessageContent = messageContent;
        
        base.Type = MessageTypeEnum.Err;
        
        MessageId = messageId;
    }

    public override string ToTcpString()
    {
        return $"ERR FROM {DisplayName} IS {MessageContent}\r\n";
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] id = BitConverter.GetBytes((ushort)MessageId);
        Array.Reverse(id);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        byte[] messageContent = Encoding.ASCII.GetBytes(this.MessageContent);
        
        return type.Concat(id).Concat(displayName).Concat([(byte)0]).Concat(messageContent).Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        return $"ERROR FROM {DisplayName}: {MessageContent}";
    }
}