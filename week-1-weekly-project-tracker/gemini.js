async function generateSummary(fileContents) {
  const res = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${process.env.GEMINI_API_KEY}`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contents: [{ parts: [{ text: `Summarize what this code does in 2 sentences:\n\n${fileContents}` }] }]
      })
    }
  );
  const data = await res.json();
  return data.candidates[0].content.parts[0].text;
}

module.exports = { generateSummary };