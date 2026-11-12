"""Conservative native preprocessing-token guard; ambiguous edited sources require compiler review."""
import json
from pathlib import Path
import re
from foundation import ROOT, BASELINE, git, require, sha

PUNCTUATORS = sorted(['%:%:', '>>=', '<<=', '<=>', '->*', '...', '##', '::', '.*', '->', '++', '--',
                      '<<', '>>', '<=', '>=', '==', '!=', '&&', '||', '*=', '/=', '%=', '+=', '-=',
                      '&=', '^=', '|=', '<:', ':>', '<%', '%>', '%:'], key=len, reverse=True)


def shape(text):
    # Translation-phase splicing/trigraphs deserve the platform compiler rather than a guessed rule.
    require('\\\n' not in text and '\\\r\n' not in text and '??' not in text,
            'Edited native source uses splicing/trigraphs; assign an exact compiler-token/directive proof')
    result, offset, line_start = [], 0, True
    while offset < len(text):
        if text[offset].isspace():
            line_start = line_start or text[offset] in '\r\n'
            offset += 1
            continue
        if text.startswith('//', offset):
            end = text.find('\n', offset)
            offset = len(text) if end < 0 else end
            continue
        if text.startswith('/*', offset):
            end = text.find('*/', offset + 2)
            require(end >= 0, 'Unterminated native comment')
            line_start = line_start or '\n' in text[offset:end + 2]
            offset = end + 2
            continue
        require(not text.startswith('%:', offset), 'Native directive digraph requires an assigned compiler proof')
        if text[offset] == '#' and line_start:
            end = text.find('\n', offset)
            end = len(text) if end < 0 else end
            result.append(('directive', len(result), text[offset:end]))
            offset = end
            continue
        raw = re.match(r'(?:u8|u|U|L)?R"([^\s()\\]{0,16})\(', text[offset:])
        quoted = re.match(r'(?:u8|u|U|L)?(["\'])', text[offset:])
        if raw:
            terminal = ')' + raw.group(1) + '"'
            end = text.find(terminal, offset + raw.end())
            require(end >= 0, 'Unterminated C++ raw string')
            end += len(terminal)
        elif quoted:
            delimiter = quoted.group(1)
            end = offset + quoted.end()
            while end < len(text) and text[end] != delimiter:
                require(text[end] not in '\r\n', 'Unterminated native string/character literal')
                end += 2 if text[end] == '\\' else 1
            require(end < len(text), 'Unterminated native literal')
            end += 1
        else:
            atom = re.match(r'(?:[A-Za-z_]\w*|(?:\d|\.\d)(?:[eEpP][+-]|[\w.\'])*)', text[offset:])
            punctuator = next((value for value in PUNCTUATORS if text.startswith(value, offset)), None)
            if atom:
                end = offset + atom.end()
            elif punctuator:
                end = offset + len(punctuator)
            else:
                require(text[offset] in '{}[]()#;:?.~!%^&*+-=/<>|,', f'Unclassified native token near {offset}')
                end = offset + 1
        if raw or quoted:
            suffix = re.match(r'[A-Za-z_]\w*', text[end:])
            if suffix:
                end += suffix.end()  # User-defined literal suffix adjacency changes the token.
        result.append(('token', text[offset:end]))
        line_start = False
        offset = end
    return result


def selected(path):
    return not path.startswith(('docs/research/', 'docs/archive/', 'docs/completed/', '.claude/')) and Path(path).suffix.lower() in {
        '.c', '.cc', '.cpp', '.cxx', '.h', '.hh', '.hpp', '.hxx'}


def verify():
    before = sorted(path for path in git('ls-tree', '-r', '--name-only', '-z', BASELINE).decode().split('\0') if selected(path))
    current = sorted({path for path in git('ls-files', '--cached', '--others', '--exclude-standard', '-z').decode().split('\0') if selected(path)})
    require(before == current, 'Native-source membership changed; assign the added/removed source explicitly')
    rows = []
    for path in before:
        old = git('show', BASELINE + ':' + path)
        source = ROOT / path
        require(source.is_file() and not source.is_symlink(), f'Redirected/missing native source: {path}')
        new = source.read_bytes()
        if old != new:
            require(shape(old.decode()) == shape(new.decode()), f'Native tokens/directives changed: {path}')
        rows.append(dict(path=path, raw_identical=old == new, baseline_sha256=sha(old), current_sha256=sha(new)))
    return dict(source_baseline=BASELINE, files=rows,
                status='tokens-preserved' if rows else 'not-applicable-no-current-native-c-cpp-source',
                limits='Conservative preprocessing-token identity; edited splice/trigraph sources require an assigned compiler proof.')


if __name__ == '__main__':
    print(json.dumps(verify(), indent=2))
