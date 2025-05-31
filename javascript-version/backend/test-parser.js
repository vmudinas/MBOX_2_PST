// Simple test to create a mock MBOX file and test our parser
const fs = require('fs');
const path = require('path');
const MboxParser = require('./mboxParser');

// Create a test MBOX file
const testMboxContent = `From user1@example.com Mon Jan 01 10:00:00 2024
From: user1@example.com
To: user2@example.com
Subject: Test Email 1
Date: Mon, 01 Jan 2024 10:00:00 +0000

This is the body of the first test email.
It has multiple lines.

From user2@example.com Tue Jan 02 15:30:00 2024
From: user2@example.com
To: user1@example.com
Subject: Re: Test Email 1
Date: Tue, 02 Jan 2024 15:30:00 +0000

This is a reply to the first email.
`;

async function testParser() {
  const testDir = '/tmp/mbox-test';
  const testFile = path.join(testDir, 'test.mbox');
  
  // Create test directory
  if (!fs.existsSync(testDir)) {
    fs.mkdirSync(testDir, { recursive: true });
  }
  
  // Write test MBOX file
  fs.writeFileSync(testFile, testMboxContent);
  
  // Test the parser
  const parser = new MboxParser();
  try {
    const emails = await parser.parseMboxFile(testFile);
    
    console.log('MBOX Parser Test Results:');
    console.log(`- Parsed ${emails.length} emails`);
    
    emails.forEach((email, index) => {
      console.log(`\nEmail ${index + 1}:`);
      console.log(`  From: ${email.from}`);
      console.log(`  To: ${email.to}`);
      console.log(`  Subject: ${email.subject}`);
      console.log(`  Date: ${email.date}`);
      console.log(`  Body length: ${email.body.length} characters`);
    });
    
    // Cleanup
    fs.unlinkSync(testFile);
    fs.rmdirSync(testDir);
    
    console.log('\n✅ MBOX Parser test completed successfully!');
    return true;
  } catch (error) {
    console.error('❌ MBOX Parser test failed:', error.message);
    return false;
  }
}

// Run the test
testParser().then(success => {
  process.exit(success ? 0 : 1);
});