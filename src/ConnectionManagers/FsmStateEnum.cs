namespace ipk25_chat.ConnectionManagers;

public enum FsmStateEnum
{
    START = 0,
    AUTH = 1,       // redundant in current implementation
    OPEN = 2,
    JOIN = 3,       // redundant in current implementation
    END = 4,        // redundant in current implementation
}