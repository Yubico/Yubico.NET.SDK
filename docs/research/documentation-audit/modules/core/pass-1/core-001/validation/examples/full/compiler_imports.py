"""Wrapper import composition only. Accepted directives are a byte-preserved ordered segment."""
import re

DIRECTIVE=re.compile(r'^[ \t]*using[ \t]+(?:(static)[ \t]+)?([A-Za-z0-9_.]+);[ \t]*$')
INVALID_WRAPPER_NAMESPACE='Yubico.YubiKit.Ctap2.Ctap'


def key(line):
    match=DIRECTIVE.fullmatch(line.rstrip('\r\n'))
    return (bool(match[1]),match[2]) if match else None


def compose(added,original):
    """Keep original bytes/order; omit only redundant or demonstrated-invalid added directives."""
    original_keys={key(line) for line in original.splitlines() if key(line)}
    seen=set(original_keys)
    kept=[]
    for line in added.splitlines(keepends=True):
        value=key(line)
        if value is None:
            if line.strip():raise ValueError('Wrapper import segment contains a resource declaration or other code')
            kept.append(line)
        elif value not in seen and value!=(False,INVALID_WRAPPER_NAMESPACE):
            kept.append(line);seen.add(value)
    return ''.join(kept)+original


def correct_prefix(prefix,original):
    lines=prefix.splitlines(keepends=True);end=0
    while end<len(lines) and (not lines[end].strip() or key(lines[end])):end+=1
    header=''.join(lines[:end]);tail=''.join(lines[end:])
    if original:
        start=header.rfind(original)
        if start<0:raise ValueError('Original directive segment is absent from the import header')
        after=header[start+len(original):].splitlines(keepends=True)
        separator=[]
        while after and not after[-1].strip():separator.insert(0,after.pop())
        added=header[:start]+''.join(after)
        return compose(added,original)+''.join(separator)+tail
    return compose(header,'')+tail
