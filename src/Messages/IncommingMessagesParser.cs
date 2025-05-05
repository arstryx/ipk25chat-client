using System.Text;

namespace ipk25_chat.Messages;



/**
 * <summary>
 * Used to translate incoming messages as strings for TCP or
 * bytes for UDP into appropriate Message objects.
 * </summary>
 */
public static class IncomingMessagesParser
{
    public static Message TranslateIncomingTcpMessage(string message)
    {
        message = message.TrimEnd("\r\n".ToCharArray());
        string[] words = message.Split(' ');

        switch (words[0])
        {
            case "MSG":
                if (words.Length < 5 || words[1] != "FROM" || words[3] != "IS")
                {
                    throw new ArgumentException("Invalid MSG format");
                }
                string sender = words[2];
                string content = string.Join(' ', words.Skip(4));
                return new MsgM(sender, content);

            case "REPLY":
                if (words.Length < 4 || words[2] != "IS")
                {
                    throw new ArgumentException("Invalid REPLY format");
                }
                bool status = words[1] == "OK" ? true :
                    words[1] == "NOK" ? false :
                    throw new ArgumentException("Invalid REPLY status");
                string replyText = string.Join(' ', words.Skip(3));
                return new ReplyM(status, replyText);

            case "ERR":
                if (words.Length < 5 || words[1] != "FROM" || words[3] != "IS")
                {
                    throw new ArgumentException("Invalid ERR format");
                }
                string code = words[2];
                string errText = string.Join(' ', words.Skip(4));
                return new ErrM(code, errText);

            case "BYE":
                if (words.Length < 3 || words[1] != "FROM")
                {
                    throw new ArgumentException("Invalid BYE format");
                }
                string byeText = string.Join(' ', words.Skip(2));
                return new ByeM(byeText);
        }
        throw new Exception("Unknown message type");
    }

    public static Message TranslateIncomingUdpMessage(byte[] message)
    {
        if (message.Length != 0)
        {
            ushort messageId = (ushort)((message[1] << 8) | message[2]);
            
            if (message[0] == (byte)MessageTypeEnum.Msg)
            {
                int displayNameStart = 3;
                int displayNameEnd = Array.IndexOf(message, (byte)0, displayNameStart);
                string displayName =
                    Encoding.ASCII.GetString(message, displayNameStart, displayNameEnd - displayNameStart);

                int messageBodyStart = displayNameEnd + 1;
                int messageBodyEnd = Array.IndexOf(message, (byte)0, messageBodyStart);
                string messageBody =
                    Encoding.ASCII.GetString(message, messageBodyStart, messageBodyEnd - messageBodyStart);

                MsgM msg = new MsgM(displayName, messageBody, messageId);
                return msg;
            }
            else if (message[0] == (byte)MessageTypeEnum.Reply)
            {
                bool result = message[3] == 1;
                ushort refId = (ushort)((message[4] << 8) | message[5]);
                int msgContentStart = 6;
                int msgContentEnd = Array.IndexOf(message, (byte)0, msgContentStart);
                string msgContent = Encoding.ASCII.GetString(message, msgContentStart, msgContentEnd - msgContentStart);
                ReplyM reply = new ReplyM(result, msgContent, refId, messageId);
                return reply;
            }
            else if (message[0] == (byte)MessageTypeEnum.Confirm)
            {
                ConfirmM confirm = new ConfirmM(messageId);
                return confirm;
            }
            else if (message[0] == (byte)MessageTypeEnum.Err)
            {
                int displayNameStart = 3;
                int displayNameEnd = Array.IndexOf(message, (byte)0, displayNameStart);
                string displayName =
                    Encoding.ASCII.GetString(message, displayNameStart, displayNameEnd - displayNameStart);

                int messageBodyStart = displayNameEnd + 1;
                int messageBodyEnd = Array.IndexOf(message, (byte)0, messageBodyStart);
                string messageBody =
                    Encoding.ASCII.GetString(message, messageBodyStart, messageBodyEnd - messageBodyStart);

                ErrM err = new ErrM(displayName, messageBody, messageId);
                return err;
            }
            else if (message[0] == (byte)MessageTypeEnum.Bye)
            {
                int displayNameStart = 3;
                int displayNameEnd = Array.IndexOf(message, (byte)0, displayNameStart);
                string displayName =
                    Encoding.ASCII.GetString(message, displayNameStart, displayNameEnd - displayNameStart);



                ByeM bye = new ByeM(displayName, messageId);
                return bye;
            }
            else if (message[0] == (byte)MessageTypeEnum.Ping)
            {
                PingM ping = new PingM(messageId);
                return ping;
            }
        }
        throw new ArgumentException("Invalid message length");
    }
}