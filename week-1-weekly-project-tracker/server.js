require('dotenv').config();
const express = require('express');
const multer = require('multer');
const fs = require('fs');
const path = require('path');
const { readEntries, writeEntries } = require('./db');
const { ensureRepoExists, pushFile } = require('./github');

const app = express();
app.use(express.json());
app.use(express.static('public'));

const upload = multer({ dest: 'uploads/' });

// ---------- basic CRUD ----------

app.get('/api/entries', (req, res) => {
  res.json(readEntries());
});

app.post('/api/entries', (req, res) => {
  const entries = readEntries();
  const entry = {
    id: Date.now().toString(36),
    ...req.body,
    createdAt: new Date().toISOString(),
    pushedToGithub: false
  };
  entries.push(entry);
  writeEntries(entries);
  res.json(entry);
});

app.patch('/api/entries/:id', (req, res) => {
  const entries = readEntries();
  const idx = entries.findIndex(e => e.id === req.params.id);
  if (idx === -1) return res.status(404).json({ error: 'not found' });
  entries[idx] = { ...entries[idx], ...req.body };
  writeEntries(entries);
  res.json(entries[idx]);
});

app.delete('/api/entries/:id', (req, res) => {
  writeEntries(readEntries().filter(e => e.id !== req.params.id));
  res.json({ ok: true });
});

// ---------- file upload (folder-aware) ----------

app.post('/api/entries/:id/upload', upload.array('files'), (req, res) => {
  const entryDir = path.join(__dirname, 'uploads', req.params.id);

  req.files.forEach(f => {
    // f.originalname carries the relative path (e.g. "src/index.js")
    // because the frontend appends each file with its webkitRelativePath as the filename
    const relPath = f.originalname;
    const dest = path.join(entryDir, relPath);

    fs.mkdirSync(path.dirname(dest), { recursive: true }); // recreate subfolders
    fs.renameSync(f.path, dest);
  });

  res.json({ uploaded: req.files.map(f => f.originalname) });
});

// ---------- recursive folder walk, used by push ----------

function walkDir(dir, baseDir = dir) {
  let results = [];
  const items = fs.readdirSync(dir, { withFileTypes: true });
  for (const item of items) {
    const fullPath = path.join(dir, item.name);
    if (item.isDirectory()) {
      results = results.concat(walkDir(fullPath, baseDir));
    } else {
      const relPath = path.relative(baseDir, fullPath);
      results.push({ fullPath, relPath });
    }
  }
  return results;
}

// ---------- push to GitHub (preserves folder structure) ----------

app.post('/api/entries/:id/push', async (req, res) => {
  try {
    const entries = readEntries();
    const entry = entries.find(e => e.id === req.params.id);
    if (!entry) return res.status(404).json({ error: 'not found' });

    await ensureRepoExists();

    const entryDir = path.join(__dirname, 'uploads', req.params.id);
    const slug = entry.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
    const folder = `${(entry.week || 'week').toLowerCase().replace(/\s+/g, '-')}-${slug}`;

    const files = fs.existsSync(entryDir) ? walkDir(entryDir) : [];

    for (const { fullPath, relPath } of files) {
      const content = fs.readFileSync(fullPath);
      const githubPath = `${folder}/${relPath.split(path.sep).join('/')}`; // normalize Windows backslashes
      await pushFile(githubPath, content, `${entry.week}: ${entry.title}`);
    }

    entry.pushedToGithub = true;
    writeEntries(entries);
    res.json({ ok: true, filesPushed: files.length, folder });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Tracker running at http://localhost:${PORT}`));