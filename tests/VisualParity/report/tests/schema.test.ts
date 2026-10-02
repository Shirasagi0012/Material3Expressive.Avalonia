import { expect, it } from 'vitest'
import {
  document,
  parseYaml,
  sampleDocument,
  side,
  fields,
  value,
  type Point,
} from '../src/data/schema'

it('keeps large YAML integers exact while safe counts remain usable numbers', () => {
  const result = parseYaml<{ count: number; ticks: bigint; label: string }>(
    'count: 13\nticks: 9007199254740993\nlabel: "001"',
  )
  expect(result).toEqual({ count: 13, ticks: 9007199254740993n, label: '001' })
})
it('rejects old results and mismatched sample identities', () => {
  expect(() => document('schemaVersion: 6')).toThrow('schema 8')
  expect(() =>
    sampleDocument(
      'schemaVersion: 8\ncase: a\nsample: b\npoints: {}\nrasters: {}\ntrace: {}',
      'a',
      'c',
    ),
  ).toThrow('identity')
})
it('preserves common nulls, conflicting descriptors, missing values and mixed categories', () => {
  const point = parseYaml<Point>(`status: notCompared
role: {compose: button, avalonia: null}
parent: null
mixed:
  radius:
    status: notCompared
    kind: scalar
    category: {compose: geometry, avalonia: properties}
    unit: {compose: px, avalonia: dp}
    compose: 4
    reason: missing
geometry:
  bounds:
    status: equal
    value: null
`)
  expect(side(point.role, 'avalonia')).toBeNull()
  expect(side(point.parent, 'compose')).toBeNull()
  const rows = fields(point)
  expect(rows.map((r) => r.category)).toEqual(['geometry', 'mixed'])
  expect(value(rows[0]!.field, 'avalonia')).toBeNull()
  expect(value(rows[1]!.field, 'avalonia')).toBeUndefined()
  expect(side(rows[1]!.field.unit, 'avalonia')).toBe('dp')
})
