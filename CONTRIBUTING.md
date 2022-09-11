# Contributing
First off, thank you for your interest in contributing to Cachr. Before you do, make sure you read the LICENSE file, and CODE_OF_CONDUCT.md

## Coding Standards
This repository follows the coding standards and styles outlined by Microsoft here: https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions (08/12/2022 version)

## Testing
There are many example tests, and all new code is expected to have a suite of tests. We strive for 100% code coverage.

## Submitting Changes
Please send a GitHub pull request to jasoncouture/cachr with a clear list of what has changed, and why the changes are needed.
If your changes are large, please start a discussion before requesting a review. It will save us time, as we're going to ask you to do that anyway!


### Commits are expected to be atomic
* Commits build independently.
  * You can test this with `git fetch; git rebase origin/main --exec dotnet build`.
* Commits pass tests
  * You can test this with `git fetch; git rebase origin/main --exec dotnet test`.
* Commits contain exactly one feature, concern, or refactoring
### Commits and pull requests are expected to be understandable
* Each pull request should focus on one group of related things at a time.
* Please keep commits small where possible
* Always write a clear log message for your commits. One-line messages are fine for small changes, but bigger changes should look like this:
```
$ git commit -m "A brief summary of the commit
> 
> A paragraph describing what changed and its impact."
```


And hey, you made it to the end! Thanks again for taking the time to read this!
