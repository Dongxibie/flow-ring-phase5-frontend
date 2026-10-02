module.exports = {
  root: true,
  env: { browser: true, es2022: true },
  parser: '@typescript-eslint/parser',
  parserOptions: { ecmaVersion: 2022, sourceType: 'module' },
  plugins: ['@typescript-eslint', 'react-hooks', 'react-refresh'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
  ],
  rules: {
    'react-hooks/rules-of-hooks': 'error',
    'react-hooks/exhaustive-deps': 'error',
    'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    'no-restricted-imports': [
      'error',
      {
        paths: [
          { name: 'fs', message: 'UI 禁止直访 fs，必须走 useBridge()。' },
          { name: 'path', message: 'UI 禁止直访 path，必须走 useBridge()。' },
          { name: 'child_process', message: 'UI 禁止直访 child_process。' },
        ],
      },
    ],
  },
  ignorePatterns: ['dist/', 'node_modules/'],
};