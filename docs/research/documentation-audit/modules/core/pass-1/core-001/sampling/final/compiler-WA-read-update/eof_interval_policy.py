"""Pure future-handoff comparator; no file discovery, reads, writes or sampling verdicts."""
import hashlib


def compare_eof_append(before,after,start_character,end_character_exclusive):
    """Inventory intervals are decoded UTF-8-sig characters; range hashes encode that exact slice.

    Accept only one literal terminal LF/CRLF append to a previously unterminated file. A changed
    original interval, newline normalization elsewhere, BOM change, or repeated terminator is not
    this exception. Content correctness and actual sampler credit remain separate.
    """
    digest=lambda b:hashlib.sha256(b).hexdigest()
    if before.endswith((b'\n',b'\r')) or after not in (before+b'\n',before+b'\r\n'):
        return dict(state='NOT-STRICT-EOF-ONLY-APPEND',semantic_eligibility_decision='NO AUTOMATIC CHANGE; use ordinary exact-span reconciliation')
    old=before.decode('utf-8-sig');new=after.decode('utf-8-sig')
    assert 0<=start_character<=end_character_exclusive<=len(old)
    old_unit=old[start_character:end_character_exclusive];new_interval=new[start_character:end_character_exclusive]
    suffix=after[len(before):]
    assert old_unit.encode()==new_interval.encode()
    assert old_unit.replace('\r\n','\n')==new_interval.replace('\r\n','\n')
    touches_eof=end_character_exclusive==len(old)
    successor_unit=new[start_character:] if touches_eof else new_interval
    return dict(state='STRICT-EOF-ONLY-APPEND',literal_append_hex=suffix.hex(),literal_append_bytes=len(suffix),
        original_file_bytes=len(before),successor_file_bytes=len(after),literal_change_byte_interval=[len(before),len(after)],
        original_unit_character_interval=[start_character,end_character_exclusive],
        original_interval_bytes_unchanged=True,original_interval_normalized_text_unchanged=True,
        original_interval_sha256=digest(old_unit.encode()),same_interval_successor_sha256=digest(new_interval.encode()),
        successor_unit_including_EOF_sha256=digest(successor_unit.encode()),
        original_unit_ended_at_EOF=touches_eof,successor_unit_literal_EOF_difference=suffix.decode() if touches_eof else '',
        semantic_edit_exclusion=False,whole_file_exclusion=False,content_or_sample_credit_granted=False,
        qualification='Literal EOF byte/range difference only. No source correctness, compiler, formatter or final sampling acceptance inferred.')
