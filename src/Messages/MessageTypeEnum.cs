namespace ipk25_chat;

public enum MessageTypeEnum
{
    Auth = 0x02,
    Bye = 0xFF,
    Err = 0xFE,
    Join = 0x03,
    Msg = 0x04,
    Reply = 0x01,
    Confirm = 0x00,
    Ping = 0xfd,
}