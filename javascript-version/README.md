# JavaScript Version - MBOX to PST Converter

This directory contains the JavaScript/Node.js implementation of the MBOX to PST converter with a React frontend.

## Structure

- `backend/` - Node.js/Express API server for MBOX parsing
- `frontend/` - React application for viewing emails and conversion UI

## Features

- Upload and parse MBOX files
- View email list with metadata (sender, subject, date)
- View full email content including headers and body
- Simulated PST conversion process with progress tracking
- Responsive web interface

## Prerequisites

- Node.js 16+ and npm
- Modern web browser

## Installation

### Backend Setup

```bash
cd javascript-version/backend
npm install
```

### Frontend Setup

```bash
cd javascript-version/frontend
npm install
```

## Running the Application

### Start the Backend API

```bash
cd javascript-version/backend
npm start
```

The backend API will run on http://localhost:3001

### Start the Frontend

```bash
cd javascript-version/frontend
npm start
```

The React app will run on http://localhost:3000

## Usage

1. Open http://localhost:3000 in your browser
2. Upload an MBOX file using the file input
3. Browse through the parsed emails in the list
4. Click on any email to view its full content
5. Use the conversion section to simulate PST conversion

## API Endpoints

- `POST /api/upload-mbox` - Upload and parse MBOX file
- `GET /api/email/:filename/:id` - Get full email content
- `POST /api/convert-to-pst` - Start PST conversion (simulated)
- `GET /api/conversion-progress/:id` - Get conversion progress
- `GET /api/health` - Health check

## Limitations

This JavaScript implementation provides:
- ✅ MBOX file parsing
- ✅ Email viewing and browsing
- ✅ Conversion process simulation
- ❌ Actual PST file creation (requires specialized libraries)

For actual PST file creation, use the C# version which utilizes the Aspose.Email library.

## Development

### Backend Development

```bash
cd javascript-version/backend
npm run dev  # Uses nodemon for auto-restart
```

### Frontend Development

```bash
cd javascript-version/frontend
npm start  # React development server with hot reload
```

## Testing

```bash
# Backend tests
cd javascript-version/backend
npm test

# Frontend tests
cd javascript-version/frontend
npm test
```