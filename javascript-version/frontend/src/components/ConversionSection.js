import React, { useState } from 'react';
import axios from 'axios';

const API_BASE_URL = 'http://localhost:3001/api';

function ConversionSection({ filename, emailCount }) {
  const [converting, setConverting] = useState(false);
  const [progress, setProgress] = useState(0);
  const [conversionId, setConversionId] = useState(null);
  const [conversionStatus, setConversionStatus] = useState('');
  const [conversionMessage, setConversionMessage] = useState('');
  
  // EML export state
  const [exporting, setExporting] = useState(false);
  const [exportResult, setExportResult] = useState(null);

  const startConversion = async () => {
    setConverting(true);
    setProgress(0);
    setConversionStatus('starting');
    setConversionMessage('Initializing conversion...');

    try {
      const response = await axios.post(`${API_BASE_URL}/convert-to-pst`, {
        filename,
        emailCount
      });

      setConversionId(response.data.conversionId);
      setConversionMessage(response.data.message);

      // Start polling for progress
      pollProgress(response.data.conversionId);
    } catch (error) {
      console.error('Conversion error:', error);
      setConversionMessage('Failed to start conversion');
      setConverting(false);
    }
  };

  const startEmlExport = async () => {
    setExporting(true);
    setExportResult(null);

    try {
      const response = await axios.post(`${API_BASE_URL}/export-to-eml`, {
        filename
      });

      setExportResult(response.data);
    } catch (error) {
      console.error('EML export error:', error);
      alert('Failed to export to EML format: ' + error.message);
    }
    
    setExporting(false);
  };

  const downloadExport = () => {
    if (exportResult && exportResult.downloadUrl) {
      window.open(`${API_BASE_URL.replace('/api', '')}${exportResult.downloadUrl}`, '_blank');
    }
  };

  const pollProgress = async (id) => {
    try {
      const response = await axios.get(`${API_BASE_URL}/conversion-progress/${id}`);
      const { progress: newProgress, status, message } = response.data;

      setProgress(newProgress);
      setConversionStatus(status);
      setConversionMessage(message);

      if (status === 'completed') {
        setConverting(false);
        // In a real implementation, this would provide a download link
        setTimeout(() => {
          setConversionMessage(message + ' (Note: This is a demonstration - actual PST file creation requires specialized libraries)');
        }, 1000);
      } else if (status === 'converting') {
        // Continue polling
        setTimeout(() => pollProgress(id), 1000);
      } else {
        setConverting(false);
      }
    } catch (error) {
      console.error('Progress polling error:', error);
      setConversionMessage('Error checking conversion progress');
      setConverting(false);
    }
  };

  const resetConversion = () => {
    setConverting(false);
    setProgress(0);
    setConversionId(null);
    setConversionStatus('');
    setConversionMessage('');
  };

  const resetExport = () => {
    setExportResult(null);
  };

  return (
    <div className="conversion-section">
      <h3>Export Options</h3>
      <p>Export your MBOX file containing {emailCount} emails</p>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '20px' }}>
        {/* EML Export Section */}
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: '#f8f9fa' }}>
          <h4 style={{ marginTop: 0, color: '#28a745' }}>Export to EML Files</h4>
          <p style={{ fontSize: '14px', color: '#666' }}>
            Convert to individual EML files that can be imported into most email clients
          </p>
          
          {!exporting && !exportResult && (
            <button onClick={startEmlExport} className="convert-btn" style={{ backgroundColor: '#28a745' }}>
              Export to EML
            </button>
          )}

          {exporting && (
            <div>
              <p>Exporting emails to EML format...</p>
              <div className="progress-bar">
                <div className="progress-fill-spinner"></div>
              </div>
            </div>
          )}

          {exportResult && (
            <div>
              <div className="success" style={{ backgroundColor: '#d4edda', color: '#155724' }}>
                ✅ Export completed! {exportResult.emailCount} emails exported
              </div>
              <button onClick={downloadExport} className="convert-btn" style={{ backgroundColor: '#007bff', marginRight: '10px' }}>
                Download ZIP ({Math.round(exportResult.zipSize / 1024)} KB)
              </button>
              <button onClick={resetExport} className="convert-btn" style={{ backgroundColor: '#6c757d' }}>
                Export Another
              </button>
            </div>
          )}
        </div>

        {/* PST Conversion Section */}
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: '#fff3cd' }}>
          <h4 style={{ marginTop: 0, color: '#856404' }}>Convert to PST (Demo)</h4>
          <p style={{ fontSize: '14px', color: '#666' }}>
            Simulated PST conversion process (requires C# version for actual PST files)
          </p>

          {!converting && progress === 0 && (
            <button onClick={startConversion} className="convert-btn" style={{ backgroundColor: '#ffc107', color: '#212529' }}>
              Simulate PST Conversion
            </button>
          )}

          {converting && (
            <div>
              <p>{conversionMessage}</p>
              <div className="progress-bar">
                <div 
                  className="progress-fill" 
                  style={{ width: `${progress}%` }}
                ></div>
              </div>
              <p>{progress}% complete</p>
            </div>
          )}

          {progress === 100 && conversionStatus === 'completed' && (
            <div>
              <div className="success">
                {conversionMessage}
              </div>
              <button onClick={resetConversion} className="convert-btn" style={{ backgroundColor: '#ffc107', color: '#212529' }}>
                Convert Another File
              </button>
            </div>
          )}
        </div>
      </div>

      <div style={{ 
        marginTop: '20px', 
        padding: '15px', 
        backgroundColor: '#fff3cd', 
        border: '1px solid #ffeaa7',
        borderRadius: '5px',
        fontSize: '14px'
      }}>
        <strong>Note:</strong> This JavaScript implementation demonstrates the MBOX parsing and UI capabilities. 
        For actual PST file creation, the C# version with Aspose.Email library should be used, as PST is a 
        proprietary Microsoft format that requires specialized libraries not readily available in JavaScript.
      </div>
    </div>
  );
}

export default ConversionSection;