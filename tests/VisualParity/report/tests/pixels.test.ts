import { readFile, readdir } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { decodePixels, metrics, pixel, renderPair } from '../src/imaging/pixels'
import { sampleDocument, side, type CaseDocument } from '../src/data/schema'
import { document } from '../src/data/schema'

describe('straight sRGBA8 pixels', () => {
  it('preserves transparent RGB and partial alpha without canvas round trips', async () => {
    const image = decodePixels(
      await readFile(
        new URL('./fixtures/straight-srgba8.png', import.meta.url),
      ),
    )
    expect(pixel(image, 0, 0)).toEqual([80, 120, 200, 0])
    expect(pixel(image, 1, 0)).toEqual([81, 121, 201, 128])
    expect(pixel(image, 2, 0)).toEqual([255, 40, 10, 255])
    expect(pixel(image, 3, 0)).toBeNull()
    expect(pixel(image, 0.5, 0)).toBeNull()
  })

  it('counts transparent RGB and alpha changes with absolute, unshifted region bounds', () => {
    const e = { width: 2, height: 2, bytes: new Uint8Array(16) }
    const a = { ...e, bytes: new Uint8Array(16) }
    a.bytes[0] = 10
    a.bytes[15] = 100
    expect(metrics(e, a)).toEqual({
      differentPixels: 2,
      maximumChannelDelta: [10, 0, 0, 100],
      meanChannelDelta: [2.5, 0, 0, 25],
      differenceBounds: [0, 0, 2, 2],
    })
    expect(metrics(e, a, [1, 1, 1, 1])).toEqual({
      differentPixels: 1,
      maximumChannelDelta: [0, 0, 0, 100],
      meanChannelDelta: [0, 0, 0, 100],
      differenceBounds: [1, 1, 1, 1],
    })
    expect(metrics(e, a, [0, 0, 0, 0]).differenceBounds).toBeNull()
    expect(() => metrics(e, a, [1, 1, 2, 1])).toThrow('region')
  })

  it('changes only the rendered view when applying threshold and opacity', () => {
    const e = { width: 1, height: 1, bytes: new Uint8Array([10, 20, 30, 100]) }
    const a = { ...e, bytes: new Uint8Array([20, 40, 60, 150]) }
    const before = metrics(e, a)
    const visible = renderPair(e, a, 0.5, 49)
    expect([...visible.overlay]).toEqual([15, 30, 45, 125])
    expect([...visible.difference]).toEqual([50, 50, 50, 255])
    expect(visible.highlighted).toBe(1)
    expect(renderPair(e, a, 1, 50).highlighted).toBe(0)
    expect(metrics(e, a)).toEqual(before)
  })
})

// Optional integration with a newly captured run; no checked-in screenshot baseline.
describe.skipIf(!process.env.PARITY_REPORT_TEST_RUN)(
  'actual .NET / front-end comparison',
  () => {
    it('reproduces every recorded full-image and point-region metric', async () => {
      const run = resolve(process.env.PARITY_REPORT_TEST_RUN!)
      let pairs = 0
      for (const caseId of await readdir(resolve(run, 'cases'))) {
        const dir = resolve(run, 'cases', caseId)
        const index = document<CaseDocument>(
          await readFile(resolve(dir, 'case.yaml'), 'utf8'),
        )
        for (const [id, path] of Object.entries(index.samples)) {
          const sample = sampleDocument(
            await readFile(resolve(dir, path), 'utf8'),
            caseId,
            id,
          )
          for (const raster of Object.values(sample.rasters)) {
            if (!raster.metrics) continue
            const [e, a] = await Promise.all(
              (['compose', 'avalonia'] as const).map(async (name) =>
                decodePixels(
                  await readFile(resolve(dir, side(raster.image, name))),
                ),
              ),
            )
            expect(metrics(e!, a!), `${caseId}/${id}`).toEqual(raster.metrics)
            for (const region of raster.regions ?? [])
              expect(metrics(e!, a!, region.bounds)).toEqual(region.metrics)
            pairs++
          }
        }
      }
      expect(pairs).toBeGreaterThan(0)
      console.info(
        `Verified ${pairs} captured PNG pairs and their point regions`,
      )
    })
  },
)
