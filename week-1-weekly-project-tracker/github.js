const GITHUB_API = 'https://api.github.com';
const REPO_NAME = 'weekly-project';

function authHeaders() {
  return {
    Authorization: `Bearer ${process.env.GITHUB_TOKEN}`,
    Accept: 'application/vnd.github+json',
    'X-GitHub-Api-Version': '2022-11-28'
  };
}

async function ensureRepoExists() {
  const check = await fetch(`${GITHUB_API}/repos/${process.env.GITHUB_USERNAME}/${REPO_NAME}`, {
    headers: authHeaders()
  });
  if (check.status === 200) return;

  await fetch(`${GITHUB_API}/user/repos`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: REPO_NAME,
      description: 'Weekly AI automation builds',
      private: false,
      auto_init: true
    })
  });
}

async function pushFile(repoPath, contentBuffer, commitMessage) {
  const url = `${GITHUB_API}/repos/${process.env.GITHUB_USERNAME}/${REPO_NAME}/contents/${repoPath}`;

  // check if the file already exists, to get its sha (required for updates)
  let sha;
  const existing = await fetch(url, { headers: authHeaders() });
  if (existing.status === 200) {
    sha = (await existing.json()).sha;
  }

  const res = await fetch(url, {
    method: 'PUT',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({
      message: commitMessage,
      content: contentBuffer.toString('base64'),
      ...(sha ? { sha } : {})
    })
  });

  if (!res.ok) throw new Error(`GitHub push failed: ${res.status} ${await res.text()}`);
  return res.json();
}

module.exports = { ensureRepoExists, pushFile };