import { shallowRef, ref, watch, type ComputedRef } from 'vue'
import { loadCase, loadEnvironment } from '../data/artifacts'
import type { CaseDocument } from '../data/schema'
export function useCase(run: ComputedRef<string>, id: ComputedRef<string>) {
  const testCase = shallowRef<CaseDocument>(),
    environment = shallowRef<Record<string, Record<string, string> | null>>()
  const loading = ref(false),
    error = ref('')
  watch(
    [run, id],
    async (_, __, cleanup) => {
      const controller = new AbortController()
      cleanup(() => controller.abort())
      loading.value = true
      error.value = ''
      testCase.value = undefined
      environment.value = undefined
      try {
        const [data, env] = await Promise.all([
          loadCase(run.value, id.value, controller.signal),
          loadEnvironment(run.value, id.value, controller.signal),
        ])
        if (!controller.signal.aborted) {
          testCase.value = data
          environment.value = env
        }
      } catch (e) {
        if (!controller.signal.aborted) error.value = String(e)
      } finally {
        if (!controller.signal.aborted) loading.value = false
      }
    },
    { immediate: true },
  )
  return { testCase, environment, loading, error }
}
