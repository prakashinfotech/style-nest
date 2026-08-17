/**
 * ENH-INFRA-008 — k6 Soak Load Test
 * TC-NFR-LOAD-004 / NFR-PERF-008
 *
 * Profile: 1,000 VUs sustained for 60 min.
 * Thresholds:
 *   - HTTP error rate < 1 %
 *   - p95 response time < 2 s (NFR-PERF-008)
 *   - p99 response time < 5 s
 *   - Memory / GC regressions visible via Serilog metrics endpoint
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Trend } from "k6/metrics";

const BASE_URL   = __ENV.BASE_URL || "http://localhost:5000";
const catalogP95 = new Trend("catalog_browse_p95");

export const options = {
  stages: [
    { duration: "5m",  target: 1000 }, // warm-up ramp
    { duration: "60m", target: 1000 }, // soak: hold 1 K VUs for 60 min
    { duration: "5m",  target: 0    }, // ramp-down
  ],
  thresholds: {
    http_req_failed:   ["rate<0.01"],   // < 1 % error rate
    http_req_duration: ["p(95)<2000", "p(99)<5000"],
    catalog_browse_p95:["p(95)<2000"],
  },
};

export default function () {
  // Health probe
  http.get(`${BASE_URL}/health`);

  // Catalog browse — simulate typical user session
  const start = Date.now();
  const catalog = http.get(`${BASE_URL}/api/v1/products?page=1&pageSize=20`);
  catalogP95.add(Date.now() - start);

  check(catalog, {
    "catalog 200": (r) => r.status === 200,
  });

  // PDP — random product between 1 and 200 (seeded products)
  const productId = Math.floor(Math.random() * 200) + 1;
  http.get(`${BASE_URL}/api/v1/products/${productId}`);

  sleep(2);
}
