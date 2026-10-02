import { parse } from 'yaml'
export const categories = [
  'geometry',
  'text',
  'properties',
  'motion',
  'mixed',
] as const
export type Category = (typeof categories)[number]
export type Status =
  | 'equal'
  | 'withinTolerance'
  | 'different'
  | 'notCompared'
  | 'error'
export type Side = 'compose' | 'avalonia'
export type Pair<T> = T | { compose: T; avalonia: T }
export type Coverage = {
  equal: number
  withinTolerance: number
  different: number
  notCompared: number
}
export type Trace = {
  sequence: number
  timeUs: number
  operation: string
  detail: string
  data?: unknown
}
export type Collection = { id: string; status: string; reason?: string }
export type Summary = {
  id: string
  status: Status
  expectedTimeUs: number | null
  actualTimeUs: number | null
  stage: string
  categories: Record<Category, Coverage>
  rasters: Coverage
  collection: { compose: Collection[] | null; avalonia: Collection[] | null }
}
export type CaseEntry = {
  id: string
  title: string
  suite: string
  status: Status
  coverage: Coverage
  samples: number
  errors: number
  repeatability: string
}
export type Run = {
  schemaVersion: 8
  key: string
  id: string
  command: string
  state: string
  startedAt: string
  finishedAt: string | null
  selectedCases: number
  cases: CaseEntry[]
}
export type RunInfo = Omit<Run, 'cases'> & { completedCases: number }
export type CaseDocument = {
  schemaVersion: 8
  metadata: {
    id: string
    title: string
    suite: string
    scenario: string
    config: Record<string, unknown>
  }
  status: Status
  errors: string[]
  samples: Record<string, string>
  sampleSummaries: Summary[]
  captures: Record<
    Side,
    {
      runner: string
      caseId: string
      errors: string[]
      sampleOrder: string[]
      traceTail: Trace[]
    } | null
  >
  repeatability: { results: Record<string, string[]> } | null
  diagnostics: string | null
}
export type Field = {
  status: Status
  kind: Pair<string>
  unit: Pair<string>
  space: Pair<string>
  definition: Pair<string>
  source: Pair<string>
  category?: Pair<Category>
  components?: Pair<string[]>
  value?: unknown
  compose?: unknown
  avalonia?: unknown
  delta?: unknown
  reason?: string
  issue?: unknown
}
export type Point = {
  status: Status
  role: Pair<string | null>
  parent: Pair<string | null>
  reason?: string
  issue?: unknown
} & Partial<Record<Category, Record<string, Field>>>
export type Metrics = {
  differentPixels: number
  maximumChannelDelta: number[]
  meanChannelDelta: number[]
  differenceBounds: number[] | null
}
export type Raster = {
  status: Status
  exactStatus?: 'equal' | 'different'
  tolerance?: {
    status: 'withinTolerance' | 'different'
    filter: string
    scale: number
    maximumChannelDeltaLimit: number
    meanMaximumChannelDeltaLimit: number
    full: ToleranceRegion
    regions: ToleranceRegion[]
  }
  present: Side[]
  image: Pair<string>
  width: Pair<number>
  height: Pair<number>
  format: Pair<string>
  space: Pair<string>
  contribution: Pair<string>
  metrics?: Metrics
  regions?: { pointId: string; bounds: number[]; metrics: Metrics }[]
  diffImage?: string
  reason?: string
  issue?: unknown
}
export type ToleranceRegion = {
  pointId: string | null
  bounds: number[]
  maximumChannelDelta: number
  meanMaximumChannelDelta: number
  accepted: boolean
}
export type Sample = {
  schemaVersion: 8
  case: string
  sample: string
  status: Status
  present: Side[]
  timeUs: Pair<number | null>
  frameTimeUs: Pair<number | null>
  stage: Pair<string>
  points: Record<string, Point>
  rasters: Record<string, Raster>
  rasterCollection: Pair<Collection[] | null>
  trace: Record<Side, Trace[] | null>
  reason?: string
  issue?: unknown
}
export type FieldRow = { name: string; category: Category; field: Field }
export function side<T>(value: Pair<T>, name: Side): T {
  return value !== null && typeof value === 'object' && 'compose' in value
    ? (value as { compose: T; avalonia: T })[name]
    : (value as T)
}
export function fields(point?: Point): FieldRow[] {
  return point
    ? categories.flatMap((category) =>
        Object.entries(point[category] ?? {}).map(([name, field]) => ({
          name,
          category,
          field,
        })),
      )
    : []
}
export function value(field: Field, name: Side): unknown {
  return Object.hasOwn(field, 'value') ? field.value : field[name]
}
export const format = (value: unknown): string =>
  value === undefined
    ? 'absent'
    : JSON.stringify(value, (_, v) => (typeof v === 'bigint' ? `${v}` : v), 2)
// Keep integers outside JS's exact range as bigint; never silently round long measurement values.
export function parseYaml<T>(source: string): T {
  return parse(
    source,
    (_key: unknown, v: unknown) =>
      typeof v === 'bigint' &&
      v >= BigInt(Number.MIN_SAFE_INTEGER) &&
      v <= BigInt(Number.MAX_SAFE_INTEGER)
        ? Number(v)
        : v,
    { intAsBigInt: true },
  ) as T
}
export function document<T extends { schemaVersion: number }>(
  source: string,
): T {
  const result = parseYaml<T>(source)
  if (!result || result.schemaVersion !== 8)
    throw new Error(
      'Result schema 8 required. Run the tests again to generate current results.',
    )
  return result
}
export function sampleDocument(
  source: string,
  caseId: string,
  sampleId: string,
): Sample {
  const result = document<Sample>(source)
  if (
    result.case !== caseId ||
    result.sample !== sampleId ||
    !result.points ||
    !result.rasters ||
    !result.trace
  )
    throw new Error('Sample identity or structure differs from its case index')
  return result
}
