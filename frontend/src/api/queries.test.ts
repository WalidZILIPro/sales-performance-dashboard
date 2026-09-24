import { describe, expect, it } from 'vitest';
import { ApiError } from './client';
import { createQueryClient } from './queries';

const { retry, retryDelay } = createQueryClient().getDefaultOptions().queries as {
  retry: (failureCount: number, error: Error) => boolean;
  retryDelay: (attempt: number, error: Error) => number;
};

describe('retry policy', () => {
  it('never retries a validation error: it would fail the same way', () => {
    expect(retry(0, new ApiError(400, 'bad'))).toBe(false);
  });

  it('retries network failures, server errors and rate limits, at most twice', () => {
    expect(retry(0, new ApiError(0, 'offline'))).toBe(true);
    expect(retry(1, new ApiError(503, 'down'))).toBe(true);
    expect(retry(0, new ApiError(429, 'slow down'))).toBe(true);
    expect(retry(2, new ApiError(503, 'down'))).toBe(false);
  });

  it("waits as long as the server's Retry-After asks on a 429", () => {
    expect(retryDelay(0, new ApiError(429, 'slow down', {}, 3))).toBe(3000);
    expect(retryDelay(1, new ApiError(503, 'down'))).toBe(2000);
  });
});
