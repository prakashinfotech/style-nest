/**
 * ENH-INFRA-008 — k6 Spike Load Test
 * TC-NFR-LOAD-003 / NFR-PERF-008
 *
 * Profile: ramp from 100 → 10,000 VUs in 30 s, hold 1 min, ramp down.
 * Thresholds:
 *   - HTTP error rate < 1 %
 *   - p95 response time < 2 s
 */

import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

export const options = {
  stages: [
    { duration: "30s", target: 10000 }, // spike: 100 → 10 K in 30 s
    { duration: "1m",  target: 10000 }, // hold
    { duration: "30s", target: 0     }, // ramp-down
  ],
  thresholds: {
    http_req_failed:   ["rate<0.01"],         // < 1 % error rate
    http_req_duration: ["p(95)<2000"],        // p95 < 2 s
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/health`);

  check(res, {
    "status 200": (r) => r.status === 200,
  });

  // Catalog browse — most common user action
  http.get(`${BASE_URL}/api/v1/products?page=1&pageSize=20`);

  sleep(1);
}
