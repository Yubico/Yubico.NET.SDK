package main

import (
	"archive/zip"
	"bytes"
	"context"
	"crypto/rsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/binary"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"
)

func readCertificates(filename string) ([]*x509.Certificate, error) {
	contents, err := os.ReadFile(filename)
	if err != nil {
		return nil, err
	}
	var certificates []*x509.Certificate
	for len(bytes.TrimSpace(contents)) > 0 {
		contents = bytes.TrimSpace(contents)
		if !bytes.HasPrefix(contents, []byte("-----BEGIN CERTIFICATE-----")) {
			return nil, errors.New("certificate file contains data other than CERTIFICATE PEM blocks")
		}
		block, rest := pem.Decode(contents)
		if block == nil || block.Type != "CERTIFICATE" || len(block.Headers) != 0 {
			return nil, errors.New("certificate file contains invalid PEM data")
		}
		certificate, err := x509.ParseCertificate(block.Bytes)
		if err != nil {
			return nil, err
		}
		certificates = append(certificates, certificate)
		contents = rest
	}
	if len(certificates) == 0 {
		return nil, errors.New("certificate file contains no certificates")
	}
	return certificates, nil
}

func validateCertificateChain(certificates []*x509.Certificate, roots *x509.CertPool) (*x509.Certificate, error) {
	leaf := certificates[0]
	publicKey, ok := leaf.PublicKey.(*rsa.PublicKey)
	if !ok {
		return nil, fmt.Errorf("signing certificate key is %T, but RSA is required", leaf.PublicKey)
	}
	if publicKey.N.BitLen() < 2048 {
		return nil, fmt.Errorf("signing certificate RSA key is %d bits, want at least 2048", publicKey.N.BitLen())
	}
	intermediates := x509.NewCertPool()
	for _, certificate := range certificates[1:] {
		intermediates.AddCert(certificate)
	}
	if _, err := leaf.Verify(x509.VerifyOptions{
		Roots: roots, Intermediates: intermediates, CurrentTime: time.Now(),
		KeyUsages: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning},
	}); err != nil {
		return nil, fmt.Errorf("validate signing certificate: %w", err)
	}
	return leaf, nil
}

func trustAnchorPool(filename string) (*x509.CertPool, error) {
	certificates, err := readCertificates(filename)
	if err != nil {
		return nil, err
	}
	pool := x509.NewCertPool()
	for index, certificate := range certificates {
		if !certificate.BasicConstraintsValid || !certificate.IsCA {
			return nil, fmt.Errorf("certificate %d is not a CA with valid basic constraints", index+1)
		}
		if !bytes.Equal(certificate.RawSubject, certificate.RawIssuer) {
			return nil, fmt.Errorf("certificate %d is not self-issued", index+1)
		}
		if err := certificate.CheckSignatureFrom(certificate); err != nil {
			return nil, fmt.Errorf("certificate %d has an invalid self-signature: %w", index+1, err)
		}
		pool.AddCert(certificate)
	}
	return pool, nil
}

type peSigningLayout struct {
	checksum       int
	security       int
	certificateAt  int
	certificateLen int
}

func parsePESigningLayout(image []byte) (peSigningLayout, error) {
	const (
		lfanewOffset       = 0x3c
		coffOptionalSizeAt = 20
		optionalOffset     = 24
		checksumOffset     = 64
		pe32Directories    = 96
		pe64Directories    = 112
		securityIndex      = 4
		directorySize      = 8
	)
	if len(image) < lfanewOffset+4 || string(image[:2]) != "MZ" {
		return peSigningLayout{}, errors.New("missing MZ header")
	}
	peOffset := int(binary.LittleEndian.Uint32(image[lfanewOffset:]))
	if peOffset < 0 || peOffset+optionalOffset > len(image) || string(image[peOffset:peOffset+4]) != "PE\x00\x00" {
		return peSigningLayout{}, errors.New("missing PE header")
	}
	optional := peOffset + optionalOffset
	optionalSize := int(binary.LittleEndian.Uint16(image[peOffset+coffOptionalSizeAt:]))
	if optionalSize < pe32Directories || optional+optionalSize > len(image) {
		return peSigningLayout{}, errors.New("truncated optional header")
	}
	directoriesOffset := 0
	switch binary.LittleEndian.Uint16(image[optional:]) {
	case 0x10b:
		directoriesOffset = pe32Directories
	case 0x20b:
		directoriesOffset = pe64Directories
	default:
		return peSigningLayout{}, errors.New("unsupported optional header")
	}
	security := optional + directoriesOffset + securityIndex*directorySize
	checksum := optional + checksumOffset
	if security+directorySize > optional+optionalSize || checksum+4 > len(image) {
		return peSigningLayout{}, errors.New("missing Authenticode header fields")
	}
	certificateAt := int(binary.LittleEndian.Uint32(image[security:]))
	certificateLen := int(binary.LittleEndian.Uint32(image[security+4:]))
	if certificateLen != 0 && (certificateAt < 0 || certificateLen < 0 || certificateAt+certificateLen < certificateAt || certificateAt+certificateLen > len(image)) {
		return peSigningLayout{}, errors.New("invalid certificate table")
	}
	return peSigningLayout{checksum: checksum, security: security, certificateAt: certificateAt, certificateLen: certificateLen}, nil
}

func verifyAuthenticodeOnlyMutation(before, after []byte) error {
	want, err := parsePESigningLayout(before)
	if err != nil {
		return fmt.Errorf("parse input: %w", err)
	}
	got, err := parsePESigningLayout(after)
	if err != nil {
		return fmt.Errorf("parse output: %w", err)
	}
	if want.checksum != got.checksum || want.security != got.security {
		return errors.New("PE header layout changed")
	}
	if want.certificateLen != 0 {
		return errors.New("input already has a certificate table")
	}
	payloadEnd := len(before)
	if got.certificateLen == 0 || got.certificateAt < payloadEnd || got.certificateAt+got.certificateLen != len(after) {
		return errors.New("output certificate table is not appended after the executable image")
	}
	for i := 0; i < payloadEnd; i++ {
		if (i >= want.checksum && i < want.checksum+4) || (i >= want.security && i < want.security+8) {
			continue
		}
		if before[i] != after[i] {
			return fmt.Errorf("executable byte %d changed", i)
		}
	}
	for _, value := range after[payloadEnd:got.certificateAt] {
		if value != 0 {
			return errors.New("non-zero bytes were inserted before the certificate table")
		}
	}
	return nil
}

func signPackage(ctx context.Context, runner commandRunner, cfg runConfig, info packageInfo, output string, selected map[string]struct{}) error {
	if info.Kind == "snupkg" {
		return runNugetSign(ctx, runner, cfg, info.Path, output)
	}
	directory, err := os.MkdirTemp(filepath.Dir(output), ".release-sign-assemblies-")
	if err != nil {
		return err
	}
	defer os.RemoveAll(directory)
	names := make([]string, 0, len(selected))
	for name := range selected {
		names = append(names, name)
	}
	sort.Strings(names)
	originals := make(map[string][]byte, len(names))
	paths := make([]string, 0, len(names))
	for _, name := range names {
		contents, err := zipEntry(info.Path, name)
		if err != nil {
			return err
		}
		layout, err := parsePESigningLayout(contents)
		if err != nil {
			return fmt.Errorf("parse %s: %w", name, err)
		}
		if layout.certificateLen != 0 {
			return fmt.Errorf("selected DLL already has a PE certificate table: %s", name)
		}
		filename := filepath.Join(directory, filepath.FromSlash(name))
		if err := os.MkdirAll(filepath.Dir(filename), 0o700); err != nil {
			return err
		}
		if err := os.WriteFile(filename, contents, 0o600); err != nil {
			return err
		}
		originals[name], paths = contents, append(paths, filename)
	}
	args := []string{"sign-assemblies", "--key", cfg.KeyLocation, "--certificate", cfg.CertificatePath, "--hash-algorithm", "sha256", "--timestamper", cfg.Timestamper}
	args = append(args, paths...)
	if err := runner.RunAttached(ctx, cfg.NugetSign, signingEnvironment(cfg.signingPIN), args...); err != nil {
		return fmt.Errorf("nuget-sign sign-assemblies: %w", err)
	}
	signed := make(map[string][]byte, len(names))
	for i, name := range names {
		contents, err := os.ReadFile(paths[i])
		if err != nil {
			return err
		}
		if err := verifyAuthenticodeOnlyMutation(originals[name], contents); err != nil {
			return fmt.Errorf("signing changed bytes outside Authenticode fields for %s: %w", name, err)
		}
		signed[name] = contents
	}
	rebuilt := filepath.Join(directory, "rebuilt.nupkg")
	if err := rewritePackage(info.Path, rebuilt, selected, func(name string, _ []byte) ([]byte, error) { return signed[name], nil }); err != nil {
		return err
	}
	return runNugetSign(ctx, runner, cfg, rebuilt, output)
}

func runNugetSign(ctx context.Context, runner commandRunner, cfg runConfig, input, output string) error {
	args := []string{"sign", "--key", cfg.KeyLocation, "--certificate", cfg.CertificatePath, "--hash-algorithm", "sha256", "--timestamper", cfg.Timestamper, "--output", output, input}
	if err := runner.RunAttached(ctx, cfg.NugetSign, signingEnvironment(cfg.signingPIN), args...); err != nil {
		return fmt.Errorf("nuget-sign sign: %w", err)
	}
	return nil
}

func signingEnvironment(pin string) []string {
	if pin == "" {
		return nil
	}
	return []string{"NUGET_SIGN_PIN=" + pin}
}

func certificateFingerprint(certificate *x509.Certificate) string {
	digest := sha256.Sum256(certificate.Raw)
	return strings.ToUpper(hex.EncodeToString(digest[:]))
}

func zipEntry(filename, name string) ([]byte, error) {
	archive, err := zip.OpenReader(filename)
	if err != nil {
		return nil, err
	}
	defer archive.Close()
	for _, entry := range archive.File {
		if entry.Name == name {
			return readZipEntry(entry)
		}
	}
	return nil, fmt.Errorf("missing entry %s", name)
}
