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
        var startTime = DateTime.Now;
        Console.WriteLine("=== MBOX to PST Conversion Started ===");
        Console.WriteLine($"Start time: {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Input file: {mboxFilePath}");
        Console.WriteLine($"Output file: {pstFilePath}");
        
        // Validate input file
        if (!File.Exists(mboxFilePath))
        {
            throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
        }
        
        var inputFileInfo = new FileInfo(mboxFilePath);
        Console.WriteLine($"Input file size: {inputFileInfo.Length / 1024.0 / 1024.0:F2} MB");
        
        // Check output directory
        var outputDir = Path.GetDirectoryName(pstFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Console.WriteLine($"Creating output directory: {outputDir}");
            Directory.CreateDirectory(outputDir);
        }
        
        // Check for existing output file
        if (File.Exists(pstFilePath))
        {
            Console.WriteLine($"Warning: Output file already exists and will be overwritten: {pstFilePath}");
        }
        
        Console.WriteLine();

        try
        {
            // Step 1: Parse MBOX file and get messages
            Console.WriteLine("Step 1: Parsing MBOX file...");
            var parseStartTime = DateTime.Now;
            var messages = _mboxParser.ParseMboxFile(mboxFilePath, parsingProgress);
            var parseEndTime = DateTime.Now;
            var parseDuration = parseEndTime - parseStartTime;
            
            Console.WriteLine($"MBOX parsing completed in {parseDuration.TotalSeconds:F2} seconds");
            Console.WriteLine();
            
            // Step 2: Create PST file from messages
            Console.WriteLine("Step 2: Creating PST file from parsed messages...");
            var conversionStartTime = DateTime.Now;
            _pstWriter.CreatePstFromMessages(messages, pstFilePath, conversionProgress);
            var conversionEndTime = DateTime.Now;
            var conversionDuration = conversionEndTime - conversionStartTime;
            
            Console.WriteLine($"PST creation completed in {conversionDuration.TotalSeconds:F2} seconds");
            Console.WriteLine();
            
            // Step 3: Validate output file
            Console.WriteLine("Step 3: Validating output file...");
            if (File.Exists(pstFilePath))
            {
                var outputFileInfo = new FileInfo(pstFilePath);
                Console.WriteLine($"PST file created successfully: {pstFilePath}");
                Console.WriteLine($"Output file size: {outputFileInfo.Length / 1024.0 / 1024.0:F2} MB");
                
                var totalDuration = DateTime.Now - startTime;
                Console.WriteLine($"Total conversion time: {totalDuration.TotalSeconds:F2} seconds");
                Console.WriteLine($"Average processing speed: {inputFileInfo.Length / 1024.0 / 1024.0 / totalDuration.TotalSeconds:F2} MB/sec");
            }
            else
            {
                throw new InvalidOperationException("PST file was not created successfully");
            }
            
            Console.WriteLine();
            Console.WriteLine("=== MBOX to PST Conversion Completed Successfully ===");
        }
        catch (Exception ex)
        {
            var errorTime = DateTime.Now;
            var errorDuration = errorTime - startTime;
            Console.WriteLine();
            Console.WriteLine("=== MBOX to PST Conversion Failed ===");
            Console.WriteLine($"Error occurred after {errorDuration.TotalSeconds:F2} seconds");
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public void ConvertPstToMbox(string pstFilePath, string mboxFilePath)
    {
        var startTime = DateTime.Now;
        Console.WriteLine("=== PST to MBOX Conversion Started ===");
        Console.WriteLine($"Start time: {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Input file: {pstFilePath}");
        Console.WriteLine($"Output file: {mboxFilePath}");
        
        // Validate input file
        if (!File.Exists(pstFilePath))
        {
            throw new FileNotFoundException($"PST file not found: {pstFilePath}");
        }
        
        var inputFileInfo = new FileInfo(pstFilePath);
        Console.WriteLine($"Input file size: {inputFileInfo.Length / 1024.0 / 1024.0:F2} MB");
        
        // Check output directory
        var outputDir = Path.GetDirectoryName(mboxFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Console.WriteLine($"Creating output directory: {outputDir}");
            Directory.CreateDirectory(outputDir);
        }
        
        // Check for existing output file
        if (File.Exists(mboxFilePath))
        {
            Console.WriteLine($"Warning: Output file already exists and will be overwritten: {mboxFilePath}");
        }
        
        Console.WriteLine();

        try
        {
            // Step 1: Parse PST file and get messages
            Console.WriteLine("Step 1: Parsing PST file...");
            var parseStartTime = DateTime.Now;
            var messages = _pstReader.ParsePstFile(pstFilePath);
            var parseEndTime = DateTime.Now;
            var parseDuration = parseEndTime - parseStartTime;
            
            Console.WriteLine($"PST parsing completed in {parseDuration.TotalSeconds:F2} seconds");
            Console.WriteLine();
            
            // Step 2: Create MBOX file from messages
            Console.WriteLine("Step 2: Creating MBOX file from parsed messages...");
            var conversionStartTime = DateTime.Now;
            _mboxWriter.CreateMboxFromMessages(messages, mboxFilePath);
            var conversionEndTime = DateTime.Now;
            var conversionDuration = conversionEndTime - conversionStartTime;
            
            Console.WriteLine($"MBOX creation completed in {conversionDuration.TotalSeconds:F2} seconds");
            Console.WriteLine();
            
            // Step 3: Validate output file
            Console.WriteLine("Step 3: Validating output file...");
            if (File.Exists(mboxFilePath))
            {
                var outputFileInfo = new FileInfo(mboxFilePath);
                Console.WriteLine($"MBOX file created successfully: {mboxFilePath}");
                Console.WriteLine($"Output file size: {outputFileInfo.Length / 1024.0 / 1024.0:F2} MB");
                
                var totalDuration = DateTime.Now - startTime;
                Console.WriteLine($"Total conversion time: {totalDuration.TotalSeconds:F2} seconds");
                Console.WriteLine($"Average processing speed: {inputFileInfo.Length / 1024.0 / 1024.0 / totalDuration.TotalSeconds:F2} MB/sec");
            }
            else
            {
                throw new InvalidOperationException("MBOX file was not created successfully");
            }
            
            Console.WriteLine();
            Console.WriteLine("=== PST to MBOX Conversion Completed Successfully ===");
        }
        catch (Exception ex)
        {
            var errorTime = DateTime.Now;
            var errorDuration = errorTime - startTime;
            Console.WriteLine();
            Console.WriteLine("=== PST to MBOX Conversion Failed ===");
            Console.WriteLine($"Error occurred after {errorDuration.TotalSeconds:F2} seconds");
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}