using System.Text.RegularExpressions;

namespace ipk25_chat.Messages;

/**
 * <summary>
 * Contains static methods to check grammar of individual message parameters
 * </summary>
 *
 * <exception cref="ArgumentException">
 * In case of any incorrect grammar
 * </exception>
 */
public static class GrammarCheck
{
    public static void CheckUsername(string username)
    {
        if (Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$") && username.Length <= 20 && username.Length >= 1)
        {
            return;
        }
        throw new ArgumentException("Invalid user id.");
    }

    public static void CheckChannelId(string channelId)
    {
        if (Regex.IsMatch(channelId, @"^([a-zA-Z0-9_-]+\.)*[a-zA-Z0-9_-]+$") && channelId.Length <= 20 && channelId.Length >= 1)
        {
            return;
        }
        throw new ArgumentException("Invalid channel id.");
    }

    public static void CheckSecret(string secret)
    {
        if (Regex.IsMatch(secret, @"^[a-zA-Z0-9_-]+$") && secret.Length <= 128 && secret.Length >= 1)
        {
            return;
        }
        throw new ArgumentException("Invalid secret.");
    }

    public static void CheckDisplayName(string displayName)
    {
        if (displayName.Length <= 20 && Regex.IsMatch(displayName, @"^[\x21-\x7E]+$") && displayName.Length >= 1)
        {
            return;
        }
        throw new ArgumentException("Invalid display name.");
    }

    public static void CheckMessageContent(string content)
    {
        if (content.Length <= 60000 && Regex.IsMatch(content, @"^[\x0A\x20-\x7E]*$") && content.Length >= 1)
        {
            return;
        }
        throw new ArgumentException("Invalid message content.");
    }
    
}