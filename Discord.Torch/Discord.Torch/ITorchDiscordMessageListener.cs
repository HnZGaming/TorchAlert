namespace Discord.Torch
{
    public interface ITorchDiscordMessageListener
    {
        bool TryRespond(ulong steamId, string message, out string response);
    }
}