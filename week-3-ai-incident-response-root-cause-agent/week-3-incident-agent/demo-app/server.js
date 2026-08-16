require('dotenv').config();
const express = require('express');
const { pool } = require('./db');

const app = express();
app.use(express.json());
app.use(express.static('public'));

// toggle this to simulate a slow/broken DB pool — this is your "incident switch"
let SIMULATE_SLOW_DB = false;

async function logRequest(endpoint, responseTimeMs, level = 'info', message = '') {
  await pool.query(
    'INSERT INTO logs (level, endpoint, message, response_time_ms) VALUES ($1,$2,$3,$4)',
    [level, endpoint, message, responseTimeMs]
  );
}

// simulated business endpoint — this is what "breaks"
app.get('/api/orders', async (req, res) => {
  const start = Date.now();

  if (SIMULATE_SLOW_DB) {
    await new Promise(r => setTimeout(r, 3000)); // artificial slowdown
    await pool.query(
      "INSERT INTO db_events (event_type, detail, duration_ms) VALUES ('pool_exhausted', 'Connection pool exhausted under load', $1)",
      [3000]
    );
  }

  const responseTime = Date.now() - start;
  await logRequest('/api/orders', responseTime, responseTime > 1000 ? 'error' : 'info');

  // fire an alert if response time crosses a threshold
  if (responseTime > 1000) {
    await pool.query(
      `INSERT INTO alerts (metric, threshold_value, actual_value, message)
       VALUES ('response_time', 300, $1, $2)`,
      [responseTime, `Response time increased to ${responseTime}ms, threshold is 300ms`]
    );
  }

  res.json({ orders: [], responseTimeMs: responseTime });
});

// simulate a deployment happening
app.post('/api/simulate/deploy', async (req, res) => {
  const sha = Math.random().toString(16).slice(2, 9);
  const { breakIt } = req.body; // { breakIt: true } to trigger the incident

  await pool.query(
    'INSERT INTO deployments (commit_sha, description) VALUES ($1,$2)',
    [sha, breakIt ? 'Simulated bad deploy (reduced DB pool size)' : 'Simulated normal deploy']
  );

  if (breakIt) SIMULATE_SLOW_DB = true;

  res.json({ deployed: sha, breakIt: !!breakIt });
});

// manual reset, so you can re-run the test scenario
app.post('/api/simulate/reset', (req, res) => {
  SIMULATE_SLOW_DB = false;
  res.json({ reset: true });
});

app.get('/api/recent', async (req, res) => {
  const logs = await pool.query('SELECT * FROM logs ORDER BY created_at DESC LIMIT 10');
  const deployments = await pool.query('SELECT * FROM deployments ORDER BY deployed_at DESC LIMIT 5');
  const dbEvents = await pool.query('SELECT * FROM db_events ORDER BY created_at DESC LIMIT 10');
  const alerts = await pool.query('SELECT * FROM alerts ORDER BY fired_at DESC LIMIT 5');
  res.json({
    logs: logs.rows,
    deployments: deployments.rows,
    dbEvents: dbEvents.rows,
    alerts: alerts.rows
  });
});

const PORT = 4000;
app.listen(PORT, () => console.log(`Demo app running at http://localhost:${PORT}`));