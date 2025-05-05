namespace ipk25_chat.Messages;

public class PingM: Message
{
    public PingM(ushort id)
    {
        Type = MessageTypeEnum.Ping;
        MessageId = id;
    }

    public override string ToTcpString()
    {
        throw new NotImplementedException();
    }

    public override byte[] ToUdpBytes()
    {
        throw new NotImplementedException();
    }

    public override string ToFormattedOutput()
    {
        throw new NotImplementedException();
    }
    
    
}