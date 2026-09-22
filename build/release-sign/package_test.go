package main

import (
	"archive/zip"
	"bytes"
	"context"
	"crypto"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/binary"
	"math/big"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/Yubico/nuget-sign/pkg/assembly"
)

func TestAssemblySignDigestChecksumAndPackageRawPreservation(t *testing.T) {
	key, cert := testIdentity(t)
	original := minimalPE()
	signed, err := signPE(context.Background(), original, assembly.SignOptions{Certificates: []*x509.Certificate{cert}, Key: key, Hash: crypto.SHA256})
	if err != nil {
		t.Fatal(err)
	}
	sig, err := mustAssembly(t, signed).Signature()
	if err != nil || !sig.Matches() || sig.Hash != crypto.SHA256 {
		t.Fatalf("signature=%v err=%v", sig, err)
	}
	pe := int(binary.LittleEndian.Uint32(signed[0x3c:]))
	if binary.LittleEndian.Uint32(signed[pe+88:]) == 0 {
		t.Fatal("PE checksum was not set")
	}
	if _, err := signPE(context.Background(), signed, assembly.SignOptions{Certificates: []*x509.Certificate{cert}, Key: key}); err == nil {
		t.Fatal("accepted signed input")
	}

	dir := t.TempDir()
	input := writeZip(t, dir, "in.nupkg", []zipItem{{"lib/a.dll", original}, {"data.bin", bytes.Repeat([]byte("raw"), 100)}})
	output := filepath.Join(dir, "out.nupkg")
	if err := rewritePackage(input, output, map[string]struct{}{"lib/a.dll": {}}, func(_ string, b []byte) ([]byte, error) { return append(b, 0), nil }); err != nil {
		t.Fatal(err)
	}
	if !bytes.Equal(rawEntry(t, input, "data.bin"), rawEntry(t, output, "data.bin")) {
		t.Fatal("unselected compressed bytes changed")
	}
}

func TestAuthenticodeMutationGuardRejectsExecutableChange(t *testing.T) {
	key, cert := testIdentity(t)
	original := minimalPE()
	signed, err := signPE(context.Background(), original, assembly.SignOptions{Certificates: []*x509.Certificate{cert}, Key: key})
	if err != nil {
		t.Fatal(err)
	}
	if err := verifyAuthenticodeOnlyMutation(original, signed); err != nil {
		t.Fatal(err)
	}

	mutated := append([]byte(nil), signed...)
	mutated[len(original)-1] ^= 1
	if err := verifyAuthenticodeOnlyMutation(original, mutated); err == nil || !strings.Contains(err.Error(), "executable byte") {
		t.Fatalf("expected executable mutation rejection, got %v", err)
	}
}

func TestComparePreservationForNupkgAndSnupkg(t *testing.T) {
	dir := t.TempDir()
	in := writeZip(t, dir, "in.snupkg", []zipItem{{"x.nuspec", []byte("same")}, {"symbols.pdb", []byte("symbols")}})
	out := writeZip(t, dir, "out.snupkg", []zipItem{{"x.nuspec", []byte("same")}, {"symbols.pdb", []byte("symbols")}, {".signature.p7s", []byte("sig")}})
	if err := verifyPreservation(in, out, nil, true); err != nil {
		t.Fatal(err)
	}
	bad := writeZip(t, dir, "bad.snupkg", []zipItem{{"x.nuspec", []byte("changed")}, {"symbols.pdb", []byte("symbols")}, {".signature.p7s", []byte("sig")}})
	if err := verifyPreservation(in, bad, nil, true); err == nil {
		t.Fatal("accepted changed symbol package")
	}
}

func TestSelectEntriesIsPolicyChokepoint(t *testing.T) {
	policy := authenticodePolicy{Include: []string{"lib/Yubico.Core.dll"}, FirstParty: []string{"Yubico.*.dll"}}
	tests := []struct {
		name    string
		entries []zipItem
		want    string
	}{
		{"unmatched", []zipItem{{"data.txt", []byte("x")}}, "unmatched include"},
		{"first party unselected", []zipItem{{"lib/Yubico.Core.dll", minimalPE()}, {"lib/Yubico.Other.dll", minimalPE()}}, "first-party DLL is not selected"},
		{"include case mismatch", []zipItem{{"lib/yubico.core.dll", minimalPE()}}, "unmatched include"},
		{"case collision", []zipItem{{"lib/Yubico.Core.dll", minimalPE()}, {"LIB/YUBICO.CORE.DLL", minimalPE()}}, "case-insensitive"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			filename := writeZip(t, t.TempDir(), "test.nupkg", test.entries)
			_, err := selectEntries(filename, policy)
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestInspectPackageRequiresOneRootNuspecAndUniqueIdentityValues(t *testing.T) {
	tests := []struct {
		name, nuspecName, body, want string
	}{
		{"nested", "nested/a.nuspec", `<package><metadata><id>A</id><version>1</version></metadata></package>`, "root-level"},
		{"duplicate id", "a.nuspec", `<package><metadata><id>A</id><id>B</id><version>1</version></metadata></package>`, "exactly one id"},
		{"duplicate version", "a.nuspec", `<package><metadata><id>A</id><version>1</version><version>2</version></metadata></package>`, "exactly one version"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			filename := writeZip(t, t.TempDir(), "a.nupkg", []zipItem{{test.nuspecName, []byte(test.body)}})
			_, err := inspectPackage(extractedPackage{Path: filename, Name: filepath.Base(filename)})
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func rawEntry(t *testing.T, p, name string) []byte {
	t.Helper()
	z, err := zip.OpenReader(p)
	if err != nil {
		t.Fatal(err)
	}
	defer z.Close()
	for _, f := range z.File {
		if f.Name == name {
			r, err := f.OpenRaw()
			if err != nil {
				t.Fatal(err)
			}
			b := new(bytes.Buffer)
			if _, err := b.ReadFrom(r); err != nil {
				t.Fatal(err)
			}
			return b.Bytes()
		}
	}
	t.Fatal("missing entry")
	return nil
}
func mustAssembly(t *testing.T, b []byte) *assembly.File {
	t.Helper()
	f, err := assembly.Open(b)
	if err != nil {
		t.Fatal(err)
	}
	return f
}
func testIdentity(t *testing.T) (*rsa.PrivateKey, *x509.Certificate) {
	t.Helper()
	k, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	now := time.Now()
	tmpl := &x509.Certificate{SerialNumber: big.NewInt(1), Subject: pkix.Name{CommonName: "test"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning}}
	der, err := x509.CreateCertificate(rand.Reader, tmpl, tmpl, &k.PublicKey, k)
	if err != nil {
		t.Fatal(err)
	}
	c, err := x509.ParseCertificate(der)
	if err != nil {
		t.Fatal(err)
	}
	return k, c
}
func minimalPE() []byte {
	b := make([]byte, 512)
	copy(b, "MZ")
	binary.LittleEndian.PutUint32(b[0x3c:], 0x80)
	copy(b[0x80:], "PE\x00\x00")
	coff := b[0x84:]
	binary.LittleEndian.PutUint16(coff, 0x14c)
	binary.LittleEndian.PutUint16(coff[16:], 224)
	binary.LittleEndian.PutUint16(coff[18:], 0x2102)
	opt := b[0x98:]
	binary.LittleEndian.PutUint16(opt, 0x10b)
	binary.LittleEndian.PutUint32(opt[32:], 0x1000)
	binary.LittleEndian.PutUint32(opt[36:], 0x200)
	binary.LittleEndian.PutUint32(opt[56:], 0x1000)
	binary.LittleEndian.PutUint32(opt[60:], 0x200)
	binary.LittleEndian.PutUint32(opt[92:], 16)
	return b
}
