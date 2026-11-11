#!/usr/bin/env python3
"""Read-only baseline inventory. Writes only this run's inventory and coverage.json.

Pygments supplies lexical masking, not a C# compiler. Source ranges are disjoint
declaration-sized reading units. Snapshot reconciliation and source accessibility
are mechanical evidence; semantic binding and executable consumers have separate gates.
Run from the repository root with python -B.
"""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

from pygments.lexers import CSharpLexer
from pygments.token import Comment, String

ROOT = pathlib.Path.cwd()
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = RUN / 'inventory'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
MODULES = ['Oath', 'Management', 'Ctap2', 'WebAuthn', 'SecurityDomain', 'YubiOtp', 'YubiHsm', 'OpenPgp', 'Piv']


def git(*args):
    return subprocess.check_output(['git', *args], text=True).strip()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def emit(name, value):
    target = OUT / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def mask(text):
    chunks = []
    for _, token, value in CSharpLexer().get_tokens_unprocessed(text + '\n'):
        chunks.append(''.join('\n' if c == '\n' else ' ' for c in value)
                      if token in Comment or token in String else value)
    result = ''.join(chunks)[:len(text)]
    assert len(result) == len(text)
    return result


TYPE = re.compile(r'\b(?P<kind>record\s+(?:class\s+|struct\s+)?|class\s+|struct\s+|interface\s+|enum\s+|delegate\s+[\w.<>?,\[\]]+\s+)(?P<name>@?[A-Za-z_]\w*)')


def source_shape(path, text):
    """Find type containment and balanced declaration ranges without evaluating code."""
    clean = mask(text)
    pairs, stack, bad = {}, [], []
    for i, c in enumerate(clean):
        if c == '{':
            stack.append(i)
        elif c == '}':
            if stack:
                pairs[stack.pop()] = i
            else:
                bad.append(i)
    if stack or bad:
        raise ValueError(f'Unbalanced lexical braces: {path}: {stack}, {bad}')
    ns = re.search(r'\bnamespace\s+([\w.]+)', clean)
    namespace = ns.group(1) if ns else ''
    types = []
    for m in TYPE.finditer(clean):
        # A declaration keyword follows modifiers/start punctuation, never a generic constraint.
        start = max(clean.rfind('\n', 0, m.start()), clean.rfind('}', 0, m.start()), clean.rfind(';', 0, m.start())) + 1
        prefix = clean[start:m.start()]
        if ':' in prefix or re.search(r'\bwhere\b', prefix):
            continue
        paren = bracket = 0
        body = end = None
        for i in range(m.end(), len(clean)):
            c = clean[i]
            if c == '(': paren += 1
            if c == ')': paren -= 1
            if c == '[': bracket += 1
            if c == ']': bracket -= 1
            if paren == bracket == 0 and c in '{;':
                body = i if c == '{' else None
                end = pairs[i] + 1 if c == '{' else i + 1
                break
        if end is None:
            continue
        parents = [t for t in types if t['body'] is not None and t['body'] < m.start() < t['end']]
        parent = max(parents, key=lambda x: x['start']) if parents else None
        access = ('private protected' if re.search(r'\bprivate\s+protected\b', prefix) else
                  'protected internal' if re.search(r'\bprotected\s+internal\b', prefix) else
                  next((a for a in ['public', 'protected', 'internal', 'private', 'file'] if re.search(r'\b' + a + r'\b', prefix)),
                       'public' if parent and parent['kind'] == 'interface' else 'private' if parent else 'internal'))
        effective = access in ['public', 'protected', 'protected internal'] and (not parent or parent['effective_public'])
        name = (parent['name'] + '.' if parent else namespace + '.' if namespace else '') + m.group('name')
        kind = 'delegate' if m.group('kind').startswith('delegate') else m.group('kind').strip()
        types.append(dict(path=path, name=name, simple_name=m.group('name'), kind=kind,
                          start=start, declaration=m.start(), body=body, end=end, accessibility=access,
                          effective_public=effective, parent=parent['name'] if parent else None,
                          declaration_line=text.count('\n', 0, m.start()) + 1))
    # Split complete direct type members; nested type bodies recurse through their own spans.
    cuts = {0, len(text)}
    spans = []
    for t in types:
        if t['body'] is None:
            spans.append((t['start'], t['end'], 'type-contract', t['name']))
            cuts.update([t['start'], t['end']])
            continue
        cuts.update([t['start'], t['body'] + 1, t['end'] - 1, t['end']])
        spans.append((t['start'], t['body'] + 1, 'type-contract', t['name']))
        children = {x['start']: x for x in types if x['parent'] == t['name'] and t['start'] < x['start'] < t['end']}
        begin, i, paren, bracket = t['body'] + 1, t['body'] + 1, 0, 0
        while i < t['end'] - 1:
            if i in children:
                child = children[i]
                if begin < i: cuts.update([begin, i])
                begin = i = child['end']
                continue
            c = clean[i]
            if c == '(': paren += 1
            elif c == ')': paren -= 1
            elif c == '[': bracket += 1
            elif c == ']': bracket -= 1
            end = None
            if c == '{' and paren == bracket == 0:
                header = clean[begin:i]
                close = pairs[i] + 1
                # Initializer braces and expression-body lambdas belong to the semicolon declaration.
                nesting = 0
                initializer = False
                for position, char in enumerate(header):
                    if char in '([': nesting += 1
                    elif char in ')]': nesting -= 1
                    elif char == '=' and nesting == 0 and header[position:position + 2] != '=>' and header[max(0, position - 1):position] not in ['=', '!', '<', '>']:
                        initializer = True
                if (initializer or '=>' in header) and not re.search(r'\b(?:operator)\b', header):
                    i = close
                    continue
                end = close
                tail = close
                while tail < t['end'] - 1 and clean[tail].isspace(): tail += 1
                if tail < len(clean) and clean[tail] == ';': end = tail + 1
                if tail < len(clean) and clean[tail] == '=':
                    i = tail + 1
                    continue
            elif c == ';' and paren == bracket == 0:
                end = i + 1
            elif c == ',' and t['kind'] == 'enum' and paren == bracket == 0:
                end = i + 1
            if end is not None:
                raw = text[begin:end]
                code = clean[begin:end].strip()
                if code:
                    category = 'enum-value' if t['kind'] == 'enum' else 'member-contract-and-implementation'
                    label = re.sub(r'\s+', ' ', code.split('{', 1)[0])[:230]
                    spans.append((begin, end, category, t['name'] + ' :: ' + label))
                    cuts.update([begin, end])
                begin = i = end
                continue
            i += 1
        if begin < t['end'] - 1:
            cuts.update([begin, t['end'] - 1])
            if t['kind'] == 'enum' and clean[begin:t['end'] - 1].strip():
                label = re.sub(r'\s+', ' ', clean[begin:t['end'] - 1].strip())
                spans.append((begin, t['end'] - 1, 'enum-value', t['name'] + ' :: ' + label))
    ordered = sorted(cuts)
    units = []
    for start, end in zip(ordered, ordered[1:]):
        if start == end: continue
        matches = [s for s in spans if s[0] <= start and end <= s[1]]
        chosen = min(matches, key=lambda x: x[1] - x[0]) if matches else None
        units.append((start, end, chosen[2] if chosen else 'source-context-and-explanations', chosen[3] if chosen else path))
    return types, units


B_DEVICE_NAMES = set('DeviceConnectionRegistry DisposalGate HidConnectionSlot PcscConnectionSlot RegisteredConnections ConnectionInUseException UnrecoveredConnectionException YubiKeyConnectionExtensions'.split())
B_TEST_NAMES = set('ApplicationSessionScpTests ConnectionOwnershipContractTests DeviceConnectionOwnershipTests DeviceConnectionRegistryTests HeldExceptionPropagationTests HidConnectionSlotTests LinuxHidRoutingTests PcscConnectionSlotTests PcscDiscoveryLifetimeTests ResolvePreferredConnectionTests SessionTransportTests FidoHidOwnershipIntegrationTests'.split())


def family(path):
    name = pathlib.Path(path).stem
    if path.startswith('src/Core/'):
        if '/Devices/' in path:
            return 'B' if name in B_DEVICE_NAMES | B_TEST_NAMES else 'A'
        if any('/' + x + '/' in path for x in ['Cryptography', 'Credentials']): return 'C'
        if '/Utilities/' in path: return 'B' if name in ['ExchangeGuard', 'ExchangeGuardTests'] else 'C'
        if '/Abstractions/' in path: return 'A' if name in ['DeviceCorrelation', 'IDevice', 'IYubiKey'] else 'B'
        if '/Infrastructure/' in path: return 'A'
        if name in ['ProductAtrs', 'DeviceListenerStatus', 'Transport']: return 'A'
        if name in ['YubiKitLogging', 'CoreCredentialReaderAuditReproTests', 'CoreNumericBoundaryAuditReproTests']: return 'C'
        if path.endswith(('.md', '.csproj', '.json')) and '/BoundaryInventory/' not in path: return 'SHARED'
        return 'B'
    if path.startswith(('src/Tests.Shared/', 'src/Cli.Shared/', 'src/Cli.Commands/', 'src/PublicApi/')): return 'EXTERNAL'
    for module in MODULES:
        if path.startswith('src/' + module + '/'): return 'BOUNDARY-' + module
    if path.startswith(('verification/', 'benchmarks/')): return 'B'
    return 'SHARED'


def stage(path, owner):
    if owner != 'B': return owner
    name = pathlib.Path(path).stem
    if '/Protocols/SmartCard/' in path or '/SmartCard/Scp/' in path or name in ['CoreScpAuditReproTests']:
        return 'B6-wire-scp'
    if '/Protocols/' in path or '/Sessions/' in path or '/Abstractions/' in path or '/Utilities/' in path:
        return 'B5-session-framing'
    if '/Linux/' in path or '/LinuxHidRouteVerification/' in path or pathlib.Path(path).name.startswith('Linux'):
        return 'B2-linux-lifetimes'
    if '/MacOS/' in path or '/MacOSHid' in path or pathlib.Path(path).name.startswith(('MacOS', 'CoreFoundation')):
        return 'B3-macos-lifetimes'
    if '/Windows/' in path or pathlib.Path(path).name.startswith(('Windows', 'CfgMgr')):
        return 'B4-windows-support'
    if '/SmartCard/' in path or '/Devices/' in path:
        return 'B1-portable-lifetimes'
    return 'B4-windows-support'


def md_sections(text):
    lines = text.splitlines(keepends=True)
    offsets, offset, fence = [], 0, None
    heads = [(0, '(preamble)')]
    for line in lines:
        offsets.append(offset)
        stripped = line.lstrip()
        fm = re.match(r'(`{3,}|~{3,})', stripped)
        if fm:
            if fence is None: fence = fm.group(1)[0]
            elif fm.group(1)[0] == fence: fence = None
        if fence is None and re.match(r'#{1,6}\s', line): heads.append((offset, line.strip()))
        offset += len(line)
    heads = sorted(dict(heads).items())
    return [(a, b, 'narrative-section-with-complete-examples', h)
            for (a, h), (b, _) in zip(heads, heads[1:] + [(len(text), '')]) if b > a]


def map_sections(text):
    heads = [(0, '(map preamble and schema)')]
    offset = 0
    for line in text.splitlines(keepends=True):
        if re.match(r'  - id:|  L[0-9][\w-]*:', line): heads.append((offset, line.strip()))
        offset += len(line)
    heads = sorted(dict(heads).items())
    return [(a, b, 'structured-map-entry', h) for (a, h), (b, _) in zip(heads, heads[1:] + [(len(text), '')]) if b > a]


observed_head = git('rev-parse', 'HEAD')
assert subprocess.run(['git', 'merge-base', '--is-ancestor', PIN, observed_head], check=False).returncode == 0
tracked = git('ls-files').splitlines()
pin_paths = set(git('ls-tree', '-r', '--name-only', PIN).splitlines())
core = git('ls-tree', '-r', '--name-only', PIN, 'src/Core').splitlines()
assert len(core) == 515
alltypes, shapes = [], {}
for path in core:
    actual = (ROOT / path).read_bytes().replace(b'\r\n', b'\n')
    pinned = subprocess.check_output(['git', 'show', PIN + ':' + path]).replace(b'\r\n', b'\n')
    assert actual == pinned, ('Core source differs from approved pin', path)
    if path.endswith('.cs'):
        types, ranges = source_shape(path, (ROOT / path).read_bytes().decode('utf-8-sig'))
        shapes[path] = ranges
        if path.startswith('src/Core/src/'): alltypes.extend(types)

public_types = [t for t in alltypes if t['effective_public']]
type_names = {t['name'] for t in public_types}
snapshot_types, entries = set(), []
for path in ['src/Core/src/PublicAPI.Shipped.txt', 'src/Core/src/PublicAPI.Unshipped.txt']:
    for number, line in enumerate((ROOT / path).read_text().splitlines(), 1):
        if not line.strip() or line.startswith('#'): continue
        match = re.fullmatch(r'Yubico\.YubiKit\.Core\.[\w.]+(?:<[^>]+>)?', line)
        if match: snapshot_types.add(line)
        entries.append(dict(path=path, line=number, signature=line))
for e in entries:
    candidates = [n for n in snapshot_types if re.search(r'(?<![\w.])' + re.escape(n) + r'(?=[.(< ]|$)', e['signature'])]
    # The first snapshot owner before parameters/return wins; parameter types are context only.
    front = e['signature'].split('(', 1)[0].split(' -> ', 1)[0]
    candidates = [n for n in candidates if n in front]
    e['owner_type'] = max(candidates, key=len) if candidates else None
    source = next((t for t in public_types if t['name'] == e['owner_type']), None)
    e['source_path'] = source['path'] if source else None
    e['assigned_family'] = family(source['path']) if source else 'RECONCILE'
    sig = e['signature']
    e['entry_kind'] = ('type' if sig in snapshot_types else 'accessor' if re.search(r'\.(get|set|init)\b', sig) else
                       'enum-or-constant' if ' = ' in sig.split(' -> ')[0] and '(' not in sig.split(' -> ')[0] else 'callable')
    e['record_boilerplate_candidate'] = bool(source and source['kind'].startswith('record') and re.search(r'<Clone>\$|\b(?:Equals|GetHashCode|ToString|PrintMembers|Deconstruct|EqualityContract)\b|operator [!=]=', sig))
    e['status'] = 'UNREAD'
    e['compiler_shape_status'] = 'VALID-NOT-CHECKED'
api_inventory = dict(baseline=PIN, snapshot_entries=len(entries), snapshot_types=len(snapshot_types),
     effective_source_type_declarations=len(public_types), effective_source_families=len(type_names),
     snapshot_only_types=sorted(snapshot_types - type_names), source_only_types=sorted(type_names - snapshot_types),
     duplicate_entries=[s for s, n in collections.Counter(e['signature'] for e in entries).items() if n > 1],
     kind_counts=dict(collections.Counter(e['entry_kind'] for e in entries)),
     qualification='Lexical source containment plus checked-in analyzer snapshot; compiler/metadata binding is a later gate. Record boilerplate candidates include authored overrides: inspect source before exclusion.',
     source_types=alltypes, entries=entries)

# Select bounded external projects and active documents. Frozen archives are inputs only.
whole_prefixes = ['verification/', 'benchmarks/', 'src/Tests.Shared/', 'src/Cli.Shared/', 'src/PublicApi/',
                  'docs/usage/', 'docs/architecture/', 'docs/migration/', 'scripts/architecture/']
whole_docs = ['README.md', 'PACKAGE_README.md', 'AGENTS.md', 'CLAUDE.md', 'TOOLCHAIN.md',
              'docs/SDK-HOUSE-STYLE.md', 'docs/LOGGING.md', 'docs/MEMORY-MANAGEMENT.md', 'docs/CRYPTO-APIS.md',
              'docs/CSHARP-PATTERNS.md', 'docs/NATIVE-AOT.md', 'docs/linux-setup.md', 'docs/DEV-GUIDE.md',
              'docs/TESTING.md', 'docs/TESTING_PLATFORM_FINDINGS.md', 'docs/v1-to-v2-comparison.md', 'docs/v2-highlights.md',
              'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'toolchain.cs',
              '.github/workflows/build.yml', '.github/workflows/native-aot.yml', 'nuget.config']
history_prefixes = ['docs/plans/yubikit-async-boundaries/']
external = [p for p in tracked if p in whole_docs or any(p.startswith(x) for x in whole_prefixes)]
external += [p for p in tracked if p.startswith('docs/research/') and p.count('/') == 2 and
             any(x in pathlib.Path(p).name for x in ['linux-', 'native-aot'])]
external += [p for p in tracked if any(p.startswith(x) for x in history_prefixes) or p == '2026-09-21-yubikit-async-boundaries-ISA.md']
external += [p for p in tracked if p.startswith('scripts/toolchain/') and p.endswith('.cs')]
boundary = [p for p in tracked if any(p.startswith('src/' + m + '/') for m in MODULES) and
            (p.endswith(('.cs', '.md', '.csproj')) or pathlib.Path(p).name.startswith('PublicAPI.'))]
external += boundary
external += [p for p in tracked if p.startswith('src/Cli.Commands/')]
external += ['docs/research/documentation-audit/core.md']
paths = sorted(set(core + external))

core_token_names = {t['simple_name'] for t in alltypes if len(t['simple_name']) > 4}
call = re.compile(r'\b(?:' + '|'.join(sorted(map(re.escape, core_token_names), key=len, reverse=True)) +
                  r')\b|\.(?:Create\w*SessionAsync|ConnectAsync|FindAllAsync|FindFirstAsync|FindFirstOrDefaultAsync|WatchAsync|PrepareProtocolAsync|ConfigureProtocolAsync|OwnConnection|ConstructInitializedAsync|InitializeSessionAsync|EnsureSupports|IsSupported|CreateUserPresenceNotification|TransmitAndReceiveAsync|SendAndReceiveAsync|SendCommandAsync|SendAsync|ReceiveAsync|SelectAsync|InitializeScpAsync|DisposeAsync|Dispose|Clear|DecodeList|EncodeList|EncodeAndDisposeList|TryFindValue)\b')
core_doc = re.compile(r'Core|YubiKeyManager|IYubiKey|[Mm]onitor|[Dd]iscover|[Cc]onnection|[Tt]ransport|[Oo]wnership|[Cc]ancel|SCP|PC/SC|[Nn]ative|[Ll]ogging|[Cc]redential|[Cc]ryptograph|[Tt][Ll][Vv]|Base32|APDU|HID|AOT|[Ss]ession|[Pp]rompt|[Ff]irmware|[Vv]ersion|[Mm]emory|[Bb]uffer')
files, units, references, fences, markers, parser_limits = [], [], [], [], [], []
for path in paths:
    data = (ROOT / path).read_bytes()
    is_core = path in core
    owner = family(path)
    text = data.decode('utf-8-sig', errors='replace')
    binary = b'\x00' in data or path.endswith(('.png',))
    lines = len(text.splitlines())
    at_pin = path in pin_paths
    baseline_blob = git('rev-parse', PIN + ':' + path) if at_pin else None
    matches_pin = sha(subprocess.check_output(['git', 'show', PIN + ':' + path])) == sha(data) if at_pin else None
    # Git-normalized CRLF is explicitly reconciled rather than treated as a source edit.
    if at_pin and not matches_pin:
        normalized = data.replace(b'\r\n', b'\n')
        if sha(subprocess.check_output(['git', 'show', PIN + ':' + path])) == sha(normalized): matches_pin = 'LF-normalized-equal'
    historical = any(path.startswith(x) for x in history_prefixes) or path == '2026-09-21-yubikit-async-boundaries-ISA.md' or pathlib.Path(path).name.startswith('native-aot-readiness')
    scoped = path in boundary or path.startswith('src/Cli.Commands/')
    file_record = dict(path=path, sha256=sha(data), git_blob=baseline_blob, current_matches_baseline=matches_pin,
                       bytes=len(data), lines=lines, scope='CORE' if is_core else 'EXTERNAL-BOUNDARY' if scoped else 'EXTERNAL',
                       assigned_family=owner, stage=stage(path, owner),
                       future_editor_owner='SHARED' if path.endswith(('.md', '.yml', '.svg', '.png')) and not scoped else owner,
                       disposition='historical-design-or-evidence-context' if historical else 'derived-rendering' if binary or path.endswith('.svg') else
                       'api-snapshot-reconciliation' if 'PublicAPI.' in path else 'whole-module' if is_core else
                       'Core-boundary-sections-only' if scoped else 'bounded-support-project-or-shared-sections',
                       rationale='Complete Core tracked file population.' if is_core else
                       'Present-tense Core claims and callers; other applet behavior retains its completed module audit.' if scoped else
                       'Pinned design history informs provenance; current contracts are checked in active source.' if historical else
                       'Core-facing native/platform, caller, packaging, verification or shared guidance dependency.',
                       semantic_status='UNREAD', unit_ids=[])
    if path.endswith('.cs') and not mask(text).strip():
        file_record['disposition'] = 'commented-out-historical-source'
        file_record['rationale'] = 'Tracked dormant source consists of comments. Retain its substantive claims for historical/applicability triage; it contributes no declared public API.'
    files.append(file_record)
    if binary:
        ranges = [(0, len(text), 'derived-diagram', path)]
    elif path.endswith('.cs'):
        if path in shapes: ranges = shapes[path]
        else:
            try:
                _, ranges = source_shape(path, text)
            except ValueError as ex:
                parser_limits.append(dict(path=path, issue=str(ex), disposition='Whole-file reading unit retains every byte; refine with Roslyn before clean sampling.'))
                ranges = [(0, len(text), 'whole-source-context-parser-limit', path)]
    elif path.endswith('.md'):
        ranges = md_sections(text)
    elif path in ['docs/migration/v1-to-v2-map.yml', 'docs/architecture/sdk-architecture-map.yml']:
        ranges = map_sections(text)
    else:
        ranges = [(0, len(text), 'configuration-data-or-support-contract', path)]
    for start, end, category, label in ranges:
        body = text[start:end]
        code_body = mask(body) if path.endswith('.cs') else body
        scoped_caller = scoped or path.startswith('src/Cli.Shared/')
        relevant = True if is_core or not scoped_caller else bool(call.search(code_body) if path.endswith('.cs') else core_doc.search(body))
        if path.startswith('src/Cli.Shared/') and any(x in path for x in ['SecureCredential', 'PinPrompt', 'ConsoleUserPresencePrompt']): relevant = True
        # Shared top-level documents and applet-specific architecture include only named Core-facing sections.
        if not is_core and not scoped and path.endswith('.md') and path not in ['docs/SDK-HOUSE-STYLE.md', 'docs/LOGGING.md', 'docs/MEMORY-MANAGEMENT.md', 'docs/CRYPTO-APIS.md', 'docs/CSHARP-PATTERNS.md', 'docs/NATIVE-AOT.md', 'docs/linux-setup.md']:
            relevant = bool(core_doc.search(body))
        if path == 'docs/migration/v1-to-v2-map.yml' and start:
            relevant = bool(core_doc.search(body))
        unit_id = 'core-001:unit:' + sha((path + '|' + str(start) + '|' + str(end)).encode())[:16]
        unit = dict(id=unit_id, path=path, source_sha256=sha(data), range_sha256=sha(body.encode()),
                    start_character=start, end_character_exclusive=end, first_line=text.count('\n', 0, start) + 1,
                    last_line=text.count('\n', 0, end - 1) + 1 if end else 1, category=category, label=label,
                    assigned_family=owner, stage=stage(path, owner), status='UNREAD' if relevant else 'OUT-OF-SCOPE',
                    review_status='UNASSIGNED', sample_status='INELIGIBLE-UNTIL-READ',
                    scope_rationale='Complete source/XML/comment/assertion context; declaration ranges include code-only implicit contracts.' if is_core else
                    'Read complete Core-facing unit and neighboring lifetime/cleanup context.' if relevant else
                    'Module-specific behavior or administrative text outside the Core boundary; reopen on a dependency finding.',
                    features=dict(xml='///' in body, comments=bool(re.search(r'//|/\*', body)),
                                  todo=bool(re.search(r'\bTODO\b|\bFIXME\b|\bHACK\b', body)),
                                  specification=bool(re.search(r'RFC\s?\d|ISO\s?\d|https?://|\bNIST\b|\bGlobalPlatform\b|\bPC/SC\b|CTAPHID', body)),
                                  code_only=bool(path.endswith('.cs') and not re.search(r'//|/\*', body)),
                                  tests=bool(re.search(r'\[(?:Fact|Theory|SkippableFact|SkippableTheory)', body))))
        if not body.strip() or (path.endswith('.cs') and not re.sub(r'[{};\s]', '', code_body) and '///' not in body and not re.search(r'//(?!\s*(?:Copyright|Licensed|You may|You can|http|Unless|distributed|WITHOUT|See the|limitations|$))|/\*', body)):
            unit['status'] = 'ADMINISTRATIVE-CONTEXT'
            unit['scope_rationale'] = 'Whitespace, structural punctuation or license context; substantive neighboring declarations retain their own units.'
        units.append(unit)
        file_record['unit_ids'].append(unit_id)
        for line_num, line in enumerate(body.splitlines(), unit['first_line']):
            if re.search(r'\bTODO\b|\bFIXME\b|\bHACK\b', line): markers.append(dict(unit=unit_id, path=path, line=line_num, text=line.strip(), status=unit['status'], assigned_family=owner))
            if re.search(r'https?://|RFC\s?\d|ISO\s?\d|\bNIST\b|\bGlobalPlatform\b|CTAPHID', line): references.append(dict(unit=unit_id, path=path, line=line_num, text=line.strip(), status=unit['status'], authority_status='UNVERIFIED', assigned_family=owner))
    if path.endswith('.md'):
        active = None
        for line_num, line in enumerate(text.splitlines(keepends=True), 1):
            fm = re.match(r'\s*(`{3,}|~{3,})(.*)', line)
            if fm and active is None:
                active = dict(path=path, first_line=line_num, language=fm.group(2).strip(), marker=fm.group(1)[0], body=[])
            elif fm and active and fm.group(1)[0] == active['marker']:
                body = ''.join(active.pop('body'))
                active.pop('marker')
                active.update(last_line=line_num, body_sha256=sha(body.encode()), classification='UNCLASSIFIED',
                              compiler_status='VALID-NOT-CHECKED', status='UNREAD', assigned_family=owner)
                fences.append(active)
                active = None
            elif active: active['body'].append(line)
        assert active is None, ('Unclosed fence', path)

emit('files.json', files)
emit('semantic-units.json', units)
emit('specification-candidates.json', references)
emit('unfinished-markers.json', markers)
emit('example-bodies.json', fences)
emit('parser-limits.json', parser_limits)
for entry in entries:
    declaration_paths = {t['path'] for t in public_types if t['name'] == entry['owner_type']}
    entry['type_declaration_paths'] = sorted(declaration_paths)
    candidates = [u for u in units if u['path'] in declaration_paths and
                  entry['owner_type'] in u['label'] and u['category'] in ['type-contract', 'member-contract-and-implementation', 'enum-value']]
    suffix = entry['signature'].split(entry['owner_type'], 1)[1] if entry['owner_type'] else ''
    member = suffix.lstrip('.').split('(', 1)[0].split(' -> ', 1)[0].split(' = ', 1)[0].split('.', 1)[0].split('<', 1)[0].split('[', 1)[0].lstrip('~').strip()
    if member.startswith('operator'): member = 'operator'
    if not suffix:
        selected = [u for u in candidates if u['category'] == 'type-contract']
    else:
        selected = [u for u in candidates if u['category'] != 'type-contract' and re.search(r'\b' + re.escape(member) + r'\b', u['label'])]
        if not selected:
            for unit in candidates:
                if ' :: extension(' in unit['label'] or ' :: extension (' in unit['label']:
                    source_text = (ROOT / unit['path']).read_bytes().decode('utf-8-sig')
                    body = source_text[unit['start_character']:unit['end_character_exclusive']]
                    if re.search(r'\b' + re.escape(member) + r'\b', mask(body)): selected.append(unit)
    entry['source_unit_candidates'] = [u['id'] for u in selected]
    source_type = next((t for t in public_types if t['name'] == entry['owner_type']), None)
    entry['documentation_projection'] = ('type-contract' if not suffix else
        'authored-declaration-or-property-accessor' if selected else
        'delegate-call-signature-from-type-declaration' if source_type and source_type['kind'] == 'delegate' else
        'record-synthesis-or-positional-contract-review' if source_type and source_type['kind'].startswith('record') else
        'primary-or-implicit-constructor-from-type-declaration' if source_type and member == source_type['simple_name'] else
        'unmatched-declaration-review')
    if not selected:
        entry['source_unit_candidates'] = [u['id'] for u in candidates if u['category'] == 'type-contract']
    else:
        entry['source_path'] = selected[0]['path']
    entry['mapping_precision'] = 'Source declaration group; overload and compiler-generated symbol binding remain VALID-NOT-CHECKED.'
api_inventory['projection_counts'] = dict(collections.Counter(e['documentation_projection'] for e in entries))
emit('public-api.json', api_inventory)
shared_sections = []
for unit in units:
    if unit['assigned_family'] not in ['SHARED', 'EXTERNAL'] or unit['status'] != 'UNREAD': continue
    key = unit['path'] + ' ' + unit['label']
    reader = ('C' if re.search(r'Crypt|crypt|[Cc]redential|[Pp]rompt|PinPrompt|SecureCredential|[Ll]ogging|[Mm]emory|Base32|[Tt][Ll][Vv]|[Bb]uffer|[Ee]ncod', key) else
              'A' if re.search(r'[Dd]iscover|[Dd]evice|[Mm]onitor|[Ii]dentity|[Ss]napshot|[Mm]etadata|[Ff]orm[Ff]actor|[Ff]ilter', key) else 'B')
    shared_sections.append(dict(unit_id=unit['id'], path=unit['path'], first_line=unit['first_line'], last_line=unit['last_line'],
        label=unit['label'], source_sha256=unit['source_sha256'], investigation_owner=reader,
        investigation_stage=('B6-wire-scp' if re.search(r'SCP|Scp|APDU|Apdu|[Ss]ecure [Cc]hannel', key) else
            'B2-linux-lifetimes' if re.search(r'Linux|linux', key) else
            'B3-macos-lifetimes' if re.search(r'MacOS|macOS|MacOSHid', key) else
            'B1-portable-lifetimes' if re.search(r'PC/SC|PCSC|Pcsc|[Ss]haring|[Cc]onnection|[Oo]wnership', key) else
            'B5-session-framing' if re.search(r'[Ss]ession|[Rr]aw|HID|Hid', key) else 'B4-windows-support') if reader == 'B' else reader,
        future_editor_owner='SHARED', status='UNREAD'))
emit('shared-sections.json', shared_sections)
project_dispositions = []
prefixes = ['src/Core/', 'src/Tests.Shared/', 'src/Cli.Shared/', 'src/Cli.Commands/', 'src/PublicApi/',
            'benchmarks/Yubico.YubiKit.PerformanceBenchmarks/']
prefixes += ['src/' + module + '/' for module in MODULES]
prefixes += sorted({p.rsplit('/', 1)[0] + '/' for p in tracked if p.startswith('verification/') and p.endswith('.csproj')})
file_by_path = {f['path']: f for f in files}
for prefix in prefixes:
    population = [p for p in tracked if p.startswith(prefix)]
    included = [p for p in population if p in file_by_path]
    excluded = [p for p in population if p not in file_by_path]
    project_dispositions.append(dict(prefix=prefix, tracked_files=len(population), indexed_files=len(included),
        indexed_paths=included, excluded_paths=excluded,
        disposition='whole-Core' if prefix == 'src/Core/' else 'Core-boundary-only' if prefix.startswith('src/') and any(prefix == 'src/' + m + '/' for m in MODULES) else 'bounded-support-project',
        rationale='All tracked Core files receive an exclusive disposition.' if prefix == 'src/Core/' else
        'Read indexed Core-facing declaration/guide units with complete resource and assertion context. Other applet behavior retains its module audit; omitted runner/configuration files carry no selected Core caller claims.' if any(prefix == 'src/' + m + '/' for m in MODULES) else
        'Review Core-facing usage, native ownership, evidence or compiler/gate explanations within this named support project. Every indexed unit retains a range-level disposition.'))
emit('project-dispositions.json', project_dispositions)
packets = {}
for owner in sorted({f['assigned_family'] for f in files}):
    fs = [f for f in files if f['assigned_family'] == owner]
    active_paths = {u['path'] for u in units if u['assigned_family'] == owner and u['status'] == 'UNREAD'}
    packets[owner] = dict(paths=[f['path'] for f in fs if f['path'] in active_paths], disposition_paths=[f['path'] for f in fs],
                          file_count=len(fs), lines=sum(f['lines'] for f in fs),
                          selected_characters=sum(u['end_character_exclusive'] - u['start_character'] for u in units if u['assigned_family'] == owner and u['status'] == 'UNREAD'),
                          core_paths=[f['path'] for f in fs if f['scope'] == 'CORE'],
                          shared_read_unit_ids=[s['unit_id'] for s in shared_sections if s['investigation_owner'] == owner],
                          unit_ids=[u['id'] for u in units if u['assigned_family'] == owner and u['status'] == 'UNREAD'])
    emit('packets/' + owner + '.json', packets[owner])
for s in sorted({f['stage'] for f in files if f['assigned_family'] == 'B'}):
    fs = [f for f in files if f['stage'] == s]
    emit('packets/' + s + '.json', dict(paths=[f['path'] for f in fs], file_count=len(fs), lines=sum(f['lines'] for f in fs),
         shared_read_unit_ids=[x['unit_id'] for x in shared_sections if x['investigation_stage'] == s],
         unit_ids=[u['id'] for u in units if u['stage'] == s and u['status'] == 'UNREAD']))
summary = dict(baseline=PIN, observed_head=observed_head, branch=git('branch', '--show-current'), core_files=len(core),
               core_csharp=sum(p.endswith('.cs') for p in core),
               shipping_subtree_files=sum(p.startswith('src/Core/src/') for p in core),
               shipping_csharp=sum(p.startswith('src/Core/src/') and p.endswith('.cs') for p in core),
               native_csharp=sum(p.startswith('src/Core/src/Native/') and p.endswith('.cs') for p in core),
               core_test_subtree_files=sum(p.startswith('src/Core/tests/') for p in core),
               test_csharp=sum(p.startswith('src/Core/tests/') and p.endswith('.cs') for p in core),
               external_files=len(files)-len(core), snapshot_entries=len(entries), snapshot_types=len(snapshot_types),
               effective_source_families=len(type_names), semantic_units=len(units),
               status_counts=dict(collections.Counter(u['status'] for u in units)),
               category_counts=dict(collections.Counter(u['category'] for u in units if u['status']=='UNREAD')),
               example_bodies=len(fences), specification_candidate_lines=len(references), unfinished_marker_lines=len(markers),
               family_counts={k:{'files':v['file_count'],'lines':v['lines'],'unread_units':len(v['unit_ids']),'core_files':len(v['core_paths'])} for k,v in packets.items()},
               qualification='Inventory is a reading/dispatch denominator. All substantive units begin UNREAD. Character ranges partition files; line ranges can share a boundary line. Compiler binding, runtime behavior and original citations remain unchecked.')
emit('summary.json', summary)
(RUN / 'coverage.json').write_text(json.dumps(dict(schema='core-001-coverage-v1', **summary,
     files='inventory/files.json', units='inventory/semantic-units.json', api='inventory/public-api.json',
     examples='inventory/example-bodies.json', citations='inventory/specification-candidates.json',
     markers='inventory/unfinished-markers.json', packet_pathsets='inventory/packets/',
     documentation_status='INVENTORIED-UNREAD', implementation_status='DEFERRED-PASS-2',
     hardware_status='NOT-RUN', compiler_shape_status='VALID-NOT-CHECKED'), indent=2) + '\n')
print(json.dumps(summary, indent=2))
