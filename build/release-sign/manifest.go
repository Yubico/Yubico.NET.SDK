package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"
	"path"
	"strings"
)

type manifest struct {
	Schema          int                      `json:"schema"`
	AttestationRepo string                   `json:"attestationRepo"`
	SignerWorkflow  string                   `json:"signerWorkflow"`
	Packages        map[string]packagePolicy `json:"packages"`
}

type packagePolicy struct {
	Symbols      string              `json:"symbols"`
	Authenticode *authenticodePolicy `json:"authenticode,omitempty"`
}

type authenticodePolicy struct {
	Include       []string `json:"include"`
	FirstParty    []string `json:"firstParty"`
	AlreadySigned string   `json:"alreadySigned"`
}

var componentPackages = map[string][]string{
	"core":        {"Yubico.Core", "Yubico.YubiKey"},
	"nativeshims": {"Yubico.NativeShims"},
}

var componentWorkflows = map[string]string{
	"core":        "Yubico/Yubico.NET.SDK/.github/workflows/build.yml",
	"nativeshims": "Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml",
}

func loadManifest(filename, component string) (manifest, string, error) {
	b, err := os.ReadFile(filename)
	if err != nil {
		return manifest{}, "", err
	}
	decoder := json.NewDecoder(strings.NewReader(string(b)))
	decoder.DisallowUnknownFields()
	var m manifest
	if err := decoder.Decode(&m); err != nil {
		return manifest{}, "", fmt.Errorf("parse manifest: %w", err)
	}
	var extra any
	if err := decoder.Decode(&extra); !errors.Is(err, io.EOF) {
		if err == nil {
			return manifest{}, "", errors.New("manifest contains multiple JSON values")
		}
		return manifest{}, "", fmt.Errorf("parse manifest: %w", err)
	}
	if err := validateManifest(m, component); err != nil {
		return manifest{}, "", err
	}
	return m, sha256Bytes(b), nil
}

func validateManifest(m manifest, component string) error {
	wanted, ok := componentPackages[component]
	if !ok {
		return errors.New(`--component must be "core" or "nativeshims"`)
	}
	if m.Schema != 1 {
		return errors.New("manifest schema must be 1")
	}
	if strings.TrimSpace(m.AttestationRepo) == "" {
		return errors.New("manifest attestationRepo is required")
	}
	if m.SignerWorkflow != componentWorkflows[component] {
		return fmt.Errorf("manifest signerWorkflow must be %q", componentWorkflows[component])
	}
	allowed := make(map[string]bool, len(wanted))
	for _, id := range wanted {
		allowed[id] = true
	}
	for id, policy := range m.Packages {
		if !allowed[id] {
			if _, known := knownPackage(id); !known {
				return fmt.Errorf("unknown package ID %q", id)
			}
			return fmt.Errorf("package ID %q is not valid for component %q", id, component)
		}
		if policy.Symbols != "required" && policy.Symbols != "absent" {
			return fmt.Errorf("package %s symbols must be required or absent", id)
		}
		if policy.Authenticode == nil {
			return fmt.Errorf("package %s must have an authenticode policy", id)
		}
		if err := validateAuthenticode(*policy.Authenticode); err != nil {
			return fmt.Errorf("package %s: %w", id, err)
		}
	}
	for _, id := range wanted {
		if _, ok := m.Packages[id]; !ok {
			return fmt.Errorf("manifest is missing package %q", id)
		}
	}
	return nil
}

func knownPackage(id string) (string, bool) {
	for component, ids := range componentPackages {
		for _, known := range ids {
			if id == known {
				return component, true
			}
		}
	}
	return "", false
}

func validateAuthenticode(p authenticodePolicy) error {
	if len(p.Include) == 0 || len(p.FirstParty) == 0 {
		return errors.New("authenticode include and firstParty must not be empty")
	}
	if p.AlreadySigned != "reject" && p.AlreadySigned != "replace" {
		return errors.New(`authenticode alreadySigned must be "reject" or "replace"`)
	}
	seen := map[string]bool{}
	for _, name := range p.Include {
		if err := validateEntryName(name); err != nil {
			return fmt.Errorf("unsafe include %q: %w", name, err)
		}
		if !strings.EqualFold(path.Ext(name), ".dll") {
			return fmt.Errorf("include %q must end in .dll", name)
		}
		if seen[name] {
			return fmt.Errorf("duplicate include %q", name)
		}
		seen[name] = true
	}
	for _, pattern := range p.FirstParty {
		if pattern == "" {
			return errors.New("firstParty pattern must not be empty")
		}
		if _, err := path.Match(pattern, "candidate.dll"); err != nil {
			return fmt.Errorf("invalid firstParty pattern %q: %w", pattern, err)
		}
	}
	return nil
}

func validateEntryName(name string) error {
	if name == "" {
		return errors.New("name is empty")
	}
	if strings.Contains(name, "\\") {
		return errors.New("backslashes are not allowed")
	}
	if strings.HasPrefix(name, "/") || path.IsAbs(name) || (len(name) >= 2 && name[1] == ':') {
		return errors.New("absolute paths are not allowed")
	}
	for _, part := range strings.Split(name, "/") {
		if part == "." || part == ".." {
			return errors.New("dot path segments are not allowed")
		}
	}
	return nil
}
