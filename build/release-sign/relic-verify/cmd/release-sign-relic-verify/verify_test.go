package main

import (
	"crypto"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"

	"github.com/sassoftware/relic/v8/lib/pkcs7"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestVerifyAssemblyAcceptsTimestampedSignature(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	result, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err != nil {
		t.Fatal(err)
	}
	if result.imageHash != crypto.SHA256 {
		t.Fatalf("image hash is %s, want SHA-256", result.imageHash)
	}
	if !result.signer.Equal(fixture.codeLeaf.certificate) {
		t.Fatalf("signer is %s, want %s", result.signer.Subject, fixture.codeLeaf.certificate.Subject)
	}
	if !result.timestampAuthority.Equal(fixture.timestampLeaf.certificate) {
		t.Fatalf("timestamp authority is %s", result.timestampAuthority.Subject)
	}
	if result.timestampedAt.IsZero() {
		t.Fatal("timestamp is zero")
	}
	if delta := time.Since(result.timestampedAt); delta < 0 || delta > time.Hour {
		t.Fatalf("timestamp %s is not close to now", result.timestampedAt)
	}
}

func TestVerifyAssemblyRejectsModifiedImage(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	// Byte 511 is the last byte of the original image, inside the range the
	// Authenticode digest covers and outside the certificate table.
	modified := append([]byte(nil), signed...)
	modified[511] ^= 0x01

	if _, err := verifyAssembly(modified, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint()); err == nil {
		t.Fatal("verified an image that was modified after signing")
	}
}

// A verifier that only compared the image against the digest in the signature
// would pass a forgery, because an attacker who can rewrite the image can
// rewrite that digest too. What stops them is the CMS signature over the
// signed attributes, so that signature has to be checked.
func TestVerifyAssemblyRejectsTamperedSignerSignature(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.signWith(t, minimalPE(), func(signature *pkcs7.ContentInfoSignedData) {
		signature.Content.SignerInfos[0].EncryptedDigest[0] ^= 0xff
	})

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "indirect signature") {
		t.Fatalf("expected the CMS signature to be rejected, got %v", err)
	}
}

// This is the check the tool exists for. A timestamp token is a signed
// statement that some other blob existed at some time; unless the imprint
// inside it is compared against this signature's encrypted digest, a token
// issued for a completely different signature would be accepted, and with it
// the expired certificate the timestamp props up.
func TestVerifyAssemblyRejectsTimestampBoundToAnotherSignature(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.timestamper.imprintOf = []byte("some other signature entirely")
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "timestamp imprint") {
		t.Fatalf("expected the timestamp imprint to be rejected, got %v", err)
	}
}

// The other half of the same check: the token's own CMS signature has to be
// verified, not just the chain of the certificate it names.
func TestVerifyAssemblyRejectsTimestampWithBrokenSignature(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.timestamper.corruptSignature = true
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "verifying timestamp: invalid") {
		t.Fatalf("expected the timestamp signature to be rejected, got %v", err)
	}
}

func TestVerifyAssemblyRejectsUnsignedAndMalformedImages(t *testing.T) {
	fixture := newSigningFixture(t)
	for name, contents := range map[string][]byte{
		"unsigned":  minimalPE(),
		"not a PE":  []byte("this is not a portable executable"),
		"empty":     {},
		"truncated": minimalPE()[:64],
	} {
		t.Run(name, func(t *testing.T) {
			if _, err := verifyAssembly(contents, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint()); err == nil {
				t.Fatalf("verified %s input", name)
			}
		})
	}
}

func TestVerifyAssemblyRejectsWrongCodeSigningRoot(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())
	stranger := newRoot(t, "unrelated root")

	_, err := verifyAssembly(signed, poolOf(stranger.certificate), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "code-signing certificate chain") {
		t.Fatalf("expected code-signing chain failure, got %v", err)
	}
}

func TestVerifyAssemblyRejectsWrongTimestampRoot(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())
	stranger := newRoot(t, "unrelated root")

	_, err := verifyAssembly(signed, fixture.roots(), poolOf(stranger.certificate), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "timestamp certificate chain") {
		t.Fatalf("expected timestamp chain failure, got %v", err)
	}
}

// The code-signing root must not be accepted as a timestamp root, and the
// other way round: passing one pool for both jobs is exactly the mistake
// --timestamp-root exists to prevent.
func TestVerifyAssemblyKeepsCodeAndTimestampRootsSeparate(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	if _, err := verifyAssembly(signed, fixture.roots(), fixture.roots(), fixture.signerFingerprint()); err == nil {
		t.Fatal("code-signing root was accepted as a timestamp root")
	}
	if _, err := verifyAssembly(signed, fixture.timestampRoots(), fixture.timestampRoots(), fixture.signerFingerprint()); err == nil {
		t.Fatal("timestamp root was accepted as a code-signing root")
	}
}

func TestVerifyAssemblyRejectsNonSHA256ImageDigest(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.imageHash = crypto.SHA1
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "want SHA-256") {
		t.Fatalf("expected SHA-256 image digest guard, got %v", err)
	}
}

// There is no option anywhere in this program to accept an untimestamped
// signature. If this test ever needs a flag to make it pass, the production
// requirement has been weakened.
func TestVerifyAssemblyRequiresTimestamp(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.timestamper = nil
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "no RFC 3161 timestamp") {
		t.Fatalf("expected timestamp requirement, got %v", err)
	}
}

func TestVerifyAssemblyRejectsZeroTimestamp(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.timestamper.genTime = time.Time{}
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "attests to the zero time") {
		t.Fatalf("expected zero timestamp rejection, got %v", err)
	}
}

// A timestamp is only as good as the digest binding it to the signature. A
// SHA-1 imprint is one a collision could be found for, which would let a token
// issued for one signature be reused for another.
func TestVerifyAssemblyRejectsNonSHA256TimestampImprint(t *testing.T) {
	fixture := newSigningFixture(t)
	fixture.timestampHash = crypto.SHA1
	signed := fixture.sign(t, minimalPE())

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), fixture.signerFingerprint())
	if err == nil || !strings.Contains(err.Error(), "timestamp message imprint") {
		t.Fatalf("expected SHA-256 timestamp imprint guard, got %v", err)
	}
}

// Chaining to the right root is not the same as being the right signer: any
// certificate the root issues would chain. --signer-fingerprint pins which one.
func TestVerifyAssemblyRejectsUnexpectedSigner(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	// A second leaf under the same root: cryptographically impeccable, and
	// still not the certificate the release is supposed to be signed with.
	imposter := newLeaf(t, fixture.codeRoot, "another signer under the same root", x509.ExtKeyUsageCodeSigning)
	digest := sha256.Sum256(imposter.certificate.Raw)

	_, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), digest[:])
	if err == nil || !strings.Contains(err.Error(), "signer certificate is") {
		t.Fatalf("expected the pinned signer to be enforced, got %v", err)
	}
}

func TestVerifyAssemblyRequiresBothRootPools(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	if _, err := verifyAssembly(signed, nil, fixture.timestampRoots(), fixture.signerFingerprint()); err == nil {
		t.Fatal("verified without code-signing roots")
	}
	if _, err := verifyAssembly(signed, fixture.roots(), nil, fixture.signerFingerprint()); err == nil {
		t.Fatal("verified without timestamp roots")
	}
}

func TestVerifyAssemblyRequiresASignerFingerprint(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())

	for name, expected := range map[string][]byte{
		"absent": nil,
		"empty":  {},
		"short":  fixture.signerFingerprint()[:31],
	} {
		t.Run(name, func(t *testing.T) {
			if _, err := verifyAssembly(signed, fixture.roots(), fixture.timestampRoots(), expected); err == nil {
				t.Fatalf("verified with %s signer fingerprint", name)
			}
		})
	}
}

func TestParseFingerprint(t *testing.T) {
	want := strings.Repeat("ab", 32)
	for name, given := range map[string]string{
		"lower case":               want,
		"upper case":               strings.ToUpper(want),
		"colon separated":          strings.TrimSuffix(strings.Repeat("ab:", 32), ":"),
		"space separated":          strings.TrimSuffix(strings.Repeat("ab ", 32), " "),
		"hyphenated":               strings.TrimSuffix(strings.Repeat("AB-", 32), "-"),
		"surrounded by whitespace": "  " + want + "\n",
	} {
		t.Run(name, func(t *testing.T) {
			got, err := parseFingerprint(given)
			if err != nil {
				t.Fatal(err)
			}
			if hex.EncodeToString(got) != want {
				t.Fatalf("parseFingerprint(%q) = %x", given, got)
			}
		})
	}

	for name, given := range map[string]string{
		"empty":      "",
		"too short":  strings.Repeat("ab", 31),
		"too long":   strings.Repeat("ab", 33),
		"not hex":    strings.Repeat("zz", 32),
		"SHA-1 size": strings.Repeat("ab", 20),
		"decimal":    strings.Repeat("9", 63) + " ",
	} {
		t.Run(name, func(t *testing.T) {
			if got, err := parseFingerprint(given); err == nil {
				t.Fatalf("parseFingerprint(%q) = %x, want an error", given, got)
			}
		})
	}
}

func TestCertificatePool(t *testing.T) {
	directory := t.TempDir()
	root := newRoot(t, "pool root")

	pool, err := certificatePool(writePEM(t, directory, "root.pem", root.certificate))
	if err != nil {
		t.Fatal(err)
	}
	leaf := newLeaf(t, root, "pool leaf", x509.ExtKeyUsageCodeSigning)
	if _, err := leaf.certificate.Verify(x509.VerifyOptions{
		Roots:     pool,
		KeyUsages: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning},
	}); err != nil {
		t.Fatal(err)
	}

	// Two roots in one file is ordinary: a release may be checked against an
	// outgoing and an incoming root at the same time.
	second := newRoot(t, "second pool root")
	if _, err := certificatePool(writePEM(t, directory, "roots.pem", root.certificate, second.certificate)); err != nil {
		t.Fatal(err)
	}

	empty := filepath.Join(directory, "empty.pem")
	if err := os.WriteFile(empty, []byte("not a certificate\n"), 0o600); err != nil {
		t.Fatal(err)
	}
	if _, err := certificatePool(empty); err == nil {
		t.Fatal("accepted a PEM file with no certificates")
	}
	if _, err := certificatePool(filepath.Join(directory, "absent.pem")); err == nil {
		t.Fatal("accepted a missing PEM file")
	}
}

// A trust anchor file is a list of things this program will believe without
// further evidence, so it has to contain root CAs. A leaf slipped into it would
// otherwise make any assembly that leaf signed verify against itself.
func TestCertificatePoolRequiresSelfSignedCARoots(t *testing.T) {
	directory := t.TempDir()
	root := newRoot(t, "genuine root")

	for name, certificate := range map[string]*x509.Certificate{
		"leaf":               newLeaf(t, root, "a leaf", x509.ExtKeyUsageCodeSigning).certificate,
		"intermediate":       newIntermediate(t, root, "an intermediate CA").certificate,
		"self-signed non-CA": newSelfSignedNonCA(t, "self-signed but not a CA").certificate,
	} {
		t.Run(name, func(t *testing.T) {
			path := writePEM(t, t.TempDir(), "root.pem", certificate)
			if _, err := certificatePool(path); err == nil {
				t.Fatalf("accepted a %s as a trust anchor", name)
			}
		})
	}

	// One bad entry poisons the file even when a genuine root is alongside it.
	mixed := writePEM(t, directory, "mixed.pem", root.certificate,
		newLeaf(t, root, "a leaf", x509.ExtKeyUsageCodeSigning).certificate)
	if _, err := certificatePool(mixed); err == nil {
		t.Fatal("accepted a trust anchor file containing a leaf")
	}
}
