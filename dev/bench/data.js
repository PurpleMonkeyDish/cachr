window.BENCHMARK_DATA = {
  "lastUpdate": 1663505024897,
  "repoUrl": "https://github.com/jasoncouture/cachr",
  "entries": {
    "Benchmark": [
      {
        "commit": {
          "author": {
            "name": "jasoncouture",
            "username": "jasoncouture"
          },
          "committer": {
            "name": "jasoncouture",
            "username": "jasoncouture"
          },
          "id": "ddb345957a464badc96295fa1c4789e8198157bd",
          "message": "Major refactor",
          "timestamp": "2022-09-11T23:29:20Z",
          "url": "https://github.com/jasoncouture/cachr/pull/18/commits/ddb345957a464badc96295fa1c4789e8198157bd"
        },
        "date": 1663505024302,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAddEntry(range: 10)",
            "value": 2281.7416196550644,
            "unit": "ns",
            "range": "± 3.4428004060193644"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorDuplicate(range: 10)",
            "value": 2024.7489498683385,
            "unit": "ns",
            "range": "± 34.43603643443089"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAllShardsDuplicateDetection(range: 10)",
            "value": 675816.61640625,
            "unit": "ns",
            "range": "± 1700.0967737474616"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.LockContention(range: 10)",
            "value": 18389.377795245196,
            "unit": "ns",
            "range": "± 609.82430442584"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAddEntry(range: 1000)",
            "value": 530085.27734375,
            "unit": "ns",
            "range": "± 1718.6414951865986"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorDuplicate(range: 1000)",
            "value": 177215.27249348958,
            "unit": "ns",
            "range": "± 290.9899433123271"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAllShardsDuplicateDetection(range: 1000)",
            "value": 68364740.05833334,
            "unit": "ns",
            "range": "± 281179.86747101496"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.LockContention(range: 1000)",
            "value": 859256.7671595982,
            "unit": "ns",
            "range": "± 4493.81411348697"
          }
        ]
      }
    ]
  }
}