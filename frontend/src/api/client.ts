const BASE_URL = '/api/v1';

/** An RFC 7807 problem response from the API, or a network failure (status 0). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Record<string, string[]> = {},
    /** Seconds, from the Retry-After header of a 429. */
    readonly retryAfter: number | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** Worth retrying automatically: the network, the server, or a rate limit. Never a 400. */
  get isTransient(): boolean {
    return this.status === 0 || this.status === 429 || this.status >= 500;
  }
}

type QueryValue = string | number | undefined | null;

export async function apiGet<T>(path: string, params: Record<string, QueryValue>, signal?: AbortSignal): Promise<T> {
  // Absolute URL on the page's own origin (nginx proxies /api): same request in the browser, and valid
  // for Node's fetch in tests, which rejects relative URLs.
  const url = new URL(`${BASE_URL}${path}`, window.location.origin);
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, String(value));
    }
  }

  let response: Response;
  try {
    response = await fetch(url, { signal, headers: { Accept: 'application/json' } });
  } catch (error) {
    if (signal?.aborted) {
      throw error; // cancelled by TanStack Query (period changed): not an error to show
    }
    throw new ApiError(0, 'Network error');
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

async function toApiError(response: Response): Promise<ApiError> {
  const retryAfterHeader = response.headers.get('Retry-After');
  const retryAfter = retryAfterHeader ? Number(retryAfterHeader) : null;

  try {
    const problem = (await response.json()) as { title?: string; errors?: Record<string, string[]> };
    return new ApiError(response.status, problem.title ?? response.statusText, problem.errors ?? {}, retryAfter);
  } catch {
    return new ApiError(response.status, response.statusText, {}, retryAfter);
  }
}
