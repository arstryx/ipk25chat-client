

using System.Text;

namespace ipk25_chat.Messages;

public class JoinM: Message
{
    private string ChannelId;
    private string DisplayName;

    public JoinM(string channelId, string displayName,  ushort messageId = 0)
    {
        GrammarCheck.CheckChannelId(channelId);
        GrammarCheck.CheckDisplayName(displayName);
         
        this.ChannelId = channelId;
        this.DisplayName = displayName;
        
        base.Type = MessageTypeEnum.Join;
        
        MessageId = messageId;
    }
    
    public override string ToTcpString()
    {
        return "JOIN " + this.ChannelId + " AS " + this.DisplayName + "\r\n";
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] id = BitConverter.GetBytes((ushort)MessageId);
        Array.Reverse(id);
        byte[] channelId = Encoding.ASCII.GetBytes(this.ChannelId);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        
        return type.Concat(id).Concat(channelId).Concat([(byte)0]).Concat(displayName).Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        throw new NotImplementedException();
    }
}