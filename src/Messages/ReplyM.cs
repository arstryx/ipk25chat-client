

using System.Text;

namespace ipk25_chat.Messages;

public class ReplyM: Message
{
    private string MessageContent;
    public bool Status = false;
    public ushort RefMessageId;
    public ReplyM(bool status, string messageContent, ushort refMessageId = 0,  ushort messageId = 0)
    {
        GrammarCheck.CheckMessageContent(messageContent);
        
        this.MessageContent = messageContent;
        this.Status = status;
        this.RefMessageId = refMessageId;
        
        base.Type = MessageTypeEnum.Reply;
        
        this.MessageId = messageId;
    }

    public override string ToTcpString()
    {
        if (this.Status)
        {
            return $"REPLY OK IS {MessageContent}\r\n";
        }
        else
        {
            return $"REPLY NOK IS {MessageContent}\r\n";
        }
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)Type];
        byte[] id = BitConverter.GetBytes((ushort)MessageId);
        Array.Reverse(id);
        byte[] ok = [(byte)0];
        if (this.Status)
        {
            ok = [(byte)1];
        }
        byte[] refMessageId = BitConverter.GetBytes((ushort)RefMessageId);
        Array.Reverse(refMessageId);
        byte[] messageContent = Encoding.ASCII.GetBytes(this.MessageContent);
        
        return type.Concat(id).Concat(ok).Concat(refMessageId).Concat(messageContent).Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        if (this.Status)
        {
            return $"Action Success: {MessageContent}";
        }
        else
        {
            return $"Action Failure: {MessageContent}";
        }
    }
}