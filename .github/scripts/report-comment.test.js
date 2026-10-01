// Tests report-comment.js against a fake GitHub client: node .github/scripts/report-comment.test.js
const assert = require('node:assert/strict');
const path = require('node:path');
const { upsertSection, parseSections, MARKER } = require(path.join(__dirname, 'report-comment.js'));

function fakeGitHub(initial) {
  const comments = initial.map((body, i) => ({ id: i + 1, body }));
  const calls = [];
  const github = {
    paginate: async (fn, args) => { calls.push(['list', args.per_page]); return comments; },
    rest: {
      issues: {
        listComments: () => {},
        updateComment: async ({ comment_id, body }) => {
          calls.push(['update', comment_id]);
          comments.find((c) => c.id === comment_id).body = body;
        },
        createComment: async ({ body }) => {
          calls.push(['create']);
          comments.push({ id: comments.length + 1, body });
        },
      },
    },
  };
  return { github, comments, calls };
}

const context = { repo: { owner: 'o', repo: 'r' }, issue: { number: 7 } };
const complexity = '### Complexity\n\nNo changed method exceeds a threshold (3 methods checked).';
const crap = `${MARKER}\n### Coverage and CRAP\n\n| module | methods |\n|---|---:|\n| Core | 1 |`;

(async () => {
  // 1. Complexity finishes first on a fresh pull request: one comment is created.
  const a = fakeGitHub(['unrelated human comment']);
  await upsertSection({ github: a.github, context, section: 'complexity', body: complexity });
  assert.deepEqual(a.calls.map((c) => c[0]), ['list', 'create']);
  let body = a.comments[1].body;
  assert.ok(body.startsWith(MARKER), 'comment starts with the marker');
  assert.ok(body.includes(complexity));
  assert.ok(!body.includes('section:crap'), 'no crap section yet');

  // 2. CRAP arrives later: the same comment is updated, complexity is kept and stays first,
  //    and the marker line from crap.cs is not duplicated.
  await upsertSection({ github: a.github, context, section: 'crap', body: crap });
  assert.deepEqual(a.calls.map((c) => c[0]), ['list', 'create', 'list', 'update']);
  body = a.comments[1].body;
  assert.equal(body.split(MARKER).length - 1, 1, 'marker appears once');
  assert.ok(body.indexOf('### Complexity') < body.indexOf('### Coverage and CRAP'), 'complexity first');

  // 3. A new push re-runs complexity: only its section changes.
  const updated = '### Complexity\n\n**1 method needs action** (new or worse).';
  await upsertSection({ github: a.github, context, section: 'complexity', body: updated });
  body = a.comments[1].body;
  assert.ok(body.includes(updated) && !body.includes('3 methods checked'), 'complexity replaced');
  assert.ok(body.includes('| Core | 1 |'), 'crap kept');
  assert.equal(a.comments.length, 2, 'still one bot comment');

  // 4. Order is fixed even when crap is written first.
  const b = fakeGitHub([]);
  await upsertSection({ github: b.github, context, section: 'crap', body: crap });
  await upsertSection({ github: b.github, context, section: 'complexity', body: complexity });
  body = b.comments[0].body;
  assert.ok(body.indexOf('### Complexity') < body.indexOf('### Coverage and CRAP'), 'fixed order');

  // 5. A comment in the old single-report format is replaced, not appended to.
  const c = fakeGitHub([`${MARKER}\n### Coverage and CRAP\n\nold table`]);
  await upsertSection({ github: c.github, context, section: 'complexity', body: complexity });
  body = c.comments[0].body;
  assert.ok(!body.includes('old table'), 'legacy content dropped');
  assert.deepEqual(c.calls.map((x) => x[0]), ['list', 'update']);

  // 6. Marker-like text inside a table cell does not split a section.
  const tricky = '### Complexity\n\n| 12 | 3 | `src/X/src/<!-- section:crap:start -->.cs` |';
  const d = fakeGitHub([]);
  await upsertSection({ github: d.github, context, section: 'crap', body: crap });
  await upsertSection({ github: d.github, context, section: 'complexity', body: tricky });
  await upsertSection({ github: d.github, context, section: 'complexity', body: tricky });
  body = d.comments[0].body;
  const parsed = parseSections(body);
  assert.equal(parsed.crap, crap.replace(`${MARKER}\n`, ''), 'crap section exactly intact');
  assert.equal(parsed.complexity, tricky, 'complexity section exactly intact');
  assert.equal(body.split('<!-- section:crap:start -->').length - 1, 2, 'one real marker plus the cell text');

  // 7. Unknown section names are a programming error.
  await assert.rejects(upsertSection({ github: c.github, context, section: 'nope', body: 'x' }));

  console.log('report-comment: 7 scenarios passed');
})().catch((e) => { console.error(e); process.exit(1); });
