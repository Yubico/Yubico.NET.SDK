package main

import (
	"archive/zip"
	"bytes"
	"crypto/sha256"
	"encoding/hex"
	"encoding/xml"
	"errors"
	"fmt"
	"io"
	"os"
	"path"
	"sort"
	"strings"
)

const maxPackageEntrySize = int64(256 << 20)

type packageInfo struct {
	ID           string
	Version      string
	Kind         string
	Path         string
	Name         string
	ArtifactPath string
	Policy       packagePolicy
}

func inspectPackage(extracted extractedPackage) (packageInfo, error) {
	archive, err := zip.OpenReader(extracted.Path)
	if err != nil {
		return packageInfo{}, fmt.Errorf("open package %s: %w", extracted.Name, err)
	}
	defer archive.Close()
	seen := map[string]string{}
	var nuspec *zip.File
	for _, entry := range archive.File {
		if err := validateEntryName(entry.Name); err != nil {
			return packageInfo{}, fmt.Errorf("package %s has unsafe ZIP entry %q: %w", extracted.Name, entry.Name, err)
		}
		folded := strings.ToLower(entry.Name)
		if previous, ok := seen[folded]; ok {
			return packageInfo{}, fmt.Errorf("package %s has duplicate ZIP name (case-insensitive): %s and %s", extracted.Name, previous, entry.Name)
		}
		seen[folded] = entry.Name
		if strings.EqualFold(path.Ext(entry.Name), ".nuspec") {
			if path.Base(entry.Name) != entry.Name {
				return packageInfo{}, fmt.Errorf("package %s nuspec must be root-level: %s", extracted.Name, entry.Name)
			}
			if nuspec != nil {
				return packageInfo{}, fmt.Errorf("package %s contains multiple nuspec files", extracted.Name)
			}
			nuspec = entry
		}
	}
	if nuspec == nil {
		return packageInfo{}, fmt.Errorf("package %s contains no nuspec", extracted.Name)
	}
	contents, err := readZipEntry(nuspec)
	if err != nil {
		return packageInfo{}, err
	}
	id, version, err := parseNuspecIdentity(contents)
	if err != nil {
		return packageInfo{}, fmt.Errorf("parse nuspec in %s: %w", extracted.Name, err)
	}
	if id == "" || version == "" {
		return packageInfo{}, fmt.Errorf("nuspec in %s must contain id and version", extracted.Name)
	}
	kind := strings.TrimPrefix(strings.ToLower(path.Ext(extracted.Name)), ".")
	return packageInfo{ID: id, Version: version, Kind: kind, Path: extracted.Path, Name: extracted.Name, ArtifactPath: extracted.ArtifactPath}, nil
}

func parseNuspecIdentity(contents []byte) (string, string, error) {
	decoder := xml.NewDecoder(bytes.NewReader(contents))
	metadataDepth := 0
	var ids, versions []string
	for {
		token, err := decoder.Token()
		if errors.Is(err, io.EOF) {
			break
		}
		if err != nil {
			return "", "", err
		}
		switch value := token.(type) {
		case xml.StartElement:
			if value.Name.Local == "metadata" {
				metadataDepth++
				continue
			}
			if metadataDepth == 0 || (value.Name.Local != "id" && value.Name.Local != "version") {
				continue
			}
			var text string
			if err := decoder.DecodeElement(&text, &value); err != nil {
				return "", "", err
			}
			if value.Name.Local == "id" {
				ids = append(ids, strings.TrimSpace(text))
			} else {
				versions = append(versions, strings.TrimSpace(text))
			}
		case xml.EndElement:
			if value.Name.Local == "metadata" && metadataDepth > 0 {
				metadataDepth--
			}
		}
	}
	if len(ids) != 1 {
		return "", "", fmt.Errorf("nuspec metadata must contain exactly one id, found %d", len(ids))
	}
	if len(versions) != 1 {
		return "", "", fmt.Errorf("nuspec metadata must contain exactly one version, found %d", len(versions))
	}
	return ids[0], versions[0], nil
}

func planPackages(m manifest, packages []packageInfo) ([]packageInfo, error) {
	seen := map[string]packageInfo{}
	for i := range packages {
		policy, ok := m.Packages[packages[i].ID]
		if !ok {
			return nil, fmt.Errorf("unknown package ID %q", packages[i].ID)
		}
		if packages[i].Kind != "nupkg" && packages[i].Kind != "snupkg" {
			return nil, fmt.Errorf("unknown package kind %q", packages[i].Kind)
		}
		key := strings.ToLower(packages[i].ID + "\x00" + packages[i].Kind)
		if previous, ok := seen[key]; ok {
			return nil, fmt.Errorf("duplicate %s package for %s: %s and %s", packages[i].Kind, packages[i].ID, previous.Name, packages[i].Name)
		}
		packages[i].Policy = policy
		seen[key] = packages[i]
	}
	for id, policy := range m.Packages {
		nupkg, ok := seen[strings.ToLower(id+"\x00nupkg")]
		if !ok {
			return nil, fmt.Errorf("missing package %s.nupkg", id)
		}
		symbols, hasSymbols := seen[strings.ToLower(id+"\x00snupkg")]
		if policy.Symbols == "required" && (!hasSymbols || symbols.Version != nupkg.Version) {
			return nil, fmt.Errorf("package %s requires exactly one matching .snupkg with version %s", id, nupkg.Version)
		}
		if policy.Symbols == "absent" && hasSymbols {
			return nil, fmt.Errorf("package %s requires symbols absent", id)
		}
	}
	sort.Slice(packages, func(i, j int) bool {
		if packages[i].ID != packages[j].ID {
			return packages[i].ID < packages[j].ID
		}
		return packages[i].Kind < packages[j].Kind
	})
	return packages, nil
}

func selectEntries(filename string, policy authenticodePolicy) (map[string]struct{}, error) {
	archive, err := zip.OpenReader(filename)
	if err != nil {
		return nil, err
	}
	defer archive.Close()
	wanted := map[string]bool{}
	for _, name := range policy.Include {
		wanted[name] = true
	}
	selected := map[string]struct{}{}
	seen := map[string]string{}
	var problems []string
	for _, entry := range archive.File {
		if err := validateEntryName(entry.Name); err != nil {
			problems = append(problems, fmt.Sprintf("unsafe ZIP entry %q: %v", entry.Name, err))
		}
		folded := strings.ToLower(entry.Name)
		if previous, ok := seen[folded]; ok {
			problems = append(problems, fmt.Sprintf("duplicate ZIP name (case-insensitive): %s and %s", previous, entry.Name))
		}
		seen[folded] = entry.Name
		if wanted[entry.Name] {
			selected[entry.Name] = struct{}{}
		}
	}
	for _, name := range policy.Include {
		if _, ok := selected[name]; !ok {
			problems = append(problems, "unmatched include: "+name)
		}
	}
	for _, entry := range archive.File {
		for _, pattern := range policy.FirstParty {
			match, _ := path.Match(strings.ToLower(pattern), strings.ToLower(path.Base(entry.Name)))
			if match {
				if _, ok := selected[entry.Name]; !ok {
					problems = append(problems, "first-party DLL is not selected: "+entry.Name)
				}
				break
			}
		}
	}
	if len(problems) > 0 {
		sort.Strings(problems)
		return nil, errors.New(strings.Join(problems, "\n"))
	}
	return selected, nil
}

func rewritePackage(input, output string, selected map[string]struct{}, transform func(string, []byte) ([]byte, error)) error {
	archive, err := zip.OpenReader(input)
	if err != nil {
		return err
	}
	defer archive.Close()
	return writeNewFile(output, func(destination io.Writer) error {
		writer := zip.NewWriter(destination)
		for _, entry := range archive.File {
			if _, ok := selected[entry.Name]; !ok {
				if err := writer.Copy(entry); err != nil {
					return err
				}
				continue
			}
			contents, err := readZipEntry(entry)
			if err != nil {
				return err
			}
			replacement, err := transform(entry.Name, contents)
			if err != nil {
				return fmt.Errorf("sign %s: %w", entry.Name, err)
			}
			header := entry.FileHeader
			header.Extra = nil
			header.CRC32, header.CompressedSize, header.CompressedSize64 = 0, 0, 0
			header.UncompressedSize, header.UncompressedSize64 = 0, 0
			out, err := writer.CreateHeader(&header)
			if err != nil {
				return err
			}
			if _, err := out.Write(replacement); err != nil {
				return err
			}
		}
		return writer.Close()
	})
}

func verifyPreservation(input, output string, selected map[string]struct{}, symbolPackage bool) error {
	want, err := archiveContents(input)
	if err != nil {
		return err
	}
	got, err := archiveContents(output)
	if err != nil {
		return err
	}
	delete(got, ".signature.p7s")
	if len(want) != len(got) {
		return errors.New("signed package entry set differs from input")
	}
	for name, before := range want {
		after, ok := got[name]
		if !ok {
			return fmt.Errorf("signed package is missing %s", name)
		}
		if symbolPackage {
			if !bytes.Equal(before, after) {
				return fmt.Errorf("symbol package entry changed: %s", name)
			}
			continue
		}
		if _, chosen := selected[name]; !chosen && !bytes.Equal(before, after) {
			return fmt.Errorf("unselected package entry changed: %s", name)
		}
	}
	return nil
}

func archiveContents(filename string) (map[string][]byte, error) {
	archive, err := zip.OpenReader(filename)
	if err != nil {
		return nil, err
	}
	defer archive.Close()
	result := make(map[string][]byte, len(archive.File))
	for _, entry := range archive.File {
		contents, err := readZipEntry(entry)
		if err != nil {
			return nil, err
		}
		result[entry.Name] = contents
	}
	return result, nil
}

func readZipEntry(entry *zip.File) ([]byte, error) {
	if entry.UncompressedSize64 > uint64(maxPackageEntrySize) {
		return nil, fmt.Errorf("ZIP entry %s exceeds 256 MiB limit", entry.Name)
	}
	reader, err := entry.Open()
	if err != nil {
		return nil, err
	}
	contents, readErr := io.ReadAll(io.LimitReader(reader, maxPackageEntrySize+1))
	if int64(len(contents)) > maxPackageEntrySize {
		return nil, errors.Join(fmt.Errorf("ZIP entry %s exceeds 256 MiB limit", entry.Name), reader.Close())
	}
	return contents, errors.Join(readErr, reader.Close())
}

func writeNewFile(filename string, write func(io.Writer) error) error {
	file, err := os.OpenFile(filename, os.O_CREATE|os.O_EXCL|os.O_WRONLY, 0o600)
	if err != nil {
		return err
	}
	ok := false
	defer func() {
		file.Close()
		if !ok {
			os.Remove(filename)
		}
	}()
	if err := write(file); err != nil {
		return err
	}
	if err := file.Close(); err != nil {
		return err
	}
	ok = true
	return nil
}

func sha256File(filename string) (string, error) {
	file, err := os.Open(filename)
	if err != nil {
		return "", err
	}
	defer file.Close()
	hash := sha256.New()
	if _, err := io.Copy(hash, file); err != nil {
		return "", err
	}
	return hex.EncodeToString(hash.Sum(nil)), nil
}

func sha256Bytes(contents []byte) string {
	digest := sha256.Sum256(contents)
	return hex.EncodeToString(digest[:])
}
