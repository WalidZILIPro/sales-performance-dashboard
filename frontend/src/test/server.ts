import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { categories, managers, products, sales, summary, trend } from './fixtures';

/** Every API request the page made, so tests can assert on query parameters. */
export const requests: URL[] = [];

export const handlers = [
  http.get('*/api/v1/dashboard/summary', ({ request }) => HttpResponse.json(summary(new URL(request.url)))),
  http.get('*/api/v1/dashboard/trend', ({ request }) => HttpResponse.json(trend(new URL(request.url)))),
  http.get('*/api/v1/dashboard/managers', ({ request }) => HttpResponse.json(managers(new URL(request.url)))),
  http.get('*/api/v1/dashboard/categories', ({ request }) => HttpResponse.json(categories(new URL(request.url)))),
  http.get('*/api/v1/dashboard/products/top', ({ request }) => HttpResponse.json(products(new URL(request.url)))),
  http.get('*/api/v1/sales', ({ request }) => HttpResponse.json(sales(new URL(request.url)))),
];

export const server = setupServer(...handlers);

server.events.on('request:start', ({ request }) => {
  requests.push(new URL(request.url));
});

