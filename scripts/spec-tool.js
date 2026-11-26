#!/usr/bin/env node

/**
 * Universal AI Spec Tool
 * Helper CLI for managing Spec-Driven Development workflow.
 * Multi-platform: Works on Windows PowerShell, Linux, and macOS.
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '..');

const TEMPLATES_DIR = path.join(ROOT_DIR, '.spec', 'templates');
const SPECS_DIR = path.join(ROOT_DIR, 'docs', 'specs');

function showHelp() {
  console.log(`
Universal AI Spec Tool
Usage:
  node scripts/spec-tool.js new <feature-name>   Create a new spec/plan/tasks workspace
  node scripts/spec-tool.js list                 List all existing specs and task progress
  node scripts/spec-tool.js check <feature-name> Check task completion for a spec
`);
}

function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

function createNewSpec(featureName) {
  if (!featureName) {
    console.error('Error: Please provide a feature name (e.g. node scripts/spec-tool.js new user-auth)');
    process.exit(1);
  }

  const sanitized = featureName.toLowerCase().replace(/[^a-z0-9-_]/g, '-');
  const targetDir = path.join(SPECS_DIR, sanitized);

  if (fs.existsSync(targetDir)) {
    console.error(`Error: Spec directory already exists at: docs/specs/${sanitized}`);
    process.exit(1);
  }

  ensureDir(targetDir);

  const files = [
    { src: 'spec-template.md', dest: 'spec.md' },
    { src: 'plan-template.md', dest: 'plan.md' },
    { src: 'tasks-template.md', dest: 'tasks.md' },
  ];

  for (const { src, dest } of files) {
    const srcPath = path.join(TEMPLATES_DIR, src);
    const destPath = path.join(targetDir, dest);
    let content = fs.readFileSync(srcPath, 'utf8');
    content = content.replace(/\[TÊN TÍNH NĂNG \/ FEATURE NAME\]/g, sanitized);
    content = content.replace(/\[feature-name\]/g, sanitized);
    fs.writeFileSync(destPath, content, 'utf8');
  }

  console.log(`\n[SUCCESS] Created new Spec workspace for: ${sanitized}`);
  console.log(`Location: docs/specs/${sanitized}/`);
  console.log(`Files generated:`);
  console.log(`  - docs/specs/${sanitized}/spec.md`);
  console.log(`  - docs/specs/${sanitized}/plan.md`);
  console.log(`  - docs/specs/${sanitized}/tasks.md`);
  console.log(`\nNext steps:`);
  console.log(`  1. Define requirements and BDD scenarios in spec.md`);
  console.log(`  2. Review plan.md and execute tasks in tasks.md sequentially`);
}

function listSpecs() {
  if (!fs.existsSync(SPECS_DIR)) {
    console.log('No specs found. Create one with: npm run spec:new <name>');
    return;
  }

  const entries = fs.readdirSync(SPECS_DIR, { withFileTypes: true });
  const specs = entries.filter((e) => e.isDirectory()).map((e) => e.name);

  if (specs.length === 0) {
    console.log('No specs found in docs/specs/.');
    return;
  }

  console.log('\nExisting Specs in docs/specs/:');
  for (const spec of specs) {
    const tasksFile = path.join(SPECS_DIR, spec, 'tasks.md');
    let progress = 'No tasks file';
    if (fs.existsSync(tasksFile)) {
      const content = fs.readFileSync(tasksFile, 'utf8');
      const totalTasks = (content.match(/- \[[ xX]\]/g) || []).length;
      const completedTasks = (content.match(/- \[[xX]\]/g) || []).length;
      progress = totalTasks > 0 ? `${completedTasks}/${totalTasks} tasks completed (${Math.round((completedTasks / totalTasks) * 100)}%)` : '0 tasks';
    }
    console.log(`  - [${spec}]: ${progress}`);
  }
}

function checkSpec(featureName) {
  if (!featureName) {
    console.error('Error: Please provide a feature name.');
    process.exit(1);
  }

  const tasksFile = path.join(SPECS_DIR, featureName, 'tasks.md');
  if (!fs.existsSync(tasksFile)) {
    console.error(`Error: tasks.md not found for "${featureName}".`);
    process.exit(1);
  }

  const content = fs.readFileSync(tasksFile, 'utf8');
  const totalTasks = (content.match(/- \[[ xX]\]/g) || []).length;
  const completedTasks = (content.match(/- \[[xX]\]/g) || []).length;

  console.log(`\nTask Status for "${featureName}": ${completedTasks}/${totalTasks} done.`);
  if (completedTasks === totalTasks && totalTasks > 0) {
    console.log('[OK] All atomic tasks are completed!');
  } else {
    console.log(`[!] Still ${totalTasks - completedTasks} task(s) remaining.`);
  }
}

const [cmd, arg] = process.argv.slice(2);

switch (cmd) {
  case 'new':
    createNewSpec(arg);
    break;
  case 'list':
    listSpecs();
    break;
  case 'check':
    checkSpec(arg);
    break;
  default:
    showHelp();
    break;
}
