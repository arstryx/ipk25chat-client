using System.Net;
using System.Text;

namespace ipk25_chat.Messages;

public class ByeM: Message
{
    string DisplayName;
    
    public ByeM(string displayName,  ushort messageId = 0)
    {
        GrammarCheck.CheckDisplayName(displayName);
        
        this.DisplayName = displayName;
        
        base.Type = MessageTypeEnum.Bye;
        
        MessageId = messageId;
    }
    
    public override string ToTcpString()
    {
        return "BYE FROM " + this.DisplayName + "\r\n";
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] id = BitConverter.GetBytes((ushort)MessageId);
        Array.Reverse(id);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        
        return type.Concat(id).Concat(displayName).Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        throw new NotImplementedException();
    }
}