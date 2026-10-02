import { shallowRef, ref, watch, type ComputedRef, type ShallowRef } from 'vue'
import { loadSample } from '../data/artifacts'
import type { CaseDocument, Sample } from '../data/schema'
export function useSample(
  run: ComputedRef<string>,
  testCase: ShallowRef<CaseDocument | undefined>,
  id: ComputedRef<string>,
) {
  const sample = shallowRef<Sample>(),
    loading = ref(false),
    error = ref('')
  watch(
    [run, testCase, id],
    async (_, __, cleanup) => {
      const controller = new AbortController()
      cleanup(() => controller.abort())
      sample.value = undefined
      error.value = ''
      loading.value = false
      if (!testCase.value || !id.value) return
      loading.value = true
      try {
        const result = await loadSample(
          run.value,
          testCase.value,
          id.value,
          controller.signal,
        )
        if (!controller.signal.aborted) sample.value = result
      } catch (e) {
        if (!controller.signal.aborted) error.value = String(e)
      } finally {
        if (!controller.signal.aborted) loading.value = false
      }
    },
    { immediate: true },
  )
  return { sample, loading, error }
}
