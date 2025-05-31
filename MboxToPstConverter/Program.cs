using MboxToPstConverter;

// MBOX to PST Converter
if (args.Length != 2)
{
    Console.WriteLine("Usage: MboxToPstConverter <input.mbox> <output.pst>");
    Console.WriteLine("  input.mbox  - Path to the input MBOX file");
    Console.WriteLine("  output.pst  - Path to the output PST file");
    return 1;
}

string mboxPath = args[0];
string pstPath = args[1];

// Validate input file exists
if (!File.Exists(mboxPath))
{
    Console.WriteLine($"Error: Input MBOX file not found: {mboxPath}");
    return 1;
}

// Ensure output directory exists
string? outputDir = Path.GetDirectoryName(pstPath);
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
    converter.ConvertMboxToPst(mboxPath, pstPath);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
