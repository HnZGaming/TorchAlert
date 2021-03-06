﻿using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Discord.Torch
{
    public static class DiscordUtils
    {
        public static Task MentionAsync(this ISocketMessageChannel self, ulong userId, string message)
        {
            var mention = MentionUtils.MentionUser(userId);
            return self.SendMessageAsync($"{mention} {message}");
        }

        public static string RemoveMentionPrefix(string message)
        {
            return Regex.Replace(message, @"<@!\d+>", "").Trim();
        }
    }
}