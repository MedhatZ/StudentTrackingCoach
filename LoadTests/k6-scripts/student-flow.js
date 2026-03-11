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
  const listRes = http.get(`${BASE_URL}/Students?highRiskOnly=false&pageNumber=1&pageSize=20`);
  check(listRes, { "students list loaded": (r) => r.status === 200 });

  const detailsRes = http.get(`${BASE_URL}/Students/Details/100001`);
  check(detailsRes, { "student details loaded": (r) => r.status === 200 });

  const guideRes = http.get(`${BASE_URL}/Students/RecommendedStudyGuide/100001`);
  check(guideRes, { "study guide loaded": (r) => r.status === 200 });

  sleep(Math.random() * 2 + 0.5);
}
