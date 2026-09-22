package main

import (
	"archive/zip"
	"context"
	"crypto"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/sha256"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/asn1"
	"encoding/binary"
	"encoding/pem"
	"errors"
	"math/big"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/sassoftware/relic/v8/lib/authenticode"
	"github.com/sassoftware/relic/v8/lib/pkcs7"
	"github.com/sassoftware/relic/v8/lib/pkcs9"
	"github.com/sassoftware/relic/v8/lib/x509tools"
)

// The fixtures below build a complete Authenticode signature in software: a
// code-signing root and leaf, a timestamp root and leaf, an RFC 3161 timestamp
// authority that runs in-process, and a minimal PE image to sign. No YubiKey,
// no network, and no pre-baked binary blobs are involved, so every negative
// case can be produced by changing one input.

// minimalPE builds the smallest PE/COFF image relic will digest. It is copied
// from the nuget-sign proof of concept rather than shelling out to a compiler
// so the tests stay hermetic and cross-platform.
func minimalPE() []byte {
	contents := make([]byte, 512)
	copy(contents, "MZ")
	binary.LittleEndian.PutUint32(contents[0x3c:], 0x80)
	copy(contents[0x80:], "PE\x00\x00")
	coff := contents[0x84:]
	binary.LittleEndian.PutUint16(coff[0:], 0x14c)
	binary.LittleEndian.PutUint16(coff[2:], 0)
	binary.LittleEndian.PutUint16(coff[16:], 224)
	binary.LittleEndian.PutUint16(coff[18:], 0x2102)
	optional := contents[0x98:]
	binary.LittleEndian.PutUint16(optional[0:], 0x10b)
	binary.LittleEndian.PutUint32(optional[32:], 0x1000)
	binary.LittleEndian.PutUint32(optional[36:], 0x200)
	binary.LittleEndian.PutUint32(optional[56:], 0x1000)
	binary.LittleEndian.PutUint32(optional[60:], 0x200)
	binary.LittleEndian.PutUint32(optional[92:], 16)
	return contents
}

type authority struct {
	key         *ecdsa.PrivateKey
	certificate *x509.Certificate
}

func newRoot(t *testing.T, name string) authority {
	t.Helper()
	now := time.Now()
	return issue(t, authority{}, &x509.Certificate{
		SerialNumber:          big.NewInt(1),
		Subject:               pkix.Name{CommonName: name},
		NotBefore:             now.Add(-time.Hour),
		NotAfter:              now.Add(24 * time.Hour),
		KeyUsage:              x509.KeyUsageCertSign,
		BasicConstraintsValid: true,
		IsCA:                  true,
	})
}

// newIntermediate is a CA that is not self-signed, which is what a trust
// anchor file must not contain.
func newIntermediate(t *testing.T, root authority, name string) authority {
	t.Helper()
	now := time.Now()
	return issue(t, root, &x509.Certificate{
		SerialNumber:          big.NewInt(3),
		Subject:               pkix.Name{CommonName: name},
		NotBefore:             now.Add(-time.Hour),
		NotAfter:              now.Add(24 * time.Hour),
		KeyUsage:              x509.KeyUsageCertSign,
		BasicConstraintsValid: true,
		IsCA:                  true,
	})
}

// newSelfSignedNonCA is self-signed but carries no CA basic constraint, which
// is the other half of what a trust anchor has to be.
func newSelfSignedNonCA(t *testing.T, name string) authority {
	t.Helper()
	now := time.Now()
	return issue(t, authority{}, &x509.Certificate{
		SerialNumber:          big.NewInt(4),
		Subject:               pkix.Name{CommonName: name},
		NotBefore:             now.Add(-time.Hour),
		NotAfter:              now.Add(24 * time.Hour),
		KeyUsage:              x509.KeyUsageDigitalSignature,
		ExtKeyUsage:           []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning},
		BasicConstraintsValid: true,
	})
}

func newLeaf(t *testing.T, root authority, name string, usage x509.ExtKeyUsage) authority {
	t.Helper()
	now := time.Now()
	return issue(t, root, &x509.Certificate{
		SerialNumber: big.NewInt(2),
		Subject:      pkix.Name{CommonName: name},
		NotBefore:    now.Add(-time.Hour),
		NotAfter:     now.Add(24 * time.Hour),
		KeyUsage:     x509.KeyUsageDigitalSignature,
		ExtKeyUsage:  []x509.ExtKeyUsage{usage},
	})
}

func issue(t *testing.T, parent authority, template *x509.Certificate) authority {
	t.Helper()
	key, err := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	if err != nil {
		t.Fatal(err)
	}
	signingCertificate, signingKey := template, key
	if parent.certificate != nil {
		signingCertificate, signingKey = parent.certificate, parent.key
	}
	der, err := x509.CreateCertificate(rand.Reader, template, signingCertificate, &key.PublicKey, signingKey)
	if err != nil {
		t.Fatal(err)
	}
	certificate, err := x509.ParseCertificate(der)
	if err != nil {
		t.Fatal(err)
	}
	return authority{key: key, certificate: certificate}
}

func poolOf(certificates ...*x509.Certificate) *x509.CertPool {
	pool := x509.NewCertPool()
	for _, certificate := range certificates {
		pool.AddCert(certificate)
	}
	return pool
}

func writePEM(t *testing.T, directory, name string, certificates ...*x509.Certificate) string {
	t.Helper()
	path := filepath.Join(directory, name)
	file, err := os.Create(path)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	for _, certificate := range certificates {
		if err := pem.Encode(file, &pem.Block{Type: "CERTIFICATE", Bytes: certificate.Raw}); err != nil {
			t.Fatal(err)
		}
	}
	return path
}

// localTimestamper is a whole RFC 3161 timestamp authority in a struct. It
// produces a real timestamp token: a nested SignedData over a TSTInfo whose
// message imprint digests the parent signature's encrypted digest. Both of
// those are the things the verifier must check, so the fixture has to get them
// right for the positive case to pass — and each of them can be broken on
// purpose to show that the verifier notices.
type localTimestamper struct {
	signer authority
	// genTime is the time the token attests to. Tests override it to produce
	// a token whose timestamp is the zero time.
	genTime time.Time
	// imprintOf, when set, is what the token attests to instead of the
	// signature it is attached to. A token like this is internally consistent
	// but binds nothing.
	imprintOf []byte
	// corruptSignature damages the token's own CMS signature after it is
	// made, so the token no longer proves the authority issued it.
	corruptSignature bool
}

func (ts localTimestamper) Timestamp(_ context.Context, request *pkcs9.Request) (*pkcs7.ContentInfoSignedData, error) {
	algorithm, ok := x509tools.PkixDigestAlgorithm(request.Hash)
	if !ok {
		return nil, errors.New("unsupported timestamp hash")
	}
	attested := request.EncryptedDigest
	if ts.imprintOf != nil {
		attested = ts.imprintOf
	}
	imprint := request.Hash.New()
	imprint.Write(attested)
	encoded, err := asn1.MarshalWithParams(ts.genTime.UTC(), "generalized")
	if err != nil {
		return nil, err
	}
	var genTime asn1.RawValue
	if _, err := asn1.Unmarshal(encoded, &genTime); err != nil {
		return nil, err
	}
	info := pkcs9.TSTInfo{
		Version: 1,
		Policy:  asn1.ObjectIdentifier{1, 3, 6, 1, 4, 1, 41482, 99, 1},
		MessageImprint: pkcs9.MessageImprint{
			HashAlgorithm: algorithm,
			HashedMessage: imprint.Sum(nil),
		},
		SerialNumber: big.NewInt(7),
		GenTime:      genTime,
	}
	// RFC 3161 carries the TSTInfo as an OCTET STRING inside the eContent, so
	// the DER has to be wrapped rather than embedded as a bare SEQUENCE.
	der, err := asn1.Marshal(info)
	if err != nil {
		return nil, err
	}
	builder := pkcs7.NewBuilder(ts.signer.key, []*x509.Certificate{ts.signer.certificate}, crypto.SHA256)
	if err := builder.SetContent(pkcs9.OidTSTInfo, der); err != nil {
		return nil, err
	}
	token, err := builder.Sign()
	if err != nil {
		return nil, err
	}
	if ts.corruptSignature {
		token.Content.SignerInfos[0].EncryptedDigest[0] ^= 0xff
	}
	return token, nil
}

// signingFixture is a complete set of trust anchors plus a signer, so a test
// can produce a signed image and the pools that should accept it.
type signingFixture struct {
	codeRoot      authority
	codeLeaf      authority
	timestampRoot authority
	timestampLeaf authority
	timestamper   *localTimestamper
	// imageHash is the Authenticode image digest algorithm. Tests override it
	// to exercise the SHA-256 guard.
	imageHash crypto.Hash
	// timestampHash is the algorithm the timestamp authority digests the
	// signature with to form the TSTInfo message imprint. relic's own signer
	// derives this from the signature's digest algorithm; keeping it separate
	// here lets the two SHA-256 guards be tested one at a time.
	timestampHash crypto.Hash
}

func newSigningFixture(t *testing.T) *signingFixture {
	t.Helper()
	codeRoot := newRoot(t, "relic-verify test code root")
	timestampRoot := newRoot(t, "relic-verify test timestamp root")
	timestampLeaf := newLeaf(t, timestampRoot, "relic-verify test TSA", x509.ExtKeyUsageTimeStamping)
	return &signingFixture{
		codeRoot:      codeRoot,
		codeLeaf:      newLeaf(t, codeRoot, "relic-verify test signer", x509.ExtKeyUsageCodeSigning),
		timestampRoot: timestampRoot,
		timestampLeaf: timestampLeaf,
		timestamper:   &localTimestamper{signer: timestampLeaf, genTime: time.Now()},
		imageHash:     crypto.SHA256,
		timestampHash: crypto.SHA256,
	}
}

// signerFingerprint is the SHA-256 of the signing certificate, which is what
// --signer-fingerprint pins.
func (f *signingFixture) signerFingerprint() []byte {
	digest := sha256.Sum256(f.codeLeaf.certificate.Raw)
	return digest[:]
}

func (f *signingFixture) roots() *x509.CertPool {
	return poolOf(f.codeRoot.certificate)
}

func (f *signingFixture) timestampRoots() *x509.CertPool {
	return poolOf(f.timestampRoot.certificate)
}

func (f *signingFixture) sign(t *testing.T, contents []byte) []byte {
	t.Helper()
	return f.signWith(t, contents, nil)
}

// signWith produces a signed PE image the way relic's own Authenticode signer
// does — digest the image, sign the indirect data with the SpcSpOpusInfo and
// statement-type attributes, attach the timestamp token, patch the image, fix
// the PE checksum — but stops short of relic's self-check so that tamper can
// break the signature first. relic will not emit a signature that fails its
// own verification, so an artifact a verifier is supposed to reject has to be
// assembled here rather than asked for.
func (f *signingFixture) signWith(t *testing.T, contents []byte, tamper func(*pkcs7.ContentInfoSignedData)) []byte {
	t.Helper()
	path := filepath.Join(t.TempDir(), "image.dll")
	if err := os.WriteFile(path, contents, 0o600); err != nil {
		t.Fatal(err)
	}
	file, err := os.OpenFile(path, os.O_RDWR, 0o600)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()

	digest, err := authenticode.DigestPE(file, f.imageHash, false)
	if err != nil {
		t.Fatal(err)
	}
	indirect, err := digest.GetIndirect()
	if err != nil {
		t.Fatal(err)
	}

	builder := pkcs7.NewBuilder(f.codeLeaf.key, []*x509.Certificate{f.codeLeaf.certificate}, f.imageHash)
	if err := builder.SetContent(authenticode.OidSpcIndirectDataContent, indirect); err != nil {
		t.Fatal(err)
	}
	statement := authenticode.SpcSpStatementType{Type: authenticode.OidSpcIndividualPurpose}
	if err := builder.AddAuthenticatedAttribute(authenticode.OidSpcStatementType, statement); err != nil {
		t.Fatal(err)
	}
	if err := builder.AddAuthenticatedAttribute(authenticode.OidSpcSpOpusInfo, authenticode.SpcSpOpusInfo{}); err != nil {
		t.Fatal(err)
	}
	signature, err := builder.Sign()
	if err != nil {
		t.Fatal(err)
	}

	if f.timestamper != nil {
		signerInfo := &signature.Content.SignerInfos[0]
		token, err := f.timestamper.Timestamp(context.Background(), &pkcs9.Request{
			EncryptedDigest: signerInfo.EncryptedDigest,
			Hash:            f.timestampHash,
		})
		if err != nil {
			t.Fatal(err)
		}
		if err := pkcs9.AddStampToSignedAuthenticode(signerInfo, *token); err != nil {
			t.Fatal(err)
		}
	}
	if tamper != nil {
		tamper(signature)
	}

	raw, err := signature.Marshal()
	if err != nil {
		t.Fatal(err)
	}
	patch, err := digest.MakePatch(raw)
	if err != nil {
		t.Fatal(err)
	}
	if err := patch.Apply(file, path); err != nil {
		t.Fatal(err)
	}
	if err := authenticode.FixPEChecksum(file); err != nil {
		t.Fatal(err)
	}
	signed, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	return signed
}

type packageEntry struct {
	name     string
	contents []byte
}

func writeNupkg(t *testing.T, entries ...packageEntry) string {
	t.Helper()
	path := filepath.Join(t.TempDir(), "test.nupkg")
	file, err := os.Create(path)
	if err != nil {
		t.Fatal(err)
	}
	writer := zip.NewWriter(file)
	for _, entry := range entries {
		// CreateHeader with Store keeps duplicate and unsafe names intact;
		// Create would still allow them, but being explicit documents that
		// these archives are deliberately hostile in some tests.
		destination, err := writer.CreateHeader(&zip.FileHeader{Name: entry.name, Method: zip.Store})
		if err != nil {
			t.Fatal(err)
		}
		if _, err := destination.Write(entry.contents); err != nil {
			t.Fatal(err)
		}
	}
	if err := writer.Close(); err != nil {
		t.Fatal(err)
	}
	if err := file.Close(); err != nil {
		t.Fatal(err)
	}
	return path
}
