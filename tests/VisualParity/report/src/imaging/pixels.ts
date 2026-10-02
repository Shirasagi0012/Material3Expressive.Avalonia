import { decode } from 'fast-png'
import type { Metrics } from '../data/schema'
export type Pixels = { width: number; height: number; bytes: Uint8Array }
export function decodePixels(png: ArrayBuffer | Uint8Array): Pixels {
  const image = decode(png, { checkCrc: true })
  // Capture schema 4 writes normalized sRGB 8-bit RGB/RGBA PNG, not a browser colour transform.
  if (
    image.depth !== 8 ||
    ![3, 4].includes(image.channels) ||
    image.palette ||
    image.transparency
  )
    throw new Error('Canonical sRGB 8-bit RGB/RGBA PNG required')
  const bytes = new Uint8Array(image.width * image.height * 4)
  for (let p = 0; p < image.width * image.height; p++) {
    for (let c = 0; c < 3; c++)
      bytes[p * 4 + c] = image.data[p * image.channels + c]!
    bytes[p * 4 + 3] = image.channels === 4 ? image.data[p * 4 + 3]! : 255
  }
  return { width: image.width, height: image.height, bytes }
}
export function pixel(
  image: Pixels | null,
  x: number,
  y: number,
): number[] | null {
  return image &&
    Number.isInteger(x) &&
    Number.isInteger(y) &&
    x >= 0 &&
    y >= 0 &&
    x < image.width &&
    y < image.height
    ? [
        ...image.bytes.subarray(
          (y * image.width + x) * 4,
          (y * image.width + x) * 4 + 4,
        ),
      ]
    : null
}
export function metrics(
  e: Pixels,
  a: Pixels,
  region = [0, 0, e.width, e.height],
): Metrics {
  const [left, top, width, height] = region as [number, number, number, number]
  if (
    e.width !== a.width ||
    e.height !== a.height ||
    region.length !== 4 ||
    region.some((n) => !Number.isInteger(n)) ||
    left < 0 ||
    top < 0 ||
    width < 0 ||
    height < 0 ||
    left + width > e.width ||
    top + height > e.height
  )
    throw new Error('Invalid comparison dimensions or region')
  let differentPixels = 0,
    minX = e.width,
    minY = e.height,
    maxX = -1,
    maxY = -1
  const maximumChannelDelta = [0, 0, 0, 0],
    sum = [0, 0, 0, 0]
  for (let y = top; y < top + height; y++)
    for (let x = left; x < left + width; x++) {
      let changed = false
      for (let c = 0; c < 4; c++) {
        const i = (y * e.width + x) * 4 + c,
          d = Math.abs(a.bytes[i]! - e.bytes[i]!)
        sum[c]! += d
        maximumChannelDelta[c] = Math.max(maximumChannelDelta[c]!, d)
        changed ||= d !== 0
      }
      if (changed) {
        differentPixels++
        minX = Math.min(minX, x)
        minY = Math.min(minY, y)
        maxX = Math.max(maxX, x)
        maxY = Math.max(maxY, y)
      }
    }
  return {
    differentPixels,
    maximumChannelDelta,
    meanChannelDelta: sum.map((n) =>
      width * height === 0 ? 0 : n / (width * height),
    ),
    differenceBounds: differentPixels
      ? [minX, minY, maxX - minX + 1, maxY - minY + 1]
      : null,
  }
}
export function renderPair(
  e: Pixels,
  a: Pixels,
  opacity: number,
  threshold: number,
) {
  if (e.width !== a.width || e.height !== a.height)
    throw new Error('Images cannot be paired')
  const overlay = new Uint8ClampedArray(e.bytes.length),
    difference = new Uint8ClampedArray(e.bytes.length)
  let highlighted = 0
  for (let i = 0; i < e.bytes.length; i += 4) {
    let max = 0
    for (let c = 0; c < 4; c++) {
      overlay[i + c] = Math.round(
        e.bytes[i + c]! * (1 - opacity) + a.bytes[i + c]! * opacity,
      )
      max = Math.max(max, Math.abs(a.bytes[i + c]! - e.bytes[i + c]!))
    }
    const d = max > threshold ? max : 0
    if (d) highlighted++
    difference.set([d, d, d, 255], i)
  }
  return { overlay, difference, highlighted }
}
