import React, { useState } from 'react';
import FileUpload from './components/FileUpload';
import EmailList from './components/EmailList';
import EmailViewer from './components/EmailViewer';
import ConversionSection from './components/ConversionSection';
import './App.css';

function App() {
  const [emails, setEmails] = useState([]);
  const [selectedEmail, setSelectedEmail] = useState(null);
  const [emailCount, setEmailCount] = useState(0);
  const [filename, setFilename] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleFileUploaded = (data) => {
    setEmails(data.emails);
    setEmailCount(data.emailCount);
    setFilename(data.filename);
    setSelectedEmail(null);
    setError('');
  };

  const handleEmailSelected = (email) => {
    setSelectedEmail(email);
  };

  const handleError = (errorMessage) => {
    setError(errorMessage);
  };

  return (
    <div className="App">
      <header className="header">
        <div className="container">
          <h1>MBOX to PST Converter</h1>
          <p>Upload, view, and convert your MBOX files to PST format</p>
        </div>
      </header>

      <div className="container">
        {error && (
          <div className="error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <FileUpload 
          onFileUploaded={handleFileUploaded}
          onError={handleError}
          loading={loading}
          setLoading={setLoading}
        />

        {emailCount > 0 && (
          <div className="stats">
            <div className="stat-item">
              <div className="stat-number">{emailCount}</div>
              <div className="stat-label">Total Emails</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">{filename}</div>
              <div className="stat-label">File Name</div>
            </div>
          </div>
        )}

        {emails.length > 0 && (
          <>
            <div style={{ display: 'flex', gap: '20px' }}>
              <div style={{ flex: '1' }}>
                <EmailList 
                  emails={emails}
                  selectedEmail={selectedEmail}
                  onEmailSelected={handleEmailSelected}
                />
              </div>
              
              {selectedEmail && (
                <div style={{ flex: '1' }}>
                  <EmailViewer 
                    email={selectedEmail}
                    filename={filename}
                  />
                </div>
              )}
            </div>

            <ConversionSection 
              filename={filename}
              emailCount={emailCount}
            />
          </>
        )}
      </div>
    </div>
  );
}

export default App;