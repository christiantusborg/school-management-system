import { ref } from 'vue'
import apiClient from '../../api/client.js'

// Module-level cache. Asset payloads are immutable for the lifetime of an asset
// id (delete creates a tombstone, not an in-place change), so blob URLs are
// safe to share across components. Prevents N parallel fetches when both the
// picker and the editor render the same image.
const cache = new Map() // assetId → blobUrl
const inflight = new Map() // assetId → Promise<string>

export async function fetchAssetBlobUrl(assetId) {
  if (!assetId) return ''
  if (cache.has(assetId)) return cache.get(assetId)
  if (inflight.has(assetId)) return inflight.get(assetId)
  const p = apiClient
    .get(`/v1/admin/letter-assets/${assetId}/file`, { responseType: 'blob' })
    .then(res => {
      const url = URL.createObjectURL(res.data)
      cache.set(assetId, url)
      inflight.delete(assetId)
      return url
    })
    .catch(err => {
      inflight.delete(assetId)
      throw err
    })
  inflight.set(assetId, p)
  return p
}

export function invalidateAssetBlobUrl(assetId) {
  const existing = cache.get(assetId)
  if (existing) {
    URL.revokeObjectURL(existing)
    cache.delete(assetId)
  }
}

/// Reactive map of assetId → blobUrl ('' while loading, '' on error).
/// Use in components for templated <img :src="urls[id]">.
export function useAssetUrls() {
  const urls = ref({})

  async function ensure(assetId) {
    if (!assetId || urls.value[assetId]) return urls.value[assetId]
    try {
      const url = await fetchAssetBlobUrl(assetId)
      urls.value = { ...urls.value, [assetId]: url }
      return url
    } catch {
      urls.value = { ...urls.value, [assetId]: '' }
      return ''
    }
  }

  async function ensureAll(assetIds) {
    await Promise.all((assetIds || []).map(ensure))
  }

  function invalidate(assetId) {
    invalidateAssetBlobUrl(assetId)
    if (urls.value[assetId]) {
      const next = { ...urls.value }
      delete next[assetId]
      urls.value = next
    }
  }

  return { urls, ensure, ensureAll, invalidate }
}
