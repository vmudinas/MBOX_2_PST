import React, { useState } from 'react';
import axios from 'axios';

const API_BASE_URL = 'http://localhost:3001/api';

function ConversionSection({ filename, emailCount }) {
  const [converting, setConverting] = useState(false);
  const [progress, setProgress] = useState(0);
  const [conversionId, setConversionId] = useState(null);
  const [conversionStatus, setConversionStatus] = useState('');
  const [conversionMessage, setConversionMessage] = useState('');

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

  return (
    <div className="conversion-section">
      <h3>Convert to PST</h3>
      <p>Convert your MBOX file containing {emailCount} emails to PST format</p>

      {!converting && progress === 0 && (
        <button onClick={startConversion} className="convert-btn">
          Start PST Conversion
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
          <button onClick={resetConversion} className="convert-btn">
            Convert Another File
          </button>
        </div>
      )}

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