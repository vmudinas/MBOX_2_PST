const fs = require('fs');
const readline = require('readline');

class MboxParser {
  constructor() {
    this.emails = [];
  }

  async parseMboxFile(filePath) {
    return new Promise((resolve, reject) => {
      if (!fs.existsSync(filePath)) {
        reject(new Error(`MBOX file not found: ${filePath}`));
        return;
      }

      const emails = [];
      let currentEmail = null;
      let currentLines = [];
      let inHeaders = false;

      const fileStream = fs.createReadStream(filePath);
      const rl = readline.createInterface({
        input: fileStream,
        crlfDelay: Infinity
      });

      rl.on('line', (line) => {
        // Check for message boundary (starts with "From ")
        if (line.startsWith('From ') && line.includes('@')) {
          // Save previous email if exists
          if (currentEmail && currentLines.length > 0) {
            currentEmail.body = currentLines.join('\n');
            emails.push(currentEmail);
          }

          // Start new email
          currentEmail = {
            from: '',
            to: '',
            subject: '',
            date: '',
            body: '',
            headers: {}
          };
          currentLines = [];
          inHeaders = true;
        } else if (currentEmail) {
          if (inHeaders) {
            // Parse headers
            if (line.trim() === '') {
              inHeaders = false;
            } else if (line.startsWith('From:')) {
              currentEmail.from = this.parseEmailHeader(line, 'From:');
            } else if (line.startsWith('To:')) {
              currentEmail.to = this.parseEmailHeader(line, 'To:');
            } else if (line.startsWith('Subject:')) {
              currentEmail.subject = this.parseEmailHeader(line, 'Subject:');
            } else if (line.startsWith('Date:')) {
              currentEmail.date = this.parseEmailHeader(line, 'Date:');
            } else if (line.includes(':')) {
              const colonIndex = line.indexOf(':');
              const headerName = line.substring(0, colonIndex).trim();
              const headerValue = line.substring(colonIndex + 1).trim();
              currentEmail.headers[headerName] = headerValue;
            }
          } else {
            // Add to body
            currentLines.push(line);
          }
        }
      });

      rl.on('close', () => {
        // Add the last email
        if (currentEmail && currentLines.length > 0) {
          currentEmail.body = currentLines.join('\n');
          emails.push(currentEmail);
        }

        console.log(`Parsed ${emails.length} emails from MBOX file`);
        resolve(emails);
      });

      rl.on('error', (error) => {
        reject(error);
      });
    });
  }

  parseEmailHeader(line, prefix) {
    return line.substring(prefix.length).trim();
  }

  // Extract email address from a header value
  extractEmailAddress(headerValue) {
    const emailMatch = headerValue.match(/<([^>]+)>/);
    if (emailMatch) {
      return emailMatch[1];
    }
    
    // If no angle brackets, try to find email pattern
    const simpleEmailMatch = headerValue.match(/\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/);
    if (simpleEmailMatch) {
      return simpleEmailMatch[0];
    }
    
    return headerValue.trim();
  }

  // Clean and format email content
  cleanEmailBody(body) {
    return body
      .replace(/^>+.*$/gm, '') // Remove quoted lines
      .replace(/^\s*$/gm, '') // Remove empty lines
      .trim();
  }
}

module.exports = MboxParser;