package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/url"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"
)

type report struct {
	Schema            int              `json:"schema"`
	Component         string           `json:"component"`
	SourceDigest      string           `json:"sourceDigest"`
	SignerWorkflow    string           `json:"signerWorkflow"`
	StartedUTC        time.Time        `json:"startedUtc"`
	CompletedUTC      time.Time        `json:"completedUtc"`
	Manifest          reportManifest   `json:"manifest"`
	KeyLocation       string           `json:"keyLocation"`
	SignerSubject     string           `json:"signerSubject"`
	SignerFingerprint string           `json:"signerFingerprintSha256"`
	Artifacts         []reportArtifact `json:"artifacts"`
	Packages          []reportPackage  `json:"packages"`
}

type reportArtifact struct {
	Path   string `json:"path"`
	SHA256 string `json:"sha256"`
}

type reportManifest struct {
	Path   string `json:"path"`
	SHA256 string `json:"sha256"`
}

type reportPackage struct {
	ID                  string   `json:"id"`
	Version             string   `json:"version"`
	Kind                string   `json:"kind"`
	Filename            string   `json:"filename"`
	InputSHA256         string   `json:"inputSha256"`
	OutputSHA256        string   `json:"outputSha256"`
	SelectedAssemblies  []string `json:"selectedAssemblies"`
	AttestationVerified bool     `json:"attestationVerified"`
}

func marshalReport(value report) ([]byte, error) {
	sort.Slice(value.Artifacts, func(i, j int) bool { return value.Artifacts[i].Path < value.Artifacts[j].Path })
	sort.Slice(value.Packages, func(i, j int) bool {
		if value.Packages[i].ID != value.Packages[j].ID {
			return value.Packages[i].ID < value.Packages[j].ID
		}
		return value.Packages[i].Kind < value.Packages[j].Kind
	})
	for i := range value.Packages {
		if value.Packages[i].SelectedAssemblies == nil {
			value.Packages[i].SelectedAssemblies = []string{}
		}
		sort.Strings(value.Packages[i].SelectedAssemblies)
	}
	contents, err := json.MarshalIndent(value, "", "  ")
	if err != nil {
		return nil, err
	}
	return append(contents, '\n'), nil
}

func safeKeyLocation(location string) string {
	parsed, err := url.Parse(location)
	if err != nil {
		return "redacted-invalid-key-location"
	}
	parsed.User = nil
	query := parsed.Query()
	for name := range query {
		folded := strings.ToLower(name)
		if strings.Contains(folded, "pin") || strings.Contains(folded, "pass") || strings.Contains(folded, "secret") || strings.Contains(folded, "token") || strings.Contains(folded, "credential") {
			query.Del(name)
		}
	}
	parsed.RawQuery = query.Encode()
	return parsed.String()
}

func commitOutputs(signedDirectory, packageStaging string, names []string, reportContents []byte, clean bool) error {
	parent := filepath.Dir(signedDirectory)
	if err := os.MkdirAll(parent, 0o700); err != nil {
		return err
	}
	temporary, err := os.MkdirTemp(parent, ".release-sign-output-")
	if err != nil {
		return err
	}
	defer os.RemoveAll(temporary)
	packagesDirectory := filepath.Join(temporary, "packages")
	if err := os.Mkdir(packagesDirectory, 0o700); err != nil {
		return err
	}
	for _, name := range names {
		if err := copyNewFile(filepath.Join(packageStaging, name), filepath.Join(packagesDirectory, name)); err != nil {
			return err
		}
	}
	if err := writeNewFile(filepath.Join(temporary, "report.json"), func(writer io.Writer) error {
		_, err := writer.Write(reportContents)
		return err
	}); err != nil {
		return err
	}
	if _, err := os.Lstat(signedDirectory); err == nil {
		if clean {
			if err := os.RemoveAll(signedDirectory); err != nil {
				return err
			}
		} else {
			return errors.New("refusing to replace existing signed directory without --clean")
		}
	} else if !errors.Is(err, os.ErrNotExist) {
		return err
	}
	if err := os.Rename(temporary, signedDirectory); err != nil {
		return fmt.Errorf("atomically publish signed output: %w", err)
	}
	return nil
}

func copyNewFile(source, destination string) error {
	in, err := os.Open(source)
	if err != nil {
		return err
	}
	defer in.Close()
	return writeNewFile(destination, func(out io.Writer) error {
		_, err := io.Copy(out, in)
		return err
	})
}
