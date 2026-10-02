import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { resolve, dirname, basename } from 'node:path'
import { createHash } from 'node:crypto'
import { createServer, type ViteDevServer } from 'vite'
import { stringify } from 'yaml'
import { beforeAll, afterAll, expect, it } from 'vitest'
import { artifactPlugin } from '../server/artifacts'

let root: string, server: ViteDevServer, base: string, key: string
beforeAll(async () => {
  root = await mkdtemp(resolve(tmpdir(), 'parity-report-test-'))
  const roots = [resolve(root, 'first'), resolve(root, 'second')]
  for (const [i, parent] of roots.entries()) {
    const run = resolve(parent, 'same-name'),
      testCase = resolve(run, 'cases', 'test.example')
    await mkdir(testCase, { recursive: true })
    await mkdir(resolve(run, '.work'), { recursive: true })
    await writeFile(resolve(run, '.work', 'secret.yaml'), 'internal')
    await writeFile(
      resolve(run, 'run.yaml'),
      stringify({
        schemaVersion: 8,
        id: 'same-name',
        state: 'running',
        cases: [],
        startedAt: `2026-10-0${i + 1}`,
      }),
    )
    await writeFile(resolve(testCase, 'case.yaml'), 'schemaVersion: 8\n')
    await writeFile(
      resolve(testCase, 'manifest.json'),
      JSON.stringify({
        schemaVersion: 8,
        files: {
          'case.yaml': createHash('sha256')
            .update('schemaVersion: 8\n')
            .digest('hex'),
        },
      }),
    )
  }
  server = await createServer({
    configFile: false,
    root,
    plugins: [artifactPlugin([...roots, roots[0]!])],
    server: { host: 'localhost', port: 0 },
    logLevel: 'silent',
  })
  await server.listen()
  base = server.resolvedUrls!.local[0]!.replace(/\/$/, '')
  const index = await (await fetch(`${base}/api/runs`)).json()
  expect(index.errors).toEqual([])
  expect(index.runs).toHaveLength(2)
  key = index.runs[0].key
  expect(key).not.toBe(index.runs[1].key)
})
afterAll(async () => {
  await server?.close()
  // Delete only this test's freshly allocated temp directory.
  if (
    root &&
    dirname(root) === resolve(tmpdir()) &&
    basename(root).startsWith('parity-report-test-')
  )
    await rm(root, { recursive: true, force: true })
})
it('serves current indexes and sealed case facts without embedding sample data in listings', async () => {
  expect(
    await (
      await fetch(`${base}/api/runs/${key}/files/cases/test.example/case.yaml`)
    ).text(),
  ).toBe('schemaVersion: 8\n')
  const index = await (
    await fetch(`${base}/api/runs/${key}/files/run.yaml`)
  ).text()
  expect(index).toContain('state: running')
  const list = await (await fetch(`${base}/api/runs`)).json()
  expect(list.runs[0]).not.toHaveProperty('cases')
})
it('sees newly completed runs and reports malformed indexes independently', async () => {
  const parent = resolve(root, 'first'),
    run = resolve(parent, 'new-run')
  await mkdir(run)
  await writeFile(
    resolve(run, 'run.yaml'),
    stringify({
      schemaVersion: 8,
      id: 'new-run',
      state: 'completed',
      cases: [{}],
      startedAt: '2026-10-03',
    }),
  )
  const broken = resolve(parent, 'broken')
  await mkdir(broken)
  await writeFile(resolve(broken, 'run.yaml'), 'schemaVersion: 6')
  const index = await (await fetch(`${base}/api/runs`)).json()
  expect(index.runs).toHaveLength(3)
  expect(index.runs[0]).toMatchObject({
    id: 'new-run',
    completedCases: 1,
    state: 'completed',
  })
  expect(index.errors).toHaveLength(1)
})
it('refuses modified case data, private files, traversal and writes', async () => {
  await writeFile(
    resolve(root, 'second', 'same-name', 'cases', 'test.example', 'case.yaml'),
    'tampered',
  )
  expect(
    (await fetch(`${base}/api/runs/${key}/files/cases/test.example/case.yaml`))
      .status,
  ).toBe(400)
  expect(
    (await fetch(`${base}/api/runs/${key}/files/%2ework/secret.yaml`)).status,
  ).toBe(400)
  expect(
    (await fetch(`${base}/api/runs/${key}/files/%2e%2e%2fsecret.yaml`)).status,
  ).toBe(400)
  expect((await fetch(`${base}/api/runs`, { method: 'POST' })).status).toBe(405)
})
