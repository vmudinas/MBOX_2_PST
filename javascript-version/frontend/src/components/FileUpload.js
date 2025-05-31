import React, { useState } from 'react';
import axios from 'axios';

const API_BASE_URL = 'http://localhost:3001/api';

function FileUpload({ onFileUploaded, onError, loading, setLoading }) {
  const [selectedFile, setSelectedFile] = useState(null);

  const handleFileChange = (event) => {
    setSelectedFile(event.target.files[0]);
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      onError('Please select a file first');
      return;
    }

    const formData = new FormData();
    formData.append('mboxFile', selectedFile);

    setLoading(true);
    try {
      const response = await axios.post(`${API_BASE_URL}/upload-mbox`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      onFileUploaded(response.data);
    } catch (error) {
      console.error('Upload error:', error);
      onError(error.response?.data?.error || 'Failed to upload file');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="file-upload">
      <h2>Upload MBOX File</h2>
      <p>Select an MBOX file to view its emails and convert to PST format</p>
      
      <input
        type="file"
        accept=".mbox,.mbx"
        onChange={handleFileChange}
        className="file-input"
        disabled={loading}
      />
      
      <button 
        onClick={handleUpload}
        disabled={!selectedFile || loading}
        className="upload-btn"
      >
        {loading ? 'Processing...' : 'Upload and Parse MBOX'}
      </button>

      {selectedFile && (
        <p>Selected file: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)</p>
      )}
    </div>
  );
}

export default FileUpload;