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
	Component         string
	WorkingDirectory  string
	Artifacts         []string
	ManifestPath      string
	KeyLocation       string
	CertificatePath   string
	RootPath          string
	TimestampRootPath string
	NugetSign         string
	SourceDigest      string
	Timestamper       string
	Clean             bool
	signingPIN        string
}

func run(ctx context.Context, cfg runConfig, runner commandRunner) error {
	started := time.Now().UTC()
	m, manifestHash, leaf, tool, err := validateRun(ctx, &cfg, runner)
	if err != nil {
		return err
	}
	fmt.Fprintln(os.Stderr, "Checking build provenance and package policy; the PIN prompt follows...")
	unsignedDirectory := filepath.Join(cfg.WorkingDirectory, "scratch", "unsigned")
	extracted, err := extractArtifacts(cfg.WorkingDirectory, cfg.Artifacts, unsignedDirectory)
	if err != nil {
		return err
	}
	if err := attestPackages(ctx, runner, m.AttestationRepo, m.SignerWorkflow, cfg.SourceDigest, extracted); err != nil {
		return err
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

	staging, cleanupStaging, err := createRunStaging(cfg.WorkingDirectory)
	if err != nil {
		return err
	}
	defer cleanupStaging()
	fingerprint := certificateFingerprint(leaf)
	result := report{
		Schema: 1, Component: cfg.Component, SourceDigest: cfg.SourceDigest,
		SignerWorkflow: m.SignerWorkflow, StartedUTC: started,
		Manifest:    reportManifest{Path: cfg.ManifestPath, SHA256: manifestHash},
		KeyLocation: safeKeyLocation(cfg.KeyLocation), SignerSubject: leaf.Subject.String(),
		SignerFingerprint: fingerprint, Tool: tool,
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
	cfg.signingPIN, err = acquireSigningPIN(cfg.KeyLocation, func() (string, error) {
		return promptSigningPIN(cfg.KeyLocation)
	})
	if err != nil {
		return err
	}
	var outputNames []string
	for _, info := range packages {
		packageCtx, cancel := context.WithTimeout(ctx, 5*time.Minute)
		output := filepath.Join(staging, info.Name)
		selected, err := signAndVerifyPackage(packageCtx, runner, cfg, info, output, fingerprint)
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
		result.Packages = append(result.Packages, reportPackage{ID: info.ID, Version: info.Version, Kind: info.Kind, Filename: info.Name, InputSHA256: inputHash, OutputSHA256: outputHash, SelectedAssemblies: assemblies})
		outputNames = append(outputNames, info.Name)
	}
	result.CompletedUTC = time.Now().UTC()
	reportContents, err := marshalReport(result)
	if err != nil {
		return err
	}
	return commitOutputs(filepath.Join(cfg.WorkingDirectory, "signed"), staging, outputNames, reportContents, cfg.Clean)
}

func signAndVerifyPackage(ctx context.Context, runner commandRunner, cfg runConfig, info packageInfo, output, fingerprint string) (map[string]struct{}, error) {
	selected := map[string]struct{}{}
	if info.Kind == "nupkg" {
		var err error
		selected, err = selectEntries(info.entries, *info.Policy.Authenticode)
		if err != nil {
			return nil, err
		}
	}
	if err := signPackage(ctx, runner, cfg, info, output, selected); err != nil {
		return nil, err
	}
	if err := verifySignedPackage(ctx, runner, cfg, info, output, selected, fingerprint); err != nil {
		return nil, err
	}
	return selected, nil
}

func validateRun(ctx context.Context, cfg *runConfig, runner commandRunner) (manifest, string, *x509.Certificate, reportTool, error) {
	empty := func(err error) (manifest, string, *x509.Certificate, reportTool, error) {
		return manifest{}, "", nil, reportTool{}, err
	}
	if cfg.Component == "" || cfg.WorkingDirectory == "" || len(cfg.Artifacts) == 0 || cfg.ManifestPath == "" || cfg.KeyLocation == "" || cfg.CertificatePath == "" || cfg.RootPath == "" || cfg.TimestampRootPath == "" || cfg.NugetSign == "" || cfg.SourceDigest == "" {
		return empty(errors.New("all required run flags must be provided"))
	}
	if len(cfg.SourceDigest) != 40 {
		return empty(errors.New("--source-digest must be exactly 40 hexadecimal characters"))
	}
	if _, err := hex.DecodeString(cfg.SourceDigest); err != nil {
		return empty(errors.New("--source-digest must be exactly 40 hexadecimal characters"))
	}
	if strings.TrimSpace(cfg.Timestamper) == "" {
		return empty(errors.New("--timestamper must not be empty"))
	}
	if err := os.MkdirAll(cfg.WorkingDirectory, 0o700); err != nil {
		return empty(err)
	}
	nugetTool, err := inspectNugetSign(ctx, runner, cfg.NugetSign)
	if err != nil {
		return empty(fmt.Errorf("check nuget-sign: %w", err))
	}
	cfg.NugetSign = nugetTool.Path

	signedDirectory := filepath.Join(cfg.WorkingDirectory, "signed")
	if _, err := os.Lstat(signedDirectory); err == nil && !cfg.Clean {
		return empty(errors.New("refusing to replace existing signed directory without --clean"))
	} else if err != nil && !errors.Is(err, os.ErrNotExist) {
		return empty(err)
	}
	m, hash, err := loadManifest(cfg.ManifestPath, cfg.Component)
	if err != nil {
		return empty(err)
	}
	leaf, err := readLeafCertificate(cfg.CertificatePath)
	if err != nil {
		return empty(fmt.Errorf("read signing certificate: %w", err))
	}
	return m, hash, leaf, nugetTool, nil
}

func inspectNugetSign(ctx context.Context, runner commandRunner, name string) (reportTool, error) {
	resolved, err := exec.LookPath(name)
	if err != nil {
		return reportTool{}, fmt.Errorf("%s is unavailable: %w", name, err)
	}
	resolved, err = filepath.Abs(resolved)
	if err != nil {
		return reportTool{}, err
	}
	output, err := runner.Run(ctx, resolved, "--version")
	if err != nil {
		return reportTool{}, err
	}
	if !strings.HasPrefix(string(output), "nuget-sign version ") {
		return reportTool{}, fmt.Errorf("%s is not nuget-sign: %q", resolved, strings.TrimSpace(string(output)))
	}
	digest, err := sha256File(resolved)
	if err != nil {
		return reportTool{}, err
	}
	return reportTool{Path: resolved, Version: strings.TrimSpace(string(output)), SHA256: digest}, nil
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
