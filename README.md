# MBOX_2_PST
Mbox to pst converter

A .NET console application that converts MBOX files to PST format.

## Features

- Converts MBOX files to PST format
- Preserves email metadata (sender, recipients, subject, date)
- Handles email attachments
- Command-line interface for easy automation
- Error handling and validation

## Requirements

- .NET 8.0 or later

## Usage

```bash
dotnet run <input.mbox> <output.pst>
```

### Parameters

- `input.mbox` - Path to the input MBOX file
- `output.pst` - Path to the output PST file

### Example

```bash
dotnet run emails.mbox converted_emails.pst
```

## Building

```bash
cd MboxToPstConverter
dotnet build
```

## Dependencies

- MimeKit - for parsing MBOX files
- Aspose.Email - for creating PST files

## License

This project is provided as-is for educational and personal use.
