import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const madgeConfig = {
  // Point madge at the package tsconfig so module resolution matches the compiler; without it edges that
  // TypeScript can resolve (and any future path aliases) are silently dropped and their cycles go undetected.
  tsConfig: path.resolve(__dirname, '../../tsconfig.json'),
  detectiveOptions: {
    ts: {
      skipTypeImports: true,
    },
    tsx: {
      skipTypeImports: true,
    },
  },
};

export { madgeConfig };
