using System.Text;

namespace ipk25_chat.Messages;

/**
 * <summary>
 * Base class for all message types in IPK25CHAT protocol.
 * </summary>
 */
public abstract class Message
{
    /**
     * <remarks>
     * Type comparison should be done using e.g. <c>if (message is AuthM auth){}</c>,
     * this field is rather redundant.
     * </remarks>
     */
    public MessageTypeEnum Type;
    
    
    /**
     * <remarks>
     * <c>ushort</c> type is useful for exactly 2B identifiers that are required by protocol. 
     * </remarks>
     */
    public ushort MessageId;
    
    
    /**
     * <returns>
     * String representation of message in user readable form.
     * </returns>
     *
     * <exception cref="NotImplementedException">
     * When this type of message should not be printed. 
     * </exception>
     */
    public abstract string ToFormattedOutput();
    
    /**
     * Returns message as TCP IPK25CHAT compatible string.
     */
    public abstract string ToTcpString();

    /**
     * Returns message as UDP IPK25CHAT compatible byte array.
     */
    public abstract byte[] ToUdpBytes();
}