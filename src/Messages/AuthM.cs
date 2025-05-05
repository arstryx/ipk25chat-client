

using System.Text;

namespace ipk25_chat.Messages;

public class AuthM : Message
{
    public readonly string Username;
    public readonly string DisplayName;
    public readonly string Secret;

    // Constructor
    public AuthM(string username, string displayName, string secret, ushort messageId = 0)
    {
        GrammarCheck.CheckUsername(username);
        GrammarCheck.CheckDisplayName(displayName);
        GrammarCheck.CheckSecret(secret);

        Username = username;
        DisplayName = displayName;
        Secret = secret;

        Type = MessageTypeEnum.Auth;
        
        MessageId = messageId;
    }

    public override string ToTcpString()
    {
        return "AUTH " + Username + " AS " + DisplayName + " USING " + Secret + "\r\n";
    }

    public override byte[] ToUdpBytes()
    {
        byte[] type = [(byte)this.Type];
        byte[] id = BitConverter.GetBytes((ushort)MessageId); // need 2 bytes
        Array.Reverse(id);
        byte[] username = Encoding.ASCII.GetBytes(this.Username);
        byte[] displayname = Encoding.ASCII.GetBytes(this.DisplayName);
        byte[] secret = Encoding.ASCII.GetBytes(this.Secret);

        return type.Concat(id).Concat(username).Concat([(byte)0]).Concat(displayname).Concat([(byte)0]).Concat(secret)
            .Concat([(byte)0]).ToArray();
    }

    public override string ToFormattedOutput()
    {
        throw new NotImplementedException();
    }
}