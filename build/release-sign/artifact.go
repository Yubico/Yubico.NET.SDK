package main

import (
	"archive/zip"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strings"
)

const maxPackageSize = int64(1 << 30)

type extractedPackage struct {
	Path         string
	Name         string
	ArtifactPath string
}

type commandRunner interface {
	Run(context.Context, string, ...string) ([]byte, error)
	RunAttached(context.Context, string, ...string) error
}

func (osRunner) RunAttached(ctx context.Context, name string, args ...string) error {
	command := exec.CommandContext(ctx, name, args...)
	command.Stdin = os.Stdin
	command.Stdout = os.Stdout
	command.Stderr = os.Stderr
	return command.Run()
}

type osRunner struct{}

func (osRunner) Run(ctx context.Context, name string, args ...string) ([]byte, error) {
	command := exec.CommandContext(ctx, name, args...)
	output, err := command.Output()
	if err != nil {
		var exit *exec.ExitError
		if errors.As(err, &exit) {
			return output, fmt.Errorf("%s: %w: %s", name, err, strings.TrimSpace(string(exit.Stderr)))
		}
		return output, err
	}
	return output, nil
}

func resolveArtifact(work, name string) (string, error) {
	if !filepath.IsAbs(name) {
		name = filepath.Join(work, name)
	}
	return filepath.Abs(name)
}

func extractArtifacts(work string, artifacts []string, destination string) ([]extractedPackage, error) {
	if err := os.RemoveAll(destination); err != nil {
		return nil, err
	}
	if err := os.MkdirAll(destination, 0o700); err != nil {
		return nil, err
	}
	var result []extractedPackage
	packageNames := map[string]string{}
	for _, supplied := range artifacts {
		artifact, err := resolveArtifact(work, supplied)
		if err != nil {
			return nil, err
		}
		archive, err := zip.OpenReader(artifact)
		if err != nil {
			return nil, fmt.Errorf("open artifact %s: %w", artifact, err)
		}
		count := 0
		seen := map[string]string{}
		for _, entry := range archive.File {
			if err := validateEntryName(entry.Name); err != nil {
				archive.Close()
				return nil, fmt.Errorf("artifact %s has unsafe ZIP entry %q: %w", artifact, entry.Name, err)
			}
			folded := strings.ToLower(entry.Name)
			if previous, ok := seen[folded]; ok {
				archive.Close()
				return nil, fmt.Errorf("artifact %s has case-insensitive ZIP collision: %s and %s", artifact, previous, entry.Name)
			}
			seen[folded] = entry.Name
			ext := strings.ToLower(filepath.Ext(entry.Name))
			if ext != ".nupkg" && ext != ".snupkg" {
				continue
			}
			if entry.UncompressedSize64 > uint64(maxPackageSize) {
				archive.Close()
				return nil, fmt.Errorf("package %s exceeds 1 GiB limit", entry.Name)
			}
			base := filepath.Base(entry.Name)
			baseFolded := strings.ToLower(base)
			if previous, ok := packageNames[baseFolded]; ok {
				archive.Close()
				return nil, fmt.Errorf("duplicate package filename %s in %s and %s", base, previous, artifact)
			}
			packageNames[baseFolded] = artifact
			output := filepath.Join(destination, base)
			if err := extractEntryExclusive(entry, output); err != nil {
				archive.Close()
				return nil, err
			}
			result = append(result, extractedPackage{Path: output, Name: base, ArtifactPath: artifact})
			count++
		}
		if err := archive.Close(); err != nil {
			return nil, err
		}
		if count == 0 {
			return nil, fmt.Errorf("artifact %s contains no NuGet packages", artifact)
		}
	}
	sort.Slice(result, func(i, j int) bool { return strings.ToLower(result[i].Name) < strings.ToLower(result[j].Name) })
	return result, nil
}

func extractEntryExclusive(entry *zip.File, output string) error {
	reader, err := entry.Open()
	if err != nil {
		return err
	}
	defer reader.Close()
	file, err := os.OpenFile(output, os.O_CREATE|os.O_EXCL|os.O_WRONLY, 0o600)
	if err != nil {
		return err
	}
	ok := false
	defer func() {
		file.Close()
		if !ok {
			os.Remove(output)
		}
	}()
	written, err := io.Copy(file, io.LimitReader(reader, maxPackageSize+1))
	if err != nil {
		return err
	}
	if written > maxPackageSize {
		return errors.New("package exceeds 1 GiB limit")
	}
	if err := file.Close(); err != nil {
		return err
	}
	ok = true
	return nil
}

func attestPackages(ctx context.Context, runner commandRunner, repo, workflow, sourceDigest string, packages []extractedPackage) error {
	for i := range packages {
		output, err := runner.Run(ctx, "gh", "attestation", "verify", packages[i].Path,
			"--repo", repo,
			"--signer-workflow", workflow,
			"--predicate-type", "https://slsa.dev/provenance/v1",
			"--deny-self-hosted-runners",
			"--source-digest", sourceDigest,
			"--format", "json")
		if err != nil {
			return fmt.Errorf("verify attestation for %s: %w", packages[i].Name, err)
		}
		if !attestationMatches(output, packages[i].Path) {
			return fmt.Errorf("verify attestation for %s: gh returned invalid or empty JSON", packages[i].Name)
		}
	}
	return nil
}

func attestationMatches(output []byte, filename string) bool {
	localDigest, err := sha256File(filename)
	if err != nil {
		return false
	}
	var value any
	if len(strings.TrimSpace(string(output))) == 0 || json.Unmarshal(output, &value) != nil {
		return false
	}
	return containsAttestedSHA256(value, localDigest)
}

func containsAttestedSHA256(value any, digest string) bool {
	switch item := value.(type) {
	case []any:
		for _, result := range item {
			if record, ok := result.(map[string]any); ok && verificationResultMatches(record, digest) {
				return true
			}
		}
	case map[string]any:
		return verificationResultMatches(item, digest)
	}
	return false
}

func verificationResultMatches(record map[string]any, digest string) bool {
	verification, ok := record["verificationResult"].(map[string]any)
	if !ok {
		return false
	}
	statement, ok := verification["statement"].(map[string]any)
	if !ok {
		return false
	}
	subjects, ok := statement["subject"].([]any)
	if !ok {
		return false
	}
	for _, subjectValue := range subjects {
		subject, _ := subjectValue.(map[string]any)
		digests, _ := subject["digest"].(map[string]any)
		if actual, _ := digests["sha256"].(string); actual == digest {
			return true
		}
	}
	return false
}
