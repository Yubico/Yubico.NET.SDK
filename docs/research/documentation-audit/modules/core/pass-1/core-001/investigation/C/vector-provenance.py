"""Compare published fixture text encodings; do not execute crypto or SDK code."""

import base64
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[9]
OWN = Path(__file__).resolve().parent
SOURCES = OWN / "sources"
TEST = ROOT / "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    text = (TEST / "KnownPrivateKeyVectors.cs").read_text(encoding="utf-8-sig")
    literals = {
        name: "".join(re.findall(r'"([0-9A-F]+)"', body))
        for name, body in re.findall(r'public const string (\w+)\s*=\s*(.*?);', text, re.S)
    }
    rfc = (SOURCES / "rfc7517.txt").read_text(encoding="utf-8-sig")
    example = rfc[rfc.index('A.2.  Example Private Keys', 10000):]
    mapping = {"Modulus":"n", "Exponent":"e", "D":"d", "P":"p", "Q":"q",
               "DP":"dp", "DQ":"dq", "InverseQ":"qi"}
    rsa = example[example.index('"kty":"RSA"'):]
    observations = []
    for suffix, member in mapping.items():
        encoded = "".join(re.search(r'"' + member + r'":"([^"]+)"', rsa).group(1).split())
        decoded = base64.urlsafe_b64decode(encoded + "=" * (-len(encoded) % 4)).hex().upper()
        name = "Rfc7517Rsa2048" + suffix
        assert decoded == literals[name], name
        observations.append({"fixture":name,"original":"RFC 7517 Appendix A.2", "result":"literal base64url-to-hex equality"})
    rfc6979 = "".join((SOURCES / "rfc6979.txt").read_text().split())
    for name in literals:
        if name.startswith("Rfc6979"):
            assert literals[name] in rfc6979, name
            observations.append({"fixture":name,"original":"RFC 6979 A.2.5/A.2.6", "result":"literal published hex equality"})
    out = {
        "scope":"Static source-literal annotation only: base64url/hex text conversion. No signing, key generation, modular arithmetic, primality, SDK codec/test, native call or cryptographic experiment.",
        "inputs":{str(path.relative_to(ROOT)):sha(path) for path in
                  [TEST / "KnownPrivateKeyVectors.cs", SOURCES / "rfc7517.txt", SOURCES / "rfc6979.txt"]},
        "observations":observations,
        "historical_claim_limit":"This reproduces fixture text identity, not the earlier author's programmatic extraction or arithmetic verification and not provider or device conformance."
    }
    (OWN / "vector-provenance.json").write_text(json.dumps(out, indent=2) + "\n")
    print(f"PASS: {len(observations)} published literal fixture identities; no crypto/SDK execution")


if __name__ == "__main__":
    main()
