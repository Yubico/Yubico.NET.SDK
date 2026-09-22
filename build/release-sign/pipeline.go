package main

import (
	"context"
	"crypto/x509"
	"encoding/hex"
	"errors"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strings"
	"time"
)

type runConfig struct {
	Component           string
	WorkingDirectory    string
	Artifacts           []string
	ManifestPath        string
	KeyLocation         string
	CertificatePath     string
	RootPath            string
	TimestampRootPath   string
	IndependentVerifier string
	SourceDigest        string
	Timestamper         string
	Clean               bool
}

func run(ctx context.Context, cfg runConfig, runner commandRunner, acquire signerAcquirer) error {
	started := time.Now().UTC()
	m, manifestHash, certificates, roots, timestampRoots, err := validateRun(&cfg)
	if err != nil {
		return err
	}
	unsignedDirectory := filepath.Join(cfg.WorkingDirectory, "scratch", "unsigned")
	extracted, err := extractArtifacts(cfg.WorkingDirectory, cfg.Artifacts, unsignedDirectory)
	if err != nil {
		return err
	}
	if err := attestPackages(ctx, runner, m.AttestationRepo, m.SignerWorkflow, cfg.SourceDigest, extracted, func() error { return nil }); err != nil {
		return err
	}
	for i := range extracted {
		extracted[i].Attested = true
	}
	packages := make([]packageInfo, 0, len(extracted))
	for _, item := range extracted {
		info, err := inspectPackage(item)
		if err != nil {
			return err
		}
		packages = append(packages, info)
	}
	packages, err = planPackages(m, packages)
	if err != nil {
		return err
	}
	signer, err := acquire(ctx, cfg.KeyLocation, certificates, roots)
	if err != nil {
		return err
	}
	defer signer.Close()

	staging, cleanupStaging, err := createRunStaging(cfg.WorkingDirectory)
	if err != nil {
		return err
	}
	defer cleanupStaging()
	result := report{
		Schema:            1,
		Component:         cfg.Component,
		SourceDigest:      cfg.SourceDigest,
		SignerWorkflow:    m.SignerWorkflow,
		StartedUTC:        started,
		Manifest:          reportManifest{Path: cfg.ManifestPath, SHA256: manifestHash},
		KeyLocation:       safeKeyLocation(cfg.KeyLocation),
		SignerSubject:     signer.Leaf.Subject.String(),
		SignerFingerprint: certificateFingerprint(signer.Leaf),
	}
	artifactSet := map[string]bool{}
	for _, item := range extracted {
		artifactSet[item.ArtifactPath] = true
	}
	for name := range artifactSet {
		digest, err := sha256File(name)
		if err != nil {
			return err
		}
		result.Artifacts = append(result.Artifacts, reportArtifact{Path: name, SHA256: digest})
	}
	var outputNames []string
	for _, info := range packages {
		packageCtx, cancel := context.WithTimeout(ctx, 5*time.Minute)
		output := filepath.Join(staging, info.Name)
		selected := map[string]struct{}{}
		if info.Kind == "nupkg" {
			selected, err = selectEntries(info.Path, *info.Policy.Authenticode)
		}
		var expected map[string][]byte
		if err == nil {
			expected, err = signPackage(packageCtx, info, output, cfg.Timestamper, signer, selected)
		}
		if err == nil {
			err = verifySignedPackage(packageCtx, info, output, selected, expected, roots, timestampRoots, signer.Leaf)
		}
		if err == nil && info.Kind == "nupkg" {
			err = runIndependentVerifier(packageCtx, runner, cfg, output, selected, signer.Leaf)
		}
		cancel()
		if err != nil {
			return fmt.Errorf("process %s: %w", info.Name, err)
		}
		inputHash, err := sha256File(info.Path)
		if err != nil {
			return err
		}
		outputHash, err := sha256File(output)
		if err != nil {
			return err
		}
		assemblies := make([]string, 0, len(selected))
		for name := range selected {
			assemblies = append(assemblies, name)
		}
		sort.Strings(assemblies)
		result.Packages = append(result.Packages, reportPackage{ID: info.ID, Version: info.Version, Kind: info.Kind, Filename: info.Name, InputSHA256: inputHash, OutputSHA256: outputHash, SelectedAssemblies: assemblies, AttestationVerified: info.Attested})
		outputNames = append(outputNames, info.Name)
	}
	result.CompletedUTC = time.Now().UTC()
	reportContents, err := marshalReport(result)
	if err != nil {
		return err
	}
	return commitOutputs(filepath.Join(cfg.WorkingDirectory, "signed"), staging, outputNames, reportContents, cfg.Clean)
}

func validateRun(cfg *runConfig) (manifest, string, []*x509.Certificate, *x509.CertPool, *x509.CertPool, error) {
	if cfg.Component == "" || cfg.WorkingDirectory == "" || len(cfg.Artifacts) == 0 || cfg.ManifestPath == "" || cfg.KeyLocation == "" || cfg.CertificatePath == "" || cfg.RootPath == "" || cfg.TimestampRootPath == "" || cfg.IndependentVerifier == "" || cfg.SourceDigest == "" {
		return manifest{}, "", nil, nil, nil, errors.New("all required run flags must be provided")
	}
	if len(cfg.SourceDigest) != 40 {
		return manifest{}, "", nil, nil, nil, errors.New("--source-digest must be exactly 40 hexadecimal characters")
	}
	if _, err := hex.DecodeString(cfg.SourceDigest); err != nil {
		return manifest{}, "", nil, nil, nil, errors.New("--source-digest must be exactly 40 hexadecimal characters")
	}
	if strings.TrimSpace(cfg.Timestamper) == "" {
		return manifest{}, "", nil, nil, nil, errors.New("--timestamper must not be empty")
	}
	if err := os.MkdirAll(cfg.WorkingDirectory, 0o700); err != nil {
		return manifest{}, "", nil, nil, nil, err
	}
	if _, err := exec.LookPath("gh"); err != nil {
		return manifest{}, "", nil, nil, nil, errors.New("gh executable is unavailable")
	}
	verifier, err := exec.LookPath(cfg.IndependentVerifier)
	if err != nil {
		return manifest{}, "", nil, nil, nil, fmt.Errorf("independent verifier is unavailable: %w", err)
	}
	verifier, err = filepath.Abs(verifier)
	if err != nil {
		return manifest{}, "", nil, nil, nil, err
	}
	cfg.IndependentVerifier = verifier
	signedDirectory := filepath.Join(cfg.WorkingDirectory, "signed")
	if _, err := os.Lstat(signedDirectory); err == nil && !cfg.Clean {
		return manifest{}, "", nil, nil, nil, errors.New("refusing to replace existing signed directory without --clean")
	} else if err != nil && !errors.Is(err, os.ErrNotExist) {
		return manifest{}, "", nil, nil, nil, err
	}
	packagesDir := filepath.Join(cfg.WorkingDirectory, "signed", "packages")
	if entries, err := os.ReadDir(packagesDir); err == nil && len(entries) != 0 && !cfg.Clean {
		return manifest{}, "", nil, nil, nil, errors.New("refusing to overwrite nonempty signed/packages without --clean")
	} else if err != nil && !errors.Is(err, os.ErrNotExist) {
		return manifest{}, "", nil, nil, nil, err
	}
	m, hash, err := loadManifest(cfg.ManifestPath, cfg.Component)
	if err != nil {
		return manifest{}, "", nil, nil, nil, err
	}
	certificates, err := readCertificates(cfg.CertificatePath)
	if err != nil {
		return manifest{}, "", nil, nil, nil, fmt.Errorf("read certificate chain: %w", err)
	}
	roots, err := trustAnchorPool(cfg.RootPath)
	if err != nil {
		return manifest{}, "", nil, nil, nil, fmt.Errorf("read root: %w", err)
	}
	timestampRoots, err := trustAnchorPool(cfg.TimestampRootPath)
	if err != nil {
		return manifest{}, "", nil, nil, nil, fmt.Errorf("read timestamp root: %w", err)
	}
	return m, hash, certificates, roots, timestampRoots, nil
}

func runIndependentVerifier(ctx context.Context, runner commandRunner, cfg runConfig, output string, selected map[string]struct{}, signer *x509.Certificate) error {
	args := []string{"--package", output, "--root", cfg.RootPath, "--timestamp-root", cfg.TimestampRootPath, "--signer-fingerprint", certificateFingerprint(signer)}
	names := make([]string, 0, len(selected))
	for name := range selected {
		names = append(names, name)
	}
	sort.Strings(names)
	for _, name := range names {
		args = append(args, "--assembly", name)
	}
	verifierOutput, err := runner.Run(ctx, cfg.IndependentVerifier, args...)
	if err != nil {
		return fmt.Errorf("independent verifier: %w", err)
	}
	expectedFingerprint := certificateFingerprint(signer)
	seen := make(map[string]bool, len(names))
	for _, line := range strings.Split(strings.TrimSpace(string(verifierOutput)), "\n") {
		fields := strings.Fields(line)
		if len(fields) < 4 || fields[0] != "PASS" {
			return fmt.Errorf("independent verifier returned an unexpected line: %q", line)
		}
		name := fields[1]
		if _, ok := selected[name]; !ok || seen[name] {
			return fmt.Errorf("independent verifier returned an unexpected or duplicate assembly: %s", name)
		}
		if !strings.Contains(line, " image=SHA-256 ") || !strings.Contains(line, " signer="+expectedFingerprint+" ") {
			return fmt.Errorf("independent verifier did not confirm SHA-256 and the expected signer for %s", name)
		}
		seen[name] = true
	}
	if len(seen) != len(selected) {
		return fmt.Errorf("independent verifier confirmed %d of %d assemblies", len(seen), len(selected))
	}
	return nil
}

func createRunStaging(work string) (string, func(), error) {
	scratch := filepath.Join(work, "scratch")
	if err := os.MkdirAll(scratch, 0o700); err != nil {
		return "", nil, err
	}
	directory, err := os.MkdirTemp(scratch, "release-sign-signed-")
	if err != nil {
		return "", nil, err
	}
	return directory, func() { _ = os.RemoveAll(directory) }, nil
}
