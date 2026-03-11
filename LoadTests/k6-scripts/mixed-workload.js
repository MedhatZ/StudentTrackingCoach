import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "https://localhost:5001";
const TEST_TYPE = (__ENV.TEST_TYPE || "normal").toLowerCase();

const scenarioMap = {
  normal: { vus: 50, duration: "10m" },
  peak: { vus: 200, duration: "15m" },
  stress: { vus: 500, duration: "20m" },
  soak: { vus: 120, duration: "1h" }
};

const selected = scenarioMap[TEST_TYPE] || scenarioMap.normal;

export const options = {
  vus: Number(__ENV.VUS || selected.vus),
  duration: __ENV.DURATION || selected.duration,
  thresholds: {
    http_req_duration: ["p(95)<2000", "p(99)<4000"],
    http_req_failed: ["rate<0.01"],
    checks: ["rate>0.99"]
  }
};

function hitAdvisorFlow() {
  const res = http.get(`${BASE_URL}/Advisor?search=10&pageNumber=1&pageSize=20`);
  check(res, { "advisor page ok": (r) => r.status === 200 });
}

function hitStudentFlow() {
  const res = http.get(`${BASE_URL}/Students/RecommendedStudyGuide/100001`);
  check(res, { "study guide page ok": (r) => r.status === 200 });
}

function hitAiEndpointLikeFlow() {
  const res = http.get(`${BASE_URL}/Advisor/Student/100001?regenerateAi=true`);
  check(res, { "ai generation flow ok": (r) => r.status === 200 });
}

export default function () {
  const roll = Math.random();

  if (roll < 0.45) {
    hitAdvisorFlow();
  } else if (roll < 0.80) {
    hitStudentFlow();
  } else {
    hitAiEndpointLikeFlow();
  }

  sleep(Math.random() * 1.5 + 0.3);
}
