import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'node:url'
import { resolve } from 'node:path'
import { artifactPlugin } from './server/artifacts.ts'

const repository = fileURLToPath(new URL('../../../', import.meta.url))
const extraRoots: string[] = process.env.PARITY_REPORT_ROOTS
  ? JSON.parse(process.env.PARITY_REPORT_ROOTS)
  : []
if (!Array.isArray(extraRoots) || extraRoots.some((r) => typeof r !== 'string'))
  throw new Error('PARITY_REPORT_ROOTS must be a JSON array of directories')
export default defineConfig({
  plugins: [
    vue(),
    artifactPlugin([
      resolve(repository, 'artifacts/visual-parity/runs'),
      ...extraRoots.map((r) => resolve(repository, r)),
    ]),
  ],
  server: { host: 'localhost', port: 5173, strictPort: true },
  preview: { host: 'localhost', port: 5173, strictPort: true },
})
