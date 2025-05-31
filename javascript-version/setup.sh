#!/bin/bash

# MBOX to PST Converter - JavaScript Version Setup Script

echo "🚀 Setting up MBOX to PST Converter JavaScript Version..."
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 16+ and npm first."
    echo "   Visit: https://nodejs.org/"
    exit 1
fi

echo "✅ Node.js version: $(node --version)"
echo "✅ npm version: $(npm --version)"
echo ""

# Check if we're in the right directory
if [ ! -d "javascript-version" ]; then
    echo "❌ Please run this script from the repository root directory"
    exit 1
fi

# Install backend dependencies
echo "📦 Installing backend dependencies..."
cd javascript-version/backend
npm install
if [ $? -ne 0 ]; then
    echo "❌ Failed to install backend dependencies"
    exit 1
fi
cd ../..

# Install frontend dependencies
echo "📦 Installing frontend dependencies..."
cd javascript-version/frontend
npm install
if [ $? -ne 0 ]; then
    echo "❌ Failed to install frontend dependencies"
    exit 1
fi
cd ../..

echo ""
echo "✅ Setup completed successfully!"
echo ""
echo "🔧 To start the application:"
echo ""
echo "1. Start the backend API:"
echo "   cd javascript-version/backend"
echo "   npm start"
echo ""
echo "2. In another terminal, start the frontend:"
echo "   cd javascript-version/frontend"
echo "   npm start"
echo ""
echo "3. Open your browser to: http://localhost:3000"
echo ""
echo "📖 For more information, see javascript-version/README.md"