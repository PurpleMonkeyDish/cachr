window.BENCHMARK_DATA = {
  "lastUpdate": 1663510827497,
  "repoUrl": "https://github.com/jasoncouture/cachr",
  "entries": {
    "Benchmark": [
      {
        "commit": {
          "author": {
            "email": "jasonc@alertr.info",
            "name": "Jason Couture",
            "username": "jasoncouture"
          },
          "committer": {
            "email": "jasonc@alertr.info",
            "name": "Jason Couture",
            "username": "jasoncouture"
          },
          "distinct": false,
          "id": "de2faab1cb509c80788163d6a9777de65d4a4d01",
          "message": "Fix syntax error",
          "timestamp": "2022-09-18T10:16:35-04:00",
          "tree_id": "5e22ebe1b7db596ca4141d5770473d9a380297b0",
          "url": "https://github.com/jasoncouture/cachr/commit/de2faab1cb509c80788163d6a9777de65d4a4d01"
        },
        "date": 1663510827151,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAddEntry(range: 10)",
            "value": 1852.6081403096516,
            "unit": "ns",
            "range": "± 4.4241835033463355"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorDuplicate(range: 10)",
            "value": 1567.4747772216797,
            "unit": "ns",
            "range": "± 3.647033793186918"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAllShardsDuplicateDetection(range: 10)",
            "value": 557981.625,
            "unit": "ns",
            "range": "± 1313.2640523033429"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.LockContention(range: 10)",
            "value": 13291.092447916666,
            "unit": "ns",
            "range": "± 912.5213896521492"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAddEntry(range: 1000)",
            "value": 425975.57421875,
            "unit": "ns",
            "range": "± 1179.0570016136742"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorDuplicate(range: 1000)",
            "value": 141101.52709960938,
            "unit": "ns",
            "range": "± 228.16655504536794"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.DuplicateDetectorAllShardsDuplicateDetection(range: 1000)",
            "value": 58692681.92592593,
            "unit": "ns",
            "range": "± 53558.402208845764"
          },
          {
            "name": "Cachr.Benchmarks.DuplicateTrackerBenchmarks.LockContention(range: 1000)",
            "value": 545301.7350260416,
            "unit": "ns",
            "range": "± 9387.77232166554"
          }
        ]
      }
    ]
  }
}