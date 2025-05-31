using MimeKit;
using System.Text;

namespace MboxToPstConverter;

public class MboxParser
{
    public IEnumerable<MimeMessage> ParseMboxFile(string mboxFilePath)
    {
        if (!File.Exists(mboxFilePath))
        {
            throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
        }

        using var stream = File.OpenRead(mboxFilePath);
        var parser = new MimeParser(stream, MimeFormat.Mbox);

        while (!parser.IsEndOfStream)
        {
            var message = parser.ParseMessage();
            if (message != null)
            {
                yield return message;
            }
        }
    }
}