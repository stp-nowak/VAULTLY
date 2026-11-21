import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

/**
 * Feature-Sliced Design layers, highest first. A layer may import only from layers below it.
 * `domain` replaces the standard FSD `entities` layer to mirror the backend DDD bounded contexts.
 */
const fsdLayers = ['app', 'pages', 'widgets', 'features', 'domain', 'shared']

/** Layers split into slices; other layers must use a slice's public API (`index.ts`) only. */
const slicedLayers = ['pages', 'widgets', 'features', 'domain']

const deepSliceImports = slicedLayers.map((layer) => ({
  group: [`@/${layer}/*/**`],
  message: `Import from the slice public API (@/${layer}/<slice>), not from its internals.`,
}))

/** Builds the import restrictions for files living in the given layer. */
function fsdRestrictionsFor(layer) {
  const index = fsdLayers.indexOf(layer)
  const higherLayers = fsdLayers.slice(0, index).map((higher) => ({
    group: [`@/${higher}`, `@/${higher}/**`],
    message: `FSD: the "${layer}" layer cannot import from the higher "${higher}" layer.`,
  }))
  // Slices on the same layer must not know about each other; inside a slice use relative imports.
  const sameLayer = slicedLayers.includes(layer)
    ? [{
        group: [`@/${layer}/**`],
        message: `FSD: slices on the "${layer}" layer cannot import each other. Move shared code to a lower layer.`,
      }]
    : []

  return {
    files: [`src/${layer}/**/*.{ts,tsx}`],
    rules: {
      'no-restricted-imports': ['error', { patterns: [...higherLayers, ...sameLayer, ...deepSliceImports] }],
    },
  }
}

export default defineConfig([
  globalIgnores(['dist', 'coverage', 'src/app/routeTree.gen.ts']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
      parserOptions: {
        project: ['./tsconfig.app.json', './tsconfig.node.json'],
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },
  ...fsdLayers.map(fsdRestrictionsFor),
])
