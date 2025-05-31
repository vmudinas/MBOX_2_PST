namespace MboxToPstConverter;

public class Converter
{
    private readonly MboxParser _mboxParser;
    private readonly PstWriter _pstWriter;

    public Converter()
    {
        _mboxParser = new MboxParser();
        _pstWriter = new PstWriter();
    }

    public void ConvertMboxToPst(string mboxFilePath, string pstFilePath)
    {
        Console.WriteLine($"Starting conversion from {mboxFilePath} to {pstFilePath}");

        try
        {
            // Parse MBOX file and get messages
            var messages = _mboxParser.ParseMboxFile(mboxFilePath);
            
            // Create PST file from messages
            _pstWriter.CreatePstFromMessages(messages, pstFilePath);
            
            Console.WriteLine("Conversion completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Conversion failed: {ex.Message}");
            throw;
        }
    }
}