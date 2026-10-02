/// <reference lib="webworker" />
import { decodePixels, pixel, metrics, renderPair, type Pixels } from './pixels'
import type { Input, Output, ImageSource } from './messages'
let version = 0,
  compose: Pixels | null = null,
  avalonia: Pixels | null = null,
  comparable = false
let controller: AbortController | undefined
const send = (result: Output, transfers: Transferable[] = []) =>
  self.postMessage(result, transfers)
self.onmessage = async ({ data }: MessageEvent<Input>) => {
  try {
    if (data.type === 'load') {
      controller?.abort()
      controller = new AbortController()
      const signal = controller.signal
      version = data.version
      compose = null
      avalonia = null
      comparable = false
      async function image(source: ImageSource | null) {
        if (!source) return null
        const response = await fetch(source.url, { signal })
        if (!response.ok)
          throw new Error(`Unable to load PNG: ${response.status}`)
        const decoded = decodePixels(await response.arrayBuffer())
        if (decoded.width !== source.width || decoded.height !== source.height)
          throw new Error('PNG dimensions differ from the sample')
        return decoded
      }
      const pair = await Promise.all([
        image(data.sources.compose),
        image(data.sources.avalonia),
      ])
      if (data.version !== version || signal.aborted) return
      ;[compose, avalonia] = pair
      comparable = data.comparable
      // Recorded comparison remains authoritative. A mismatch means decoding/integrity failed,
      // never a new pass/fail interpretation by the UI.
      if (comparable && compose && avalonia && data.recorded) {
        const actual = metrics(compose, avalonia)
        if (
          actual.differentPixels !== data.recorded.differentPixels ||
          JSON.stringify(actual.maximumChannelDelta) !==
            JSON.stringify(data.recorded.maximumChannelDelta) ||
          JSON.stringify(actual.differenceBounds) !==
            JSON.stringify(data.recorded.differenceBounds) ||
          actual.meanChannelDelta.some(
            (n, i) => n !== data.recorded!.meanChannelDelta[i],
          )
        )
          throw new Error(
            'Decoded pixels differ from recorded comparison metrics',
          )
      }
      send({ version, type: 'loaded' })
      return
    }
    if (data.version !== version) return
    if (data.type === 'pixel') {
      const e = pixel(compose, data.x, data.y),
        a = pixel(avalonia, data.x, data.y)
      send({
        version,
        type: 'pixel',
        x: data.x,
        y: data.y,
        compose: e,
        avalonia: a,
        delta: comparable && e && a ? a.map((v, c) => v - e[c]!) : null,
      })
    } else if (comparable && compose && avalonia) {
      if (data.type === 'region')
        send({
          version,
          type: 'region',
          metrics: metrics(compose, avalonia, data.bounds),
        })
      else if (data.type === 'render') {
        const result = renderPair(
          compose,
          avalonia,
          data.opacity,
          data.threshold,
        )
        send(
          {
            version,
            type: 'rendered',
            width: compose.width,
            height: compose.height,
            ...result,
          },
          [result.overlay.buffer, result.difference.buffer],
        )
      }
    }
  } catch (error) {
    if ((error as Error).name !== 'AbortError' && data.version === version)
      send({ version, type: 'error', error: String(error) })
  }
}
