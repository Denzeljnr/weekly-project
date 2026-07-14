const fs = require('fs');
const path = require('path');
const DB_PATH = path.join(__dirname, 'data.json');

function readEntries() {
  if (!fs.existsSync(DB_PATH)) return [];
  return JSON.parse(fs.readFileSync(DB_PATH, 'utf-8'));
}

function writeEntries(entries) {
  fs.writeFileSync(DB_PATH, JSON.stringify(entries, null, 2));
}

module.exports = { readEntries, writeEntries };