import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const madgeConfig = {
  // Without this madge cannot resolve the `@/*` path alias (defined in tsconfig.app.json, not in the
  // references-only tsconfig.json) and silently drops those edges, so cycles running through an aliased
  // import go undetected.
  tsConfig: path.resolve(__dirname, '../../../tsconfig.app.json'),
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
