# Repository instructions

## Agent Notes

Create or update an Agent Note for every non-trivial change to behavior,
architecture, cross-file conventions, workflows, testing strategy, disk
formats, configuration formats, or wire formats. Purely mechanical or local
changes do not need a note.

Before writing a note, search `.agents/notes/`:

- Update an existing note when it owns the same decision.
- When a decision reverses an earlier one, create a new note and cross-link both
  notes.
- Do not duplicate rationale already owned by another note.

Use this path:

```text
.agents/notes/{lifecycle}/{class}/yyyy-mm-dd-topic.md
```

Use the date when the topic was first proposed. Lifecycles are `proposed`,
`implemented`, and `rejected`. Classes are `feature`, `bug-fix`,
`simplification`, `architecture`, `process`, and `testing`.

Every note starts with:

```text
# Agent Note: <title>

Status: <status>
```

Lifecycle requirements:

- `proposed` includes `Problem`, `Proposal`, `Alternatives considered`,
  `Acceptance criteria`, and `Risks`.
- `implemented` includes `Problem`, `Decision`, `Alternatives considered`, and
  `Consequences`. Describe delivered facts in the present tense. Do not retain
  `Proposal`, `Plan`, `Migration plan`, or `Acceptance criteria` headings.
- `rejected` uses `Status: rejected — <one-line reason>` and includes at least
  `Problem`, `Proposal`, and `Alternatives considered`.

`Alternatives considered` records only options that were genuinely evaluated
and why they were not selected. Agent Notes explain why a decision exists,
what was rejected, and its consequences; they do not repeat implementation
steps or file inventories.

### Bilingual triplets

Every note consists of:

- `yyyy-mm-dd-topic.md`
- `yyyy-mm-dd-topic.zh.md`
- `yyyy-mm-dd-topic.i18n.yaml`

The English file includes:

```markdown
English | [中文](yyyy-mm-dd-topic.zh.md)
```

The Chinese file includes:

```markdown
[English](yyyy-mm-dd-topic.md) | 中文
```

Keep the section, list, table, code-block, and link structure aligned between
both languages. Retain the literal `# Agent Note:` and `Status:` markers in the
Chinese file. After confirming translation consistency, record the sidecar:

```bash
pnpm run verify-translation-pairing --write .agents/notes/<lifecycle>/<class>/yyyy-mm-dd-topic.md
```

### Supersession

When adding a note, check related notes:

- If it completely replaces a note and absorbs all unique rationale, merge or
  archive according to repository rules.
- If it partially replaces a note, retain and cross-link both.
- Delete the complete triplet for a rejected note only when it no longer
  prevents a realistic mistake.
- Never modify sealed notes under `archived/`.

### Validation

Run:

```bash
pnpm run verify-agent-note-classification
pnpm run verify-agent-note-format
pnpm run verify-translation-pairing .agents/notes/<lifecycle>/<class>/yyyy-mm-dd-topic.md
pnpm run doc-sync
git diff --check
```
