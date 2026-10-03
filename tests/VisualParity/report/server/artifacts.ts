import { createHash } from 'node:crypto'
import { readFile, readdir, realpath, stat } from 'node:fs/promises'
import { resolve, relative, isAbsolute, sep } from 'node:path'
import { parse } from 'yaml'
import { stringify } from 'yaml'
import type { Plugin, ViteDevServer } from 'vite'

export function artifactPlugin(roots: string[]): Plugin {
  // A run key identifies its location, including custom output roots with equal folder names.
  let directories = new Map<string, string>()
  async function listRuns() {
    const found = new Map<string, string>()
    const runs: Record<string, unknown>[] = [],
      errors: string[] = []
    for (const root of new Set(roots.map((root) => resolve(root)))) {
      let candidates: string[]
      try {
        const direct = await stat(resolve(root, 'run.yaml')).catch(() => null)
        candidates = direct
          ? [root]
          : (await readdir(root, { withFileTypes: true }))
              .filter((d) => d.isDirectory() && !d.name.startsWith('.'))
              .map((d) => resolve(root, d.name))
      } catch (error) {
        if ((error as NodeJS.ErrnoException).code !== 'ENOENT')
          errors.push(`${root}: ${String(error)}`)
        continue
      }
      for (const candidate of candidates) {
        try {
          const directory = await realpath(candidate)
          const index = resolve(directory, 'run.yaml')
          const source = await readFile(index, 'utf8').catch((error) => {
            if (error.code === 'ENOENT') return null
            throw error
          })
          if (source === null) continue
          const run = parse(source) as Record<string, unknown>
          if (
            run?.schemaVersion !== 8 ||
            typeof run.id !== 'string' ||
            !Array.isArray(run.cases)
          )
            throw new Error('Run schema 8 required')
          const key = createHash('sha256')
            .update(directory)
            .digest('hex')
            .slice(0, 16)
          if (found.has(key)) continue
          found.set(key, directory)
          const { cases, ...metadata } = run
          runs.push({
            ...metadata,
            completedCases: run.completedCases ?? (cases as unknown[]).length,
            key,
          })
        } catch (error) {
          errors.push(`${candidate}: ${String(error)}`)
        }
      }
    }
    runs.sort((a, b) => String(b.startedAt).localeCompare(String(a.startedAt)))
    directories = found
    return { runs, errors }
  }
  const install = (server: Pick<ViteDevServer, 'middlewares'>) => {
    server.middlewares.use((request, response, next) => {
      if (!request.url?.startsWith('/api/')) {
        next()
        return
      }
      void (async () => {
        response.setHeader('Cache-Control', 'no-store')
        response.setHeader('X-Content-Type-Options', 'nosniff')
        if (request.method !== 'GET') {
          response.statusCode = 405
          response.end()
          return
        }
        const url = new URL(request.url!, 'http://localhost')
        if (url.pathname === '/api/runs') {
          response.setHeader('Content-Type', 'application/json')
          response.end(JSON.stringify(await listRuns()))
          return
        }
        const match = /^\/api\/runs\/([a-f0-9]{16})\/files\/(.+)$/.exec(
          url.pathname,
        )
        if (!match) {
          response.statusCode = 404
          response.end()
          return
        }
        if (!directories.has(match[1]!)) await listRuns()
        const directory = directories.get(match[1]!)
        if (!directory) {
          response.statusCode = 404
          response.end()
          return
        }
        const file = decodeURIComponent(match[2]!)
        const parts = file.replaceAll('\\', '/').split('/')
        if (parts.some((p) => !p || p.startsWith('.') || p.includes(':')))
          throw new Error('Invalid artifact path')
        const target = await realpath(resolve(directory, ...parts))
        const inside = relative(directory, target)
        if (
          isAbsolute(inside) ||
          inside === '..' ||
          inside.startsWith(`..${sep}`)
        )
          throw new Error('Artifact outside run')
        const type = file.endsWith('.png')
          ? 'image/png'
          : file.endsWith('.json')
            ? 'application/json'
            : /\.(yaml|md)$/.test(file)
              ? 'text/plain; charset=utf-8'
              : null
        if (!type) throw new Error('Unsupported artifact file')
        let bytes = await readFile(target)
        if (file === 'run.yaml') {
          const run = parse(bytes.toString('utf8'))
          if (run.state === 'running' && run.progress === 'progress.jsonl') {
            // Ignore a partial trailing append. The fixed-size index publishes the committed count.
            const journal = await readFile(resolve(directory, 'progress.jsonl'), 'utf8').catch((error) => {
              if (error.code === 'ENOENT') return ''
              throw error
            })
            run.cases = journal.split('\n').slice(0, -1).slice(0, run.completedCases).map((line) => JSON.parse(line))
            bytes = Buffer.from(stringify(run))
          }
        }
        // Detailed data and images are sealed case assets. Verify what is actually requested.
        if (parts[0] === 'cases') {
          if (parts.length < 3) throw new Error('Invalid case path')
          const caseDirectory = resolve(directory, 'cases', parts[1]!)
          const manifest = JSON.parse(
            await readFile(resolve(caseDirectory, 'manifest.json'), 'utf8'),
          )
          if (manifest.schemaVersion !== 8)
            throw new Error('Result schema 8 required')
          const name = parts.slice(2).join('/')
          if (
            name !== 'manifest.json' &&
            name !== 'overview.md' &&
            manifest.files[name] !==
              createHash('sha256').update(bytes).digest('hex')
          )
            throw new Error('Artifact missing from manifest or modified')
        } else if (!['run.yaml', 'summary.md'].includes(file))
          throw new Error('Not a public result file')
        response.setHeader('Content-Type', type)
        response.end(bytes)
      })().catch((error) => {
        if (response.headersSent) {
          response.destroy()
          return
        }
        response.statusCode = error.code === 'ENOENT' ? 404 : 400
        response.setHeader('Content-Type', 'application/json')
        response.end(JSON.stringify({ error: String(error.message ?? error) }))
      })
    })
  }
  return {
    name: 'parity-artifacts',
    configureServer: install,
    configurePreviewServer: install,
  }
}
