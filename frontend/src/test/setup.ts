import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { MotionGlobalConfig } from 'framer-motion';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { server } from './server';

// Animations finish instantly, so tests assert final values, not frames of a count-up.
MotionGlobalConfig.skipAnimations = true;

// jsdom lacks these browser APIs; Recharts and framer-motion need them to exist.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver ??= ResizeObserverStub as unknown as typeof ResizeObserver;
window.matchMedia ??= ((query: string) => ({
  matches: false,
  media: query,
  onchange: null,
  addListener: () => {},
  removeListener: () => {},
  addEventListener: () => {},
  removeEventListener: () => {},
  dispatchEvent: () => false,
})) as typeof window.matchMedia;

// jsdom has no layout, so every chart measures 0×0 and Recharts warns about it. Only that one
// message is filtered, so real warnings still show.
const warn = console.warn.bind(console);
console.warn = (...args: unknown[]) => {
  if (typeof args[0] === 'string' && args[0].startsWith('The width(0) and height(0) of chart')) return;
  warn(...args);
};

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  cleanup();
  server.resetHandlers();
  window.history.replaceState(null, '', '/');
  localStorage.clear();
  document.documentElement.classList.remove('dark');
});
afterAll(() => server.close());
