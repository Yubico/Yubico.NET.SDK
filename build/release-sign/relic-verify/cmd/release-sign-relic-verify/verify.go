package main

import (
	"bytes"
	"crypto"
	"crypto/hmac"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"os"
	"strings"
	"time"
	"unicode"

	"github.com/sassoftware/relic/v8/lib/authenticode"
)

// assemblyResult is what a successful verification established. It exists so
// the PASS line reports facts the verifier actually checked rather than facts
// it read out of the file.
type assemblyResult struct {
	imageHash          crypto.Hash
	signer             *x509.Certificate
	timestampAuthority *x509.Certificate
	timestampedAt      time.Time
}

// verifyAssembly checks one Authenticode-signed PE image against the supplied
// trust anchors and the pinned signing certificate. Every check here is
// mandatory; there is deliberately no option to relax any of them, and in
// particular no option to accept a signature without a timestamp.
//
// relic does the cryptography: VerifyPE re-digests the image and checks the
// digest against the SpcIndirectDataContent, verifies the CMS signature over
// the signed attributes, and — because relic parses the timestamp through
// pkcs9 — verifies the timestamp token's own CMS signature and that its
// message imprint really digests this signature's encrypted digest. This
// program's job is to insist on the policy around those results.
func verifyAssembly(contents []byte, roots, timestampRoots *x509.CertPool, expectedSigner []byte) (assemblyResult, error) {
	if roots == nil {
		return assemblyResult{}, errors.New("no code-signing roots were supplied")
	}
	if timestampRoots == nil {
		return assemblyResult{}, errors.New("no timestamp roots were supplied")
	}
	if len(expectedSigner) != sha256.Size {
		return assemblyResult{}, fmt.Errorf("expected signer fingerprint is %d bytes, want %d", len(expectedSigner), sha256.Size)
	}

	signatures, err := authenticode.VerifyPE(bytes.NewReader(contents), false)
	if err != nil {
		return assemblyResult{}, err
	}
	if len(signatures) != 1 {
		return assemblyResult{}, fmt.Errorf("found %d Authenticode signatures, want exactly one", len(signatures))
	}
	signature := signatures[0]

	if signature.ImageHashFunc != crypto.SHA256 {
		return assemblyResult{}, fmt.Errorf("Authenticode image digest is %s, want SHA-256", signature.ImageHashFunc)
	}

	timestamp := signature.CounterSignature
	if timestamp == nil {
		return assemblyResult{}, errors.New("Authenticode signature has no RFC 3161 timestamp")
	}
	if timestamp.SigningTime.IsZero() {
		return assemblyResult{}, errors.New("RFC 3161 timestamp attests to the zero time")
	}
	// CounterSignature.Hash is the algorithm from the token's TSTInfo message
	// imprint, which is the digest binding the token to this signature. A weak
	// one there would let a token issued for some other signature be reused
	// here, so the same SHA-256 floor applies as to the image. An algorithm
	// relic did not recognise arrives here as the zero Hash and is rejected
	// by the same comparison.
	if timestamp.Hash != crypto.SHA256 {
		return assemblyResult{}, fmt.Errorf("timestamp message imprint is %s, want SHA-256", timestamp.Hash)
	}
	// The timestamp chain is checked as of the moment the authority says it
	// signed, which is the only time the token itself vouches for.
	if err := timestamp.VerifyChain(timestampRoots, nil); err != nil {
		return assemblyResult{}, fmt.Errorf("timestamp certificate chain: %w", err)
	}
	// The signer chain is checked as of the timestamp, so the signature keeps
	// verifying after the signing certificate expires. Note that the signer is
	// checked against roots and the authority against timestampRoots: passing
	// one pool for both would let a timestamp authority issue code-signing
	// certificates that this program would accept.
	if err := signature.Signature.VerifyChain(roots, nil, x509.ExtKeyUsageCodeSigning, timestamp.SigningTime); err != nil {
		return assemblyResult{}, fmt.Errorf("code-signing certificate chain: %w", err)
	}

	// Last, once the signature is known to be sound, check that it is the
	// signature we were expecting. Chaining to the root only says the root
	// issued it; every other certificate that root issues would chain too.
	actual := sha256.Sum256(signature.Certificate.Raw)
	if !hmac.Equal(actual[:], expectedSigner) {
		return assemblyResult{}, fmt.Errorf("signer certificate is %s, want %s",
			strings.ToUpper(hex.EncodeToString(actual[:])),
			strings.ToUpper(hex.EncodeToString(expectedSigner)))
	}

	return assemblyResult{
		imageHash:          signature.ImageHashFunc,
		signer:             signature.Certificate,
		timestampAuthority: timestamp.Certificate,
		timestampedAt:      timestamp.SigningTime,
	}, nil
}

// parseFingerprint turns a SHA-256 certificate fingerprint as a human would
// paste it — from certmgr, from `openssl x509 -fingerprint`, upper or lower
// case, with or without separators — into the 32 bytes it denotes. Separators
// and surrounding whitespace are normalised away, but the result still has to
// be exactly 64 hexadecimal digits: a SHA-1 fingerprint pasted by mistake is a
// mistake worth stopping on.
func parseFingerprint(value string) ([]byte, error) {
	normalized := strings.Map(func(r rune) rune {
		if r == ':' || r == '-' || unicode.IsSpace(r) {
			return -1
		}
		return r
	}, value)
	if len(normalized) != 2*sha256.Size {
		return nil, fmt.Errorf("%q is %d characters once separators are removed, want %d hexadecimal digits",
			value, len(normalized), 2*sha256.Size)
	}
	digest, err := hex.DecodeString(strings.ToLower(normalized))
	if err != nil {
		return nil, fmt.Errorf("%q is not hexadecimal: %w", value, err)
	}
	return digest, nil
}

// certificatePool reads PEM trust anchors into a pool. Everything in the file
// has to be a self-signed CA certificate: the file is a list of things this
// program will believe without further evidence, and a leaf or an intermediate
// in there would quietly widen that to whatever issued it — or, for a leaf, let
// a signer vouch for itself. Anything that is not a parseable certificate is an
// error rather than something to skip, so a truncated or mistyped file cannot
// produce a pool that trusts less, or more, than the caller thinks.
func certificatePool(path string) (*x509.CertPool, error) {
	contents, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}
	pool := x509.NewCertPool()
	count := 0
	for rest := contents; len(bytes.TrimSpace(rest)) != 0; {
		var block *pem.Block
		block, rest = pem.Decode(rest)
		if block == nil {
			return nil, fmt.Errorf("%s: contains data that is not PEM", path)
		}
		if block.Type != "CERTIFICATE" {
			return nil, fmt.Errorf("%s: contains a %q block, want CERTIFICATE", path, block.Type)
		}
		certificate, err := x509.ParseCertificate(block.Bytes)
		if err != nil {
			return nil, fmt.Errorf("%s: %w", path, err)
		}
		if err := checkTrustAnchor(certificate); err != nil {
			return nil, fmt.Errorf("%s: %s: %w", path, certificate.Subject, err)
		}
		pool.AddCert(certificate)
		count++
	}
	if count == 0 {
		return nil, fmt.Errorf("%s: contains no certificates", path)
	}
	return pool, nil
}

func checkTrustAnchor(certificate *x509.Certificate) error {
	if !certificate.BasicConstraintsValid || !certificate.IsCA {
		return errors.New("is not a CA certificate")
	}
	if !bytes.Equal(certificate.RawIssuer, certificate.RawSubject) {
		return errors.New("is not self-signed: its issuer and subject differ")
	}
	if err := certificate.CheckSignatureFrom(certificate); err != nil {
		return fmt.Errorf("is not self-signed: %w", err)
	}
	return nil
}
