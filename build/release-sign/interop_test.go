package main

import (
	"bytes"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/pem"
	"math/big"
	"os"
	"os/exec"
	"path/filepath"
	"testing"
	"time"
)

// Run with RELEASE_SIGN_TEST_NUGET_SIGN pointing at the companion PR's binary.
// OpenSSL creates a disposable encrypted fixture; no real token is used.
func TestNugetSignPrivateInputInterop(t *testing.T) {
	tool := os.Getenv("RELEASE_SIGN_TEST_NUGET_SIGN")
	if tool == "" {
		t.Skip("set RELEASE_SIGN_TEST_NUGET_SIGN to run the external-tool integration")
	}
	if _, err := inspectNugetSign(t.Context(), osRunner{}, tool); err != nil {
		t.Fatal(err)
	}
	dir := t.TempDir()
	key, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	now := time.Now()
	template := &x509.Certificate{SerialNumber: big.NewInt(1), Subject: pkix.Name{CommonName: "private-pipe interop"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning}}
	der, err := x509.CreateCertificate(rand.Reader, template, template, &key.PublicKey, key)
	if err != nil {
		t.Fatal(err)
	}
	certificate, privateKey, pfx := filepath.Join(dir, "cert.pem"), filepath.Join(dir, "key.pem"), filepath.Join(dir, "key.pfx")
	if err := os.WriteFile(certificate, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: der}), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(privateKey, pem.EncodeToMemory(&pem.Block{Type: "RSA PRIVATE KEY", Bytes: x509.MarshalPKCS1PrivateKey(key)}), 0o600); err != nil {
		t.Fatal(err)
	}
	password := []byte("private-pipe-test")
	defer clear(password)
	export := exec.CommandContext(t.Context(), "openssl", "pkcs12", "-export", "-out", pfx, "-inkey", privateKey, "-in", certificate, "-passout", "stdin")
	export.Stdin = bytes.NewReader(password)
	if output, err := export.CombinedOutput(); err != nil {
		t.Fatalf("fixture: %v: %s", err, output)
	}
	paths := []string{}
	for _, name := range []string{"A.1.nupkg", "B.1.nupkg"} {
		paths = append(paths, writeZip(t, dir, name, []zipItem{{"test.nuspec", []byte("<package><metadata><id>Test</id><version>1.0.0</version></metadata></package>")}}))
	}
	args := append([]string{"sign", "--key", pfx, "--certificate", certificate, "--password-stdin", "--timestamper", ""}, paths...)
	// This is the same raw-byte stdin transport used for the PIV PIN, exercised
	// against a real provider that must consume the secret to sign successfully.
	if err := (osRunner{}).RunAttached(t.Context(), tool, password, args...); err != nil {
		t.Fatal(err)
	}
	args = append([]string{"verify", "--root", certificate, "--revocation", "none"}, paths...)
	if output, err := (osRunner{}).Run(t.Context(), tool, args...); err != nil {
		t.Fatalf("verification: %v: %s", err, output)
	}
}
