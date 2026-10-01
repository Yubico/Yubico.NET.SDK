// Maintains the single sticky metrics comment on a pull request.
//
// The comment is split into named sections, each owned by one workflow job, so a fast job
// can post its part long before a slow one finishes. A job replaces only its own section and
// keeps the others. Called from actions/github-script:
//
//   const { upsertSection } = require('./.github/scripts/report-comment.js');
//   await upsertSection({ github, context, section: 'complexity', body });

const MARKER = '<!-- yubikit-crap-report -->';

// Fixed display order, whatever order the jobs finish in.
const SECTIONS = ['complexity', 'crap'];

const start = (name) => `<!-- section:${name}:start -->`;
const end = (name) => `<!-- section:${name}:end -->`;

/**
 * Returns { name: content } for every known section found in a comment body. Markers only
 * count on a line of their own, so marker-like text inside a report (a table cell, say)
 * cannot split a section.
 */
function parseSections(body) {
  const lines = body.split('\n').map((line) => line.replace(/\r$/, ''));
  const sections = {};
  for (const name of SECTIONS) {
    const from = lines.indexOf(start(name));
    const to = from < 0 ? -1 : lines.indexOf(end(name), from + 1);
    if (from >= 0 && to > from) {
      sections[name] = lines.slice(from + 1, to).join('\n').trim();
    }
  }
  return sections;
}

/**
 * Builds the comment body with `section` set to `content`, keeping the other sections of
 * `existingBody`. A comment in the old single-report format has no section markers, so its
 * content is dropped rather than duplicated.
 */
function renderBody(existingBody, section, content) {
  if (!SECTIONS.includes(section)) {
    throw new Error(`unknown report section '${section}'`);
  }

  const sections = parseSections(existingBody ?? '');
  // crap.cs --markdown starts with the marker itself; it belongs to the comment, not a section.
  sections[section] = content.split('\n').filter((line) => line.trim() !== MARKER).join('\n').trim();

  const parts = [MARKER];
  for (const name of SECTIONS) {
    if (sections[name] !== undefined) {
      parts.push(`${start(name)}\n${sections[name]}\n${end(name)}`);
    }
  }
  return parts.join('\n\n') + '\n';
}

async function upsertSection({ github, context, section, body }) {
  // Paginate: listComments returns 100 per page, and on a long-running pull request the
  // bot's own comment can fall past the first page. Missing it would post a duplicate on
  // every push instead of updating in place.
  const comments = await github.paginate(github.rest.issues.listComments, {
    owner: context.repo.owner,
    repo: context.repo.repo,
    issue_number: context.issue.number,
    per_page: 100,
  });

  const existing = comments.find((c) => c.body?.startsWith(MARKER));
  const next = renderBody(existing?.body, section, body);

  if (existing) {
    await github.rest.issues.updateComment({
      owner: context.repo.owner,
      repo: context.repo.repo,
      comment_id: existing.id,
      body: next,
    });
  } else {
    await github.rest.issues.createComment({
      owner: context.repo.owner,
      repo: context.repo.repo,
      issue_number: context.issue.number,
      body: next,
    });
  }
}

module.exports = { upsertSection, renderBody, parseSections, MARKER };
