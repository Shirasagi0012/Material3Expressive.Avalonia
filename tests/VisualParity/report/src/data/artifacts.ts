import {
  document,
  sampleDocument,
  parseYaml,
  type Run,
  type RunInfo,
  type CaseDocument,
  type Sample,
} from './schema'
export function fileUrl(run: string, path: string) {
  return `/api/runs/${encodeURIComponent(run)}/files/${path.split('/').map(encodeURIComponent).join('/')}`
}
async function request(url: string, signal?: AbortSignal) {
  const response = await fetch(url, { signal })
  if (!response.ok)
    throw new Error(`Unable to read ${url}: ${await response.text()}`)
  return response
}
export async function listRuns(
  signal?: AbortSignal,
): Promise<{ runs: RunInfo[]; errors: string[] }> {
  return (await request('/api/runs', signal)).json()
}
export async function loadCase(run: string, id: string, signal?: AbortSignal) {
  const result = document<CaseDocument>(
    await (await request(fileUrl(run, `cases/${id}/case.yaml`), signal)).text(),
  )
  if (result.metadata.id !== id || !Array.isArray(result.sampleSummaries))
    throw new Error('Case identity or sample index invalid')
  return result
}
export async function loadSample(
  run: string,
  testCase: CaseDocument,
  id: string,
  signal?: AbortSignal,
): Promise<Sample> {
  const path = testCase.samples[id]
  if (!path) throw new Error('Sample not found')
  return sampleDocument(
    await (
      await request(
        fileUrl(run, `cases/${testCase.metadata.id}/${path}`),
        signal,
      )
    ).text(),
    testCase.metadata.id,
    id,
  )
}
export async function loadEnvironment(
  run: string,
  id: string,
  signal?: AbortSignal,
) {
  return parseYaml<Record<string, Record<string, string> | null>>(
    await (
      await request(fileUrl(run, `cases/${id}/environment.yaml`), signal)
    ).text(),
  )
}

export async function loadRun(key: string, signal?: AbortSignal): Promise<Run> {
  return {
    ...document<Run>(
      await (await request(fileUrl(key, 'run.yaml'), signal)).text(),
    ),
    key,
  }
}
