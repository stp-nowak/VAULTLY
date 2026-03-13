/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly AUTH_CALLBACK_PATH?: string;
  readonly IDENTITY_BASE_URL?: string;
  readonly IDENTITY_CLIENT_ID?: string;
  readonly IDENTITY_SCOPES?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
