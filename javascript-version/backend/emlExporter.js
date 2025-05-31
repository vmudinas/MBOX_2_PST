const fs = require('fs');
const path = require('path');

class EmlExporter {
  constructor() {
    this.exportDir = './exports';
  }

  // Create export directory if it doesn't exist
  ensureExportDir() {
    if (!fs.existsSync(this.exportDir)) {
      fs.mkdirSync(this.exportDir, { recursive: true });
    }
  }

  // Convert a single email to EML format
  emailToEml(email, index) {
    const lines = [];
    
    // Add basic headers
    if (email.from) lines.push(`From: ${email.from}`);
    if (email.to) lines.push(`To: ${email.to}`);
    if (email.subject) lines.push(`Subject: ${email.subject}`);
    if (email.date) lines.push(`Date: ${email.date}`);
    
    // Add additional headers from the original email
    for (const [headerName, headerValue] of Object.entries(email.headers || {})) {
      // Skip headers we already added
      if (!['From', 'To', 'Subject', 'Date'].includes(headerName)) {
        lines.push(`${headerName}: ${headerValue}`);
      }
    }
    
    // Add content type if not present
    const hasContentType = Object.keys(email.headers || {}).some(h => 
      h.toLowerCase() === 'content-type'
    );
    if (!hasContentType) {
      lines.push('Content-Type: text/plain; charset=utf-8');
    }
    
    // Add Message-ID if not present
    const hasMessageId = Object.keys(email.headers || {}).some(h => 
      h.toLowerCase() === 'message-id'
    );
    if (!hasMessageId) {
      const messageId = `<email_${index}_${Date.now()}@mbox2pst.local>`;
      lines.push(`Message-ID: ${messageId}`);
    }
    
    // Empty line to separate headers from body
    lines.push('');
    
    // Add body
    if (email.body) {
      lines.push(email.body);
    }
    
    return lines.join('\r\n');
  }

  // Export all emails to EML files
  async exportToEml(emails, originalFilename) {
    this.ensureExportDir();
    
    const baseFilename = path.basename(originalFilename, path.extname(originalFilename));
    const exportPath = path.join(this.exportDir, `${baseFilename}_emails`);
    
    // Create subdirectory for this export
    if (!fs.existsSync(exportPath)) {
      fs.mkdirSync(exportPath, { recursive: true });
    }
    
    const exportedFiles = [];
    
    for (let i = 0; i < emails.length; i++) {
      const email = emails[i];
      const emlContent = this.emailToEml(email, i);
      
      // Create safe filename from subject or use index
      let safeSubject = email.subject ? 
        email.subject.replace(/[^a-zA-Z0-9\s]/g, '').substring(0, 50) : 
        `Email_${i + 1}`;
      
      const filename = `${String(i + 1).padStart(3, '0')}_${safeSubject}.eml`;
      const filePath = path.join(exportPath, filename);
      
      fs.writeFileSync(filePath, emlContent, 'utf8');
      exportedFiles.push({
        index: i,
        filename: filename,
        path: filePath,
        subject: email.subject || 'No Subject'
      });
    }
    
    return {
      exportPath: exportPath,
      emailCount: emails.length,
      files: exportedFiles
    };
  }

  // Create a ZIP archive of the EML files
  async createZipArchive(exportResult, originalFilename) {
    const archiver = require('archiver');
    const baseFilename = path.basename(originalFilename, path.extname(originalFilename));
    const zipPath = path.join(this.exportDir, `${baseFilename}_emails.zip`);
    
    return new Promise((resolve, reject) => {
      const output = fs.createWriteStream(zipPath);
      const archive = archiver('zip', {
        zlib: { level: 9 } // Best compression
      });
      
      output.on('close', () => {
        resolve({
          zipPath: zipPath,
          size: archive.pointer(),
          emailCount: exportResult.emailCount
        });
      });
      
      archive.on('error', (err) => {
        reject(err);
      });
      
      archive.pipe(output);
      
      // Add all EML files to the archive
      for (const file of exportResult.files) {
        archive.file(file.path, { name: file.filename });
      }
      
      archive.finalize();
    });
  }
}

module.exports = EmlExporter;