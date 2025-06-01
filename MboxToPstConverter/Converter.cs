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
        Console.WriteLine($"Starting conversion from {Path.GetFileName(mboxFilePath)} to {Path.GetFileName(pstFilePath)}");
        Console.WriteLine($"Input: {mboxFilePath}");
        Console.WriteLine($"Output: {pstFilePath}");
        Console.WriteLine();

        try
        {
            // Parse MBOX file and get messages
            var messages = _mboxParser.ParseMboxFile(mboxFilePath);
            
            Console.WriteLine();
            
            // Create PST file from messages
            _pstWriter.CreatePstFromMessages(messages, pstFilePath);
            
            Console.WriteLine();
            Console.WriteLine("Conversion completed successfully!");
            Console.WriteLine($"PST file created: {pstFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Conversion failed: {ex.Message}");
            throw;
        }
    }
}