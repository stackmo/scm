#!/usr/bin/env node

/**
 * Hindsight CLI & Helper Client for ai-spec
 * Connects to local Hindsight container at http://localhost:8888
 */

import { fileURLToPath } from 'node:url';

const BASE_URL = process.env.HINDSIGHT_URL || 'http://localhost:8888';
const DEFAULT_BANK = process.env.HINDSIGHT_BANK || 'ai-spec';

export async function request(path, options = {}, baseUrl = process.env.HINDSIGHT_URL || BASE_URL) {
  const url = `${baseUrl}${path}`;
  const timeoutMs = options.timeoutMs || 120000;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const res = await fetch(url, {
      signal: controller.signal,
      headers: { 'Content-Type': 'application/json', ...options.headers },
      ...options
    });
    clearTimeout(timer);
    if (!res.ok) {
      const errText = await res.text();
      throw new Error(`HTTP ${res.status}: ${errText}`);
    }
    return res.json();
  } catch (err) {
    clearTimeout(timer);
    throw err;
  }
}

export async function checkHealth() {
  try {
    const data = await request('/health', { timeoutMs: 3000 });
    console.log('[Hindsight] Status:', data.status, '| DB:', data.database);
    return { ok: true, data };
  } catch (err) {
    console.error('[Hindsight] Cannot connect to local server:', err.message);
    return { ok: false, error: err.message };
  }
}

export async function retainMemory(content, context = 'general', bankId = DEFAULT_BANK) {
  try {
    console.log(`[Hindsight] Retaining into bank [${bankId}]...`);
    const data = await request(`/v1/default/banks/${bankId}/memories`, {
      method: 'POST',
      body: JSON.stringify({
        items: [
          {
            content,
            context,
            timestamp: new Date().toISOString()
          }
        ]
      })
    });
    console.log(`[Hindsight] Retained successfully! (Total units: ${data.total_units || 1})`);
    return { ok: true, data };
  } catch (err) {
    console.error('[Hindsight] Retain failed:', err.message);
    return { ok: false, error: err.message };
  }
}

export async function recallMemory(query, bankId = DEFAULT_BANK) {
  try {
    console.log(`[Hindsight] Recalling query "${query}" from bank [${bankId}]...`);
    const data = await request(`/v1/default/banks/${bankId}/memories/recall`, {
      method: 'POST',
      body: JSON.stringify({ query })
    });
    const results = data.results || [];
    if (results.length === 0) {
      console.log('[Hindsight] No relevant memories found.');
    } else {
      console.log(`[Hindsight] Found ${results.length} memories:`);
      results.forEach((r, idx) => {
        const sim = r.similarity !== undefined ? `${(r.similarity * 100).toFixed(1)}%` : 'N/A';
        console.log(`  ${idx + 1}. [${sim}] ${r.text || r.content}`);
      });
    }
    return { ok: true, results };
  } catch (err) {
    console.error('[Hindsight] Recall failed:', err.message);
    return { ok: false, error: err.message, results: [] };
  }
}

export async function reflectMemory(query, bankId = DEFAULT_BANK) {
  try {
    console.log(`[Hindsight] Reflecting on "${query}"...`);
    const data = await request(`/v1/default/banks/${bankId}/reflect`, {
      method: 'POST',
      body: JSON.stringify({ query })
    });
    console.log('\n--- Hindsight Reflection ---');
    console.log(data.answer || JSON.stringify(data, null, 2));
    console.log('----------------------------\n');
    return { ok: true, data };
  } catch (err) {
    console.error('[Hindsight] Reflect failed:', err.message);
    return { ok: false, error: err.message };
  }
}

export function parseCliArgs(argv) {
  const [cmd, ...rest] = argv;
  let context = 'general';
  const cleanArgs = [];

  for (const arg of rest) {
    if (arg.startsWith('--context=')) {
      context = arg.split('=')[1];
    } else {
      cleanArgs.push(arg);
    }
  }

  return { cmd: cmd || 'help', queryOrContent: cleanArgs.join(' '), context };
}

export async function runCli(argv) {
  const { cmd, queryOrContent, context } = parseCliArgs(argv);

  if (cmd === 'status' || cmd === 'health') {
    const res = await checkHealth();
    return res.ok ? 0 : 1;
  }
  if (cmd === 'retain') {
    if (!queryOrContent) {
      console.error('Usage: node scripts/hindsight.js retain "<content to remember>" [--context=arch|debug|spec]');
      return 1;
    }
    const res = await retainMemory(queryOrContent, context);
    return res.ok ? 0 : 1;
  }
  if (cmd === 'recall') {
    if (!queryOrContent) {
      console.error('Usage: node scripts/hindsight.js recall "<search query>"');
      return 1;
    }
    const res = await recallMemory(queryOrContent);
    return res.ok ? 0 : 1;
  }
  if (cmd === 'reflect') {
    if (!queryOrContent) {
      console.error('Usage: node scripts/hindsight.js reflect "<question to answer with memory>"');
      return 1;
    }
    const res = await reflectMemory(queryOrContent);
    return res.ok ? 0 : 1;
  }

  console.log(`
Hindsight Helper CLI for ai-spec
Usage:
  node scripts/hindsight.js status
  node scripts/hindsight.js retain "<text to store>" [--context=general|arch|spec]
  node scripts/hindsight.js recall "<query to search>"
  node scripts/hindsight.js reflect "<question to answer with memory>"
`);
  return 0;
}

// Auto-run if executed directly
if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  runCli(process.argv.slice(2)).then(code => {
    if (code !== 0) process.exit(code);
  });
}

