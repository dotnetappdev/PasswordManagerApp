// Syncs the repository's top-level screenshots/ into docs-site/public/screenshots/ so the site always
// serves whatever the Capture Screenshots workflow (or a local Playwright run) most recently produced,
// without checking a duplicate copy into git. Run automatically before dev/build.
import { cpSync, rmSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const here = path.dirname(fileURLToPath(import.meta.url));
const src = path.join(here, '..', 'screenshots');
const dest = path.join(here, 'public', 'screenshots');

if (!existsSync(src)) {
  console.error(`Source not found: ${src}`);
  process.exit(1);
}

rmSync(dest, { recursive: true, force: true });
cpSync(src, dest, {
  recursive: true,
  filter: (p) => !p.endsWith('.md') && !p.endsWith('.gitkeep') && !p.endsWith('.placeholder'),
});

console.log(`Synced ${src} -> ${dest}`);
