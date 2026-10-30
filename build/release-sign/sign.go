package main

import (
	"archive/zip"
	"context"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
)

// readLeafCertificate returns the first certificate of a leaf-first PEM chain.
// The wrapper only needs its identity to pin the signer; nuget-sign validates
// the chain, the key match, and the resulting signatures.
func readLeafCertificate(filename string) (*x509.Certificate, error) {
	contents, err := os.ReadFile(filename)
	if err != nil {
		return nil, err
	}
	block, _ := pem.Decode(contents)
	if block == nil || block.Type != "CERTIFICATE" {
		return nil, errors.New("certificate file does not start with a CERTIFICATE PEM block")
	}
	return x509.ParseCertificate(block.Bytes)
}

func certificateFingerprint(certificate *x509.Certificate) string {
	digest := sha256.Sum256(certificate.Raw)
	return strings.ToUpper(hex.EncodeToString(digest[:]))
}

// signPackage signs the manifest-selected assemblies of an already packed
// package, repacks them in place of the originals, and author-signs the result.
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
	paths := make(map[string]string, len(names))
	args := []string{"sign-assemblies", "--key", cfg.KeyLocation, "--certificate", cfg.CertificatePath, "--hash-algorithm", "sha256", "--timestamper", cfg.Timestamper}
	for _, name := range names {
		contents, err := zipEntry(info.Path, name)
		if err != nil {
			return err
		}
		filename := filepath.Join(directory, filepath.FromSlash(name))
		if err := os.MkdirAll(filepath.Dir(filename), 0o700); err != nil {
			return err
		}
		if err := os.WriteFile(filename, contents, 0o600); err != nil {
			return err
		}
		paths[name] = filename
		args = append(args, filename)
	}
	if err := runner.RunAttached(ctx, cfg.NugetSign, signingEnvironment(cfg.signingPIN), args...); err != nil {
		return fmt.Errorf("nuget-sign sign-assemblies: %w", err)
	}
	rebuilt := filepath.Join(directory, "rebuilt.nupkg")
	if err := rewritePackage(info.Path, rebuilt, selected, func(name string, _ []byte) ([]byte, error) {
		return os.ReadFile(paths[name])
	}); err != nil {
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
