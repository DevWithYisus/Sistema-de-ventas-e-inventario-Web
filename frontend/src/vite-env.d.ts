/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_CODIGO_NEGOCIO?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
