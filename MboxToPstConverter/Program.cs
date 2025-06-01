using MboxToPstConverter;

// MBOX to PST Converter (bidirectional)
if (args.Length != 2)
{
    Console.WriteLine("Usage: MboxToPstConverter <input> <output>");
    Console.WriteLine("  Supports bidirectional conversion:");
    Console.WriteLine("  - MBOX to PST: MboxToPstConverter input.mbox output.pst");
    Console.WriteLine("  - PST to MBOX: MboxToPstConverter input.pst output.mbox");
    Console.WriteLine();
    Console.WriteLine("  input   - Path to the input file (MBOX or PST)");
    Console.WriteLine("  output  - Path to the output file (PST or MBOX)");
    return 1;
}

string inputPath = args[0];
string outputPath = args[1];

// Validate input file exists
if (!File.Exists(inputPath))
{
    Console.WriteLine($"Error: Input file not found: {inputPath}");
    return 1;
}

// Determine conversion direction based on file extensions
string inputExtension = Path.GetExtension(inputPath).ToLowerInvariant();
string outputExtension = Path.GetExtension(outputPath).ToLowerInvariant();

bool isMboxToPst = inputExtension == ".mbox" && outputExtension == ".pst";
bool isPstToMbox = inputExtension == ".pst" && outputExtension == ".mbox";

if (!isMboxToPst && !isPstToMbox)
{
    Console.WriteLine("Error: Unsupported conversion direction.");
    Console.WriteLine("Supported conversions:");
    Console.WriteLine("  - MBOX to PST: input.mbox → output.pst");
    Console.WriteLine("  - PST to MBOX: input.pst → output.mbox");
    return 1;
}

// Ensure output directory exists
string? outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
{
    try
    {
        Directory.CreateDirectory(outputDir);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: Could not create output directory: {ex.Message}");
        return 1;
    }
}

try
{
    var converter = new Converter();
    
    if (isMboxToPst)
    {
        converter.ConvertMboxToPst(inputPath, outputPath);
    }
    else if (isPstToMbox)
    {
        converter.ConvertPstToMbox(inputPath, outputPath);
    }
    
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
