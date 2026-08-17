// k6 Performance Test — ENH-AUTH-009: POST /api/v1/auth/otp/send
// NFR: p95 ≤ 200ms server-side (excluding SMS gateway RTT)
// Load: 100 req/s sustained for 30s
//
// STATUS: CANNOT RUN — endpoint POST /api/v1/auth/otp/send does not exist.
// The endpoint currently returns HTTP 404.  This script is provided for
// future execution once the endpoint is implemented.
//
// Run command (once endpoint exists):
//   k6 run --env BASE_URL=http://localhost:5001 docs/test-reports/enh-auth-009-k6-perf.js

import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const errorRate = new Rate("error_rate");
const serverResponseTime = new Trend("server_response_time_ms", true);

export const options = {
  scenarios: {
    otp_send_load: {
      executor: "constant-arrival-rate",
      rate: 100,           // 100 req/s
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 50,
      maxVUs: 150,
    },
  },
  thresholds: {
    // Accept criteria: p95 ≤ 200ms server-side (http_req_duration)
    "http_req_duration": ["p(95)<200"],
    "error_rate": ["rate<0.01"],   // < 1% error rate
  },
};

// Indian mobile numbers pool (test numbers only)
const testPhones = [
  "+919876543210",
  "+919876543211",
  "+919876543212",
  "+919876543213",
  "+919876543214",
];

export default function () {
  const phone = testPhones[Math.floor(Math.random() * testPhones.length)];

  const res = http.post(
    `${__ENV.BASE_URL}/api/v1/auth/otp/send`,
    JSON.stringify({ phoneNumber: phone }),
    {
      headers: {
        "Content-Type": "application/json",
        "Accept": "application/json",
      },
      tags: { endpoint: "otp_send" },
    }
  );

  // Pass criteria assertions
  const passed = check(res, {
    "status is 200 or 429": (r) => r.status === 200 || r.status === 429,
    "response has maskedPhone": (r) => {
      if (r.status !== 200) return true; // skip for 429
      const body = JSON.parse(r.body);
      return /^\+91-XXX-XXX-\d{4}$/.test(body.maskedPhone ?? "");
    },
    "response has expiry 300s": (r) => {
      if (r.status !== 200) return true;
      const body = JSON.parse(r.body);
      if (!body.expiresAt) return false;
      const expiry = new Date(body.expiresAt).getTime();
      const now = Date.now();
      const diffSeconds = (expiry - now) / 1000;
      return diffSeconds >= 290 && diffSeconds <= 310;
    },
    "p95 server time ≤ 200ms": (r) => r.timings.duration < 200,
  });

  errorRate.add(!passed);
  serverResponseTime.add(res.timings.duration);

  sleep(0.01); // minimal think-time to avoid tight loops
}

export function handleSummary(data) {
  const p95 = data.metrics["http_req_duration"]?.values?.["p(95)"] ?? "N/A";
  const errors = data.metrics["error_rate"]?.values?.rate ?? "N/A";

  return {
    stdout: `
=== ENH-AUTH-009 k6 Performance Results ===
p95 server response time : ${p95} ms  (threshold: ≤ 200ms)
Error rate               : ${errors} (threshold: < 1%)
Pass criteria met        : ${p95 <= 200 && errors < 0.01 ? "YES" : "NO"}
    `,
  };
}
