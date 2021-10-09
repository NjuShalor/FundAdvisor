using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal static class TelegramBotExtensions
    {
        // ParseMode for Markdown or MarkdownV2 require escape string
        public static string GetEscapedMessage(string message)
        {
            string[] specialCharacters = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (string ch in specialCharacters)
            {
                message = message.Replace(ch, $"\\{ch}");
            }

            return message;
        }

        public static async Task SendTextMessageWithDocumentFallbackAsync(
            this ITelegramBotClient botClient,
            ChatId chatId,
            string title,
            string text,
            ParseMode parseMode = ParseMode.Default,
            int replyToMessageId = default)
        {
            if (text.Length <= 4096)
            {
                string separator = new string('-', 50);
                string messageToSend = $"{title}\n{separator}\n{text}";
                await botClient.SendTextMessageAsync(chatId, messageToSend, parseMode, replyToMessageId: replyToMessageId);
            }
            else
            {
                string filename = $"{title}--{DateTime.Now:yyyy-MM-dd-HH-mm-ss.fffffffK}.txt";
                byte[] byteArray = Encoding.UTF8.GetBytes(text);
                MemoryStream stream = new MemoryStream(byteArray);
                InputOnlineFile file = new InputOnlineFile(stream, filename);
                await botClient.SendDocumentAsync(chatId, file, replyToMessageId: replyToMessageId);
            }
        }
    }
}
