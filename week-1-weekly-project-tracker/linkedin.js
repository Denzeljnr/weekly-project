async function generateLinkedInPost(currentEntry, recentEntries) {
  const history = recentEntries
    .map(e => `${e.week}: ${e.title} (${(e.stack || []).join(', ')})`)
    .join('\n');

  const prompt = `You are helping write a LinkedIn post for a weekly build-in-public series.

This week's project:
Title: ${currentEntry.title}
Stack: ${(currentEntry.stack || []).join(', ')}
What it does: ${currentEntry.desc}
What was learned: ${currentEntry.learn}

Recent weeks for context (don't repeat these in detail, just reference the series):
${history}

Write a LinkedIn post, casual but specific, 3-5 short paragraphs. Mention the stack naturally, reference this being part of an ongoing weekly series, and end with a short line inviting people to follow along. No hashtag spam — 2-3 relevant hashtags max at the end.`;

  const res = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${process.env.GEMINI_API_KEY}`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contents: [{ parts: [{ text: prompt }] }] })
    }
  );
  const data = await res.json();
  return data.candidates[0].content.parts[0].text;
}

module.exports = { generateLinkedInPost };