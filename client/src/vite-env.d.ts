/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_DEMO_MODE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare module '*.jsx' {
  import type { ComponentType } from 'react'
  const component: ComponentType
  export default component
}

declare module '*.json' {
  const value: unknown
  export default value
}
