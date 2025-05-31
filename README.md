# MBOX_2_PST
Mbox to pst converter

A multi-platform solution for converting MBOX files to PST format.

## Available Implementations

### C# Version (Production Ready)
- .NET console application that converts MBOX files to PST format
- Uses MimeKit for MBOX parsing and Aspose.Email for PST creation
- Command-line interface for easy automation
- Preserves email metadata, attachments, and formatting

### JavaScript Version (Demo/Viewing)
- Node.js backend for MBOX parsing with React frontend
- Web interface for viewing MBOX files and emails
- Simulated conversion process demonstration
- File upload and email browsing capabilities

## C# Version

### Features

- Converts MBOX files to PST format
- Preserves email metadata (sender, recipients, subject, date)
- Handles email attachments
- Command-line interface for easy automation
- Error handling and validation

### Requirements

- .NET 8.0 or later

### Usage

```bash
cd MboxToPstConverter
dotnet run <input.mbox> <output.pst>
```

#### Parameters

- `input.mbox` - Path to the input MBOX file
- `output.pst` - Path to the output PST file

#### Example

```bash
dotnet run emails.mbox converted_emails.pst
```

### Building

```bash
cd MboxToPstConverter
dotnet build
```

### Dependencies

- MimeKit - for parsing MBOX files
- Aspose.Email - for creating PST files

## JavaScript Version

### Features

- Upload and parse MBOX files through web interface
- View email list with metadata (sender, subject, date)
- View full email content including headers and body
- Simulated PST conversion process with progress tracking
- Responsive React-based user interface

### Requirements

- Node.js 16+ and npm
- Modern web browser

### Quick Start

```bash
# Start backend API
cd javascript-version/backend
npm install
npm start

# Start frontend (in another terminal)
cd javascript-version/frontend
npm install
npm start
```

Then open http://localhost:3000 in your browser.

### Note on JavaScript PST Creation

The JavaScript version provides MBOX parsing and viewing capabilities but cannot create actual PST files, as PST is a proprietary Microsoft format requiring specialized libraries not readily available in JavaScript. For actual PST file creation, use the C# version.

See [javascript-version/README.md](javascript-version/README.md) for detailed JavaScript setup and usage instructions.

## License

This project is provided as-is for educational and personal use.
