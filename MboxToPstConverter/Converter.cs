namespace MboxToPstConverter;

public class Converter
{
    private readonly MboxParser _mboxParser;
    private readonly PstWriter _pstWriter;
    private readonly PstReader _pstReader;
    private readonly MboxWriter _mboxWriter;

    public Converter()
    {
        _mboxParser = new MboxParser();
        _pstWriter = new PstWriter();
        _pstReader = new PstReader();
        _mboxWriter = new MboxWriter();
    }

    public void ConvertMboxToPst(string mboxFilePath, string pstFilePath)
    {
        ConvertMboxToPst(mboxFilePath, pstFilePath, null, null);
    }

    public void ConvertMboxToPst(string mboxFilePath, string pstFilePath, IProgress<MboxParsingProgress>? parsingProgress, IProgress<PstConversionProgress>? conversionProgress)
    {
        Console.WriteLine($"Starting conversion from {Path.GetFileName(mboxFilePath)} to {Path.GetFileName(pstFilePath)}");
        Console.WriteLine($"Input: {mboxFilePath}");
        Console.WriteLine($"Output: {pstFilePath}");
        Console.WriteLine();

        try
        {
            // Parse MBOX file and get messages
            var messages = _mboxParser.ParseMboxFile(mboxFilePath, parsingProgress);
            
            Console.WriteLine();
            
            // Create PST file from messages
            _pstWriter.CreatePstFromMessages(messages, pstFilePath, conversionProgress);
            
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

    public void ConvertPstToMbox(string pstFilePath, string mboxFilePath)
    {
        Console.WriteLine($"Starting conversion from {Path.GetFileName(pstFilePath)} to {Path.GetFileName(mboxFilePath)}");
        Console.WriteLine($"Input: {pstFilePath}");
        Console.WriteLine($"Output: {mboxFilePath}");
        Console.WriteLine();

        try
        {
            // Parse PST file and get messages
            var messages = _pstReader.ParsePstFile(pstFilePath);
            
            Console.WriteLine();
            
            // Create MBOX file from messages
            _mboxWriter.CreateMboxFromMessages(messages, mboxFilePath);
            
            Console.WriteLine();
            Console.WriteLine("Conversion completed successfully!");
            Console.WriteLine($"MBOX file created: {mboxFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Conversion failed: {ex.Message}");
            throw;
        }
    }
}