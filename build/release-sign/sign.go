package main

import (
	"archive/zip"
	"bytes"
	"context"
	"crypto"
	"crypto/rsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/binary"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"io"
	"os"
	"strings"
	"time"

	"github.com/Yubico/nuget-sign/pkg/assembly"
	"github.com/Yubico/nuget-sign/pkg/keys"
	_ "github.com/Yubico/nuget-sign/pkg/keys/piv"
	"github.com/Yubico/nuget-sign/pkg/nuget"
	"golang.org/x/term"
)

type signerHandle struct {
	Signer       crypto.Signer
	Certificates []*x509.Certificate
	Leaf         *x509.Certificate
	Close        func() error
}

type signerAcquirer func(context.Context, string, []*x509.Certificate, *x509.CertPool) (*signerHandle, error)

func acquireProductionSigner(ctx context.Context, location string, certificates []*x509.Certificate, roots *x509.CertPool) (*signerHandle, error) {
	var cachedPIN string
	var havePIN bool
	session := keys.NewSession(keys.Config{Secret: func(kind keys.SecretKind, _ string) (string, error) {
		if kind != keys.PIN {
			return "", fmt.Errorf("unexpected secret request for %s", kind)
		}
		if !havePIN {
			pin, err := readPIN()
			if err != nil {
				return "", err
			}
			cachedPIN, havePIN = pin, true
		}
		return cachedPIN, nil
	}})
	pair, err := session.Key(ctx, location)
	if err != nil {
		session.Close()
		return nil, err
	}
	leaf, err := signingCertificate(certificates, pair.Signer)
	if err != nil {
		session.Close()
		return nil, err
	}
	if err := validateSigningCertificate(leaf, certificates, roots); err != nil {
		session.Close()
		return nil, err
	}
	emitted := []*x509.Certificate{leaf}
	for _, certificate := range certificates {
		if certificate.Equal(leaf) || isSelfSigned(certificate) {
			continue
		}
		emitted = append(emitted, certificate)
	}
	return &signerHandle{Signer: pair.Signer, Certificates: emitted, Leaf: leaf, Close: session.Close}, nil
}

func validateSigningCertificate(leaf *x509.Certificate, certificates []*x509.Certificate, roots *x509.CertPool) error {
	intermediates := x509.NewCertPool()
	for _, certificate := range certificates {
		if !certificate.Equal(leaf) && !isSelfSigned(certificate) {
			intermediates.AddCert(certificate)
		}
	}
	_, err := leaf.Verify(x509.VerifyOptions{
		Roots:         roots,
		Intermediates: intermediates,
		CurrentTime:   time.Now(),
		KeyUsages:     []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning},
	})
	if err != nil {
		return fmt.Errorf("validate signing certificate: %w", err)
	}
	return nil
}

func readPIN() (string, error) {
	fd := int(os.Stdin.Fd())
	if term.IsTerminal(fd) {
		fmt.Fprint(os.Stderr, "Enter PIV PIN: ")
		pin, err := term.ReadPassword(fd)
		fmt.Fprintln(os.Stderr)
		return string(pin), err
	}
	if pin := os.Getenv("NUGET_SIGN_PIN"); pin != "" {
		return pin, nil
	}
	return "", errors.New("no terminal available for the PIV PIN; set NUGET_SIGN_PIN for unattended use")
}

func signingCertificate(certificates []*x509.Certificate, signer crypto.Signer) (*x509.Certificate, error) {
	public, ok := signer.Public().(*rsa.PublicKey)
	if !ok {
		return nil, fmt.Errorf("signing key is %T, but RSA is required", signer.Public())
	}
	for _, certificate := range certificates {
		key, ok := certificate.PublicKey.(*rsa.PublicKey)
		if ok && key.Equal(public) {
			return certificate, nil
		}
	}
	return nil, errors.New("no certificate matches the signing key")
}

func isSelfSigned(certificate *x509.Certificate) bool {
	return bytes.Equal(certificate.RawSubject, certificate.RawIssuer) && certificate.CheckSignatureFrom(certificate) == nil
}

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
		if block == nil || block.Type != "CERTIFICATE" {
			return nil, errors.New("certificate file contains invalid PEM data")
		}
		contents = rest
		certificate, err := x509.ParseCertificate(block.Bytes)
		if err != nil {
			return nil, err
		}
		certificates = append(certificates, certificate)
	}
	if len(certificates) == 0 {
		return nil, errors.New("certificate file contains no certificates")
	}
	return certificates, nil
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

func packageSigned(filename string) (bool, error) {
	file, size, err := openFile(filename)
	if err != nil {
		return false, err
	}
	defer file.Close()
	pkg, err := nuget.Open(file, size)
	if err != nil {
		return false, err
	}
	return pkg.Signed(), nil
}

func signPE(ctx context.Context, contents []byte, options assembly.SignOptions, existingPolicy string) ([]byte, error) {
	file, err := assembly.Open(contents)
	if err != nil {
		return nil, err
	}
	if file.Signed() && existingPolicy == "reject" {
		return nil, errors.New("selected DLL already has a PE certificate table")
	}
	options.Hash = crypto.SHA256
	signed, err := file.Sign(ctx, options)
	if err != nil {
		return nil, err
	}
	if err := verifyAuthenticodeOnlyMutation(contents, signed); err != nil {
		return nil, fmt.Errorf("signing changed bytes outside Authenticode fields: %w", err)
	}
	signature, err := assemblySignature(signed)
	if err != nil {
		return nil, err
	}
	if signature.Hash != crypto.SHA256 || !signature.Matches() {
		return nil, errors.New("new Authenticode SHA-256 image digest does not match")
	}
	return signed, nil
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
	payloadEnd := len(before)
	if want.certificateLen != 0 {
		payloadEnd = want.certificateAt
	}
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

func signPackage(ctx context.Context, info packageInfo, output, timestamper string, signer *signerHandle, selected map[string]struct{}) (map[string][]byte, error) {
	signed, err := packageSigned(info.Path)
	if err != nil {
		return nil, err
	}
	if signed {
		return nil, fmt.Errorf("input package %s is already NuGet-signed", info.Name)
	}
	options := nuget.SignOptions{Certificates: signer.Certificates, Key: signer.Signer, Hash: crypto.SHA256, TimestampURL: timestamper}
	if info.Kind == "snupkg" {
		return nil, signNuGetContainer(ctx, info.Path, output, options)
	}
	rebuilt := output + ".assemblies"
	expected := make(map[string][]byte, len(selected))
	assemblyOptions := assembly.SignOptions{Certificates: signer.Certificates, Key: signer.Signer, Hash: crypto.SHA256, TimestampURL: timestamper}
	if err := rewritePackage(info.Path, rebuilt, selected, func(name string, contents []byte) ([]byte, error) {
		signedAssembly, err := signPE(ctx, contents, assemblyOptions, info.Policy.Authenticode.AlreadySigned)
		if err != nil {
			return nil, err
		}
		signature, err := assemblySignature(signedAssembly)
		if err != nil {
			return nil, err
		}
		expected[name] = append([]byte(nil), signature.ActualDigest...)
		return signedAssembly, nil
	}); err != nil {
		return nil, err
	}
	defer os.Remove(rebuilt)
	if err := signNuGetContainer(ctx, rebuilt, output, options); err != nil {
		return nil, err
	}
	return expected, nil
}

func signNuGetContainer(ctx context.Context, input, output string, options nuget.SignOptions) error {
	file, size, err := openFile(input)
	if err != nil {
		return err
	}
	defer file.Close()
	pkg, err := nuget.Open(file, size)
	if err != nil {
		return err
	}
	return writeNewFile(output, func(writer io.Writer) error { return pkg.Sign(ctx, writer, options) })
}

func assemblySignature(contents []byte) (*assembly.Signature, error) {
	file, err := assembly.Open(contents)
	if err != nil {
		return nil, err
	}
	return file.Signature()
}

func openFile(filename string) (*os.File, int64, error) {
	file, err := os.Open(filename)
	if err != nil {
		return nil, 0, err
	}
	info, err := file.Stat()
	if err != nil {
		file.Close()
		return nil, 0, err
	}
	return file, info.Size(), nil
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
