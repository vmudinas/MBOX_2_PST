import React, { useState, useEffect } from 'react';
import axios from 'axios';

const API_BASE_URL = 'http://localhost:3001/api';

function EmailViewer({ email, filename }) {
  const [fullEmail, setFullEmail] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (email && filename) {
      loadFullEmail();
    }
  }, [email, filename]);

  const loadFullEmail = async () => {
    setLoading(true);
    setError('');
    
    try {
      const response = await axios.get(`${API_BASE_URL}/email/${filename}/${email.id}`);
      setFullEmail(response.data);
    } catch (error) {
      console.error('Error loading email:', error);
      setError('Failed to load email content');
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  };

  if (loading) {
    return (
      <div className="email-viewer">
        <div className="loading">Loading email content...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="email-viewer">
        <div className="error">{error}</div>
      </div>
    );
  }

  if (!fullEmail) {
    return (
      <div className="email-viewer">
        <div className="loading">Select an email to view its content</div>
      </div>
    );
  }

  return (
    <div className="email-viewer">
      <h3>Email Content</h3>
      
      <div style={{ marginBottom: '20px' }}>
        <p><strong>Subject:</strong> {fullEmail.subject || '(No Subject)'}</p>
        <p><strong>From:</strong> {fullEmail.from}</p>
        {fullEmail.to && <p><strong>To:</strong> {fullEmail.to}</p>}
        <p><strong>Date:</strong> {formatDate(fullEmail.date)}</p>
        
        {Object.keys(fullEmail.headers).length > 0 && (
          <details style={{ marginTop: '10px' }}>
            <summary>View Headers</summary>
            <div style={{ 
              background: '#f8f9fa', 
              padding: '10px', 
              margin: '10px 0',
              borderRadius: '5px',
              fontSize: '12px',
              fontFamily: 'monospace'
            }}>
              {Object.entries(fullEmail.headers).map(([key, value]) => (
                <div key={key}><strong>{key}:</strong> {value}</div>
              ))}
            </div>
          </details>
        )}
      </div>

      <div>
        <strong>Body:</strong>
        <div className="email-content">
          {fullEmail.body || '(No content)'}
        </div>
      </div>
    </div>
  );
}

export default EmailViewer;