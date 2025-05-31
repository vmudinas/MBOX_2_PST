import React from 'react';

function EmailList({ emails, selectedEmail, onEmailSelected }) {
  const formatDate = (dateString) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    } catch {
      return dateString;
    }
  };

  return (
    <div className="email-list">
      <h3>Emails ({emails.length})</h3>
      {emails.map((email) => (
        <div
          key={email.id}
          className={`email-item ${selectedEmail?.id === email.id ? 'selected' : ''}`}
          onClick={() => onEmailSelected(email)}
        >
          <div className="email-header">
            <div className="email-subject">{email.subject || '(No Subject)'}</div>
            <div className="email-date">{formatDate(email.date)}</div>
          </div>
          <div className="email-from">From: {email.from}</div>
          {email.to && <div className="email-from">To: {email.to}</div>}
          <div className="email-preview">{email.preview}</div>
        </div>
      ))}
    </div>
  );
}

export default EmailList;