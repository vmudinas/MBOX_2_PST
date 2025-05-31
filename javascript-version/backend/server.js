const express = require('express');
const multer = require('multer');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const MboxParser = require('./mboxParser');
const EmlExporter = require('./emlExporter');

const app = express();
const port = 3001;

// Middleware
app.use(cors());
app.use(express.json());

// Configure multer for file uploads
const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    const uploadDir = './uploads';
    if (!fs.existsSync(uploadDir)) {
      fs.mkdirSync(uploadDir, { recursive: true });
    }
    cb(null, uploadDir);
  },
  filename: (req, file, cb) => {
    cb(null, file.originalname);
  }
});

const upload = multer({ storage: storage });

// Initialize MBOX parser and EML exporter
const mboxParser = new MboxParser();
const emlExporter = new EmlExporter();

// Routes

// Upload MBOX file and parse it
app.post('/api/upload-mbox', upload.single('mboxFile'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'No file uploaded' });
    }

    const filePath = req.file.path;
    console.log(`Processing MBOX file: ${filePath}`);

    // Parse the MBOX file
    const emails = await mboxParser.parseMboxFile(filePath);
    
    res.json({
      success: true,
      filename: req.file.originalname,
      emailCount: emails.length,
      emails: emails.map((email, index) => ({
        id: index,
        from: email.from,
        to: email.to,
        subject: email.subject,
        date: email.date,
        preview: email.body ? email.body.substring(0, 200) + '...' : ''
      }))
    });

  } catch (error) {
    console.error('Error processing MBOX file:', error);
    res.status(500).json({ error: error.message });
  }
});

// Get full email content
app.get('/api/email/:filename/:id', async (req, res) => {
  try {
    const { filename, id } = req.params;
    const filePath = path.join('./uploads', filename);
    
    if (!fs.existsSync(filePath)) {
      return res.status(404).json({ error: 'MBOX file not found' });
    }

    const emails = await mboxParser.parseMboxFile(filePath);
    const emailIndex = parseInt(id);
    
    if (emailIndex < 0 || emailIndex >= emails.length) {
      return res.status(404).json({ error: 'Email not found' });
    }

    res.json(emails[emailIndex]);

  } catch (error) {
    console.error('Error getting email:', error);
    res.status(500).json({ error: error.message });
  }
});

// Simulate PST conversion
app.post('/api/convert-to-pst', async (req, res) => {
  try {
    const { filename, emailCount } = req.body;
    
    // Simulate conversion process with progress updates
    res.json({
      success: true,
      message: 'PST conversion started',
      conversionId: Date.now().toString()
    });

  } catch (error) {
    console.error('Error starting conversion:', error);
    res.status(500).json({ error: error.message });
  }
});

// Get conversion progress (simulated)
app.get('/api/conversion-progress/:id', (req, res) => {
  // Simulate progress
  const progress = Math.min(100, Math.floor(Math.random() * 100));
  
  res.json({
    conversionId: req.params.id,
    progress: progress,
    status: progress < 100 ? 'converting' : 'completed',
    message: progress < 100 ? 'Converting emails...' : 'Conversion completed!'
  });
});

// Export to EML files
app.post('/api/export-to-eml', async (req, res) => {
  try {
    const { filename } = req.body;
    
    if (!filename) {
      return res.status(400).json({ error: 'Filename is required' });
    }
    
    const filePath = path.join('./uploads', filename);
    
    if (!fs.existsSync(filePath)) {
      return res.status(404).json({ error: 'MBOX file not found' });
    }

    // Parse the MBOX file
    const emails = await mboxParser.parseMboxFile(filePath);
    
    // Export to EML files
    const exportResult = await emlExporter.exportToEml(emails, filename);
    
    // Create ZIP archive
    const zipResult = await emlExporter.createZipArchive(exportResult, filename);
    
    res.json({
      success: true,
      message: 'EML export completed',
      exportPath: exportResult.exportPath,
      zipPath: zipResult.zipPath,
      emailCount: zipResult.emailCount,
      zipSize: zipResult.size,
      downloadUrl: `/api/download-export/${path.basename(zipResult.zipPath)}`
    });

  } catch (error) {
    console.error('Error exporting to EML:', error);
    res.status(500).json({ error: error.message });
  }
});

// Download exported EML ZIP file
app.get('/api/download-export/:filename', (req, res) => {
  try {
    const filename = req.params.filename;
    const filePath = path.join('./exports', filename);
    
    if (!fs.existsSync(filePath)) {
      return res.status(404).json({ error: 'Export file not found' });
    }
    
    res.download(filePath, filename, (err) => {
      if (err) {
        console.error('Error downloading file:', err);
        res.status(500).json({ error: 'Error downloading file' });
      }
    });
    
  } catch (error) {
    console.error('Error downloading export:', error);
    res.status(500).json({ error: error.message });
  }
});

// Health check
app.get('/api/health', (req, res) => {
  res.json({ status: 'OK', message: 'MBOX to PST Backend API is running' });
});

app.listen(port, () => {
  console.log(`MBOX to PST Backend API listening at http://localhost:${port}`);
});