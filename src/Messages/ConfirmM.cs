namespace ipk25_chat.Messages;

public class ConfirmM: Message
{
    public ushort RefMessageId;

    public ConfirmM(ushort refMessageId,  ushort messageId = 0)
    {
        this.RefMessageId = refMessageId;
        
        MessageId = messageId;
    }

    public override string ToTcpString()
    {
        throw new NotImplementedException();
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] refMessageId = BitConverter.GetBytes(this.RefMessageId);
        Array.Reverse(refMessageId);
        return type.Concat(refMessageId).ToArray();
    }

    public override string ToFormattedOutput()
    {
        throw new NotImplementedException();
    }
}