import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "https://localhost:5001";

export const options = {
  vus: Number(__ENV.VUS || 50),
  duration: __ENV.DURATION || "10m",
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    http_req_failed: ["rate<0.01"]
  }
};

export default function () {
  const dashboardRes = http.get(`${BASE_URL}/Advisor?search=100&pageNumber=1&pageSize=20`);
  check(dashboardRes, { "advisor dashboard loaded": (r) => r.status === 200 });

  const detailRes = http.get(`${BASE_URL}/Advisor/Student/100001`);
  check(detailRes, { "advisor student detail loaded": (r) => r.status === 200 });

  const pendingRes = http.get(`${BASE_URL}/Advisor/PendingReviews`);
  check(pendingRes, { "pending reviews loaded": (r) => r.status === 200 });

  sleep(Math.random() * 2 + 0.5);
}
