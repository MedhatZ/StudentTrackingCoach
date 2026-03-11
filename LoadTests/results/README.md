# Load Testing Results

Use this folder to store k6 output artifacts from local runs and CI nightly runs.

## Benchmarks

- Response time: p95 `< 2s`, p99 `< 4s`
- Error rate: `< 1%` under expected load
- Expected infra targets: CPU `< 70%`, memory `< 80%`, zero functional errors

## Test Matrix

| Profile | Concurrency | Duration | Purpose |
|---|---:|---|---|
| Normal | 50 users | 10m | Baseline daily behavior |
| Peak | 200 users | 15m | Registration/semester surge |
| Stress | 500 users | 20m | Identify breaking point |
| Soak | 120 users | 1h | Memory leaks and degradation |

## Metrics to Collect

- Avg / p95 / p99 response times
- Requests per second
- Error rate by endpoint
- CPU and memory from host/cluster monitoring

## Reporting Template

For each run, record:

1. Commit SHA and environment
2. Test profile and runtime config
3. Key latency/throughput/error values
4. Resource utilization screenshots or links
5. Regressions and follow-up actions
