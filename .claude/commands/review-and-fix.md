---
description: Review and fix files that have changes not committed to Git
argument-hint: Optional instructions
---

## Context

- Current git status: !`git status`
- Recent commits: !`git log --oneline -10`
- Information of the last commit: !`git show --name-status`
- User input: $ARGUMENTS

## Your Task

### 1. Figure what the scope of the review is

Use the first scope that is available:

1. If user input specifies review scope, then deduce the scope from the user input

2. otherwise, if there are modified files, not committed to git, (according to git status), then these files are to be reviewed.

3. otherwise, the files in the last commit are to be reviewed.

### 2. Review and fix non-test code

Use the code-correcter agent to review all non-test code in context and fix any issues found.

### 3. Review and fix test code

Use the test-correcter agent to review all test code in context and fix any issues found.

### 4. Provide final review analysis

Use the code-reviewer agent to provide a final assessment of the code.