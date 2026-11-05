# Research execution limits and preserved failures

Two primary-source batches ran: initial collection and one focused follow-up. URL404/403 outcomes
remain in their manifests, alongside captured source hashes. A guessed separate SP800-73-4 Part2
URL404 was resolved by reading the combined original, which contains all three parts and its own
withdrawal notice. CSRC landing-page403 responses leave no status gap because the original PDFs
carry the status notices. The optional Python CLI device-test URL404 remains explicit; pinned
device tests, unit tests and the CLI implementation were captured instead.

The first follow-up collector completed downloads, then stopped at its optional HTML parser:

```text
Traceback (most recent call last):
  File "collect-followup.py", line 21, in <module>
    from bs4 import BeautifulSoup
ModuleNotFoundError: No module named 'bs4'
Exited with code 1
```

The corrected collector used Python's standard-library HTMLParser, reused the recorded download
bytes without repeating requests, and completed local pinned-ancestry history capture (exit0).
Its source is retained. The successful record verifier checks the original capture hashes.

Context7 was consulted for .NET cryptography documentation; direct Microsoft RSA.VerifyData API
content was retained in the focused follow-up. No .NET code was compiled or executed by this
investigator. Local Python scripts capture/index/check research artifacts only; they neither invoke
an SDK/test runner nor open a device.

The coordinator supplied the two fresh device baselines as steering messages. Their actual commands,
hardware state changes and later execution logs belong to the coordinator's hardware record, not
this investigator's execution claims. Git status also contains coordinator/user work; all123 PIV
product inventory hashes remained unchanged at the investigator's record-verification checkpoint.
