import type { Metrics, Side } from '../data/schema'
export type ImageSource = { url: string; width: number; height: number }
export type Input = { version: number } & (
  | {
      type: 'load'
      sources: Record<Side, ImageSource | null>
      comparable: boolean
      recorded?: Metrics
    }
  | { type: 'render'; opacity: number; threshold: number }
  | { type: 'pixel'; x: number; y: number }
  | { type: 'region'; bounds: number[] }
)
export type Output = { version: number } & (
  | { type: 'loaded' }
  | { type: 'error'; error: string }
  | {
      type: 'rendered'
      width: number
      height: number
      overlay: Uint8ClampedArray
      difference: Uint8ClampedArray
      highlighted: number
    }
  | {
      type: 'pixel'
      x: number
      y: number
      compose: number[] | null
      avalonia: number[] | null
      delta: number[] | null
    }
  | { type: 'region'; metrics: Metrics }
)
