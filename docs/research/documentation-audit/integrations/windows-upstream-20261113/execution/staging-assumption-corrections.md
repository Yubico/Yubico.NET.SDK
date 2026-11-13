# Staging receipt corrections

The first publication-preparation attempt stopped before staging because its absolute root spelling
did not match the relative `status.md` exception. The original preparation hashes were not changed;
the successor uses repository-relative paths.

The second attempt explicitly staged the approved packet, then stopped while comparing working bytes
to Git bytes. The two incoming Core/Windows-verifier project files retain CRLF working bytes while Git
stores their LF-normalized blobs under repository attributes. No file was rewritten. The successor
manifest records both working SHA-256 and filtered Git blob OID and verifies the latter against the
index. C# source hashes and frozen record hashes remain exact. These are corrected receipt assumptions,
not failed runtime, compiler or preservation gates; no commit or push occurred.
