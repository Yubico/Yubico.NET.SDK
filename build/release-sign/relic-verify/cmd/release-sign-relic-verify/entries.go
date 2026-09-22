package main

import (
	"archive/zip"
	"errors"
	"fmt"
	"io"
	"path"
	"strings"
)

// selectedEntry is one requested assembly, read out of the package.
type selectedEntry struct {
	name     string
	contents []byte
}

// validateEntryName rejects ZIP names that could escape the archive if
// something later extracted it. The package is not extracted here, but a name
// that is not safe to extract is evidence that the package was not produced by
// the signer we are checking, so it is worth refusing early.
func validateEntryName(name string) error {
	if name == "" {
		return errors.New("name is empty")
	}
	if strings.Contains(name, `\`) {
		return errors.New("backslashes are not allowed")
	}
	if strings.HasPrefix(name, "/") || path.IsAbs(name) {
		return errors.New("absolute paths are not allowed")
	}
	if len(name) >= 2 && name[1] == ':' {
		return errors.New("drive-qualified paths are not allowed")
	}
	for _, segment := range strings.Split(name, "/") {
		if segment == "." || segment == ".." {
			return errors.New("dot path segments are not allowed")
		}
	}
	return nil
}

// validateRequestedName is validateEntryName plus the rules that only apply to
// a name the caller asked us to verify. Such a name has to denote a file, so
// unlike a general archive entry it may not be a directory and may not contain
// an empty segment.
func validateRequestedName(name string) error {
	if err := validateEntryName(name); err != nil {
		return err
	}
	for _, segment := range strings.Split(name, "/") {
		if segment == "" {
			return errors.New("empty path segments are not allowed")
		}
	}
	return nil
}

// selectEntries reads exactly the requested entries out of the archive, in the
// order they were requested. Everything else in the package is left alone:
// checking the NuGet container is the caller's job, not this program's.
func selectEntries(archive *zip.Reader, requested []string) ([]selectedEntry, error) {
	if len(requested) == 0 {
		return nil, errors.New("no assemblies were requested")
	}
	var problems []string

	// Two --assembly values that name the same file, in the same case or in
	// different cases, mean the caller does not know what it asked for.
	byFoldedRequest := make(map[string]string, len(requested))
	for _, name := range requested {
		if err := validateRequestedName(name); err != nil {
			problems = append(problems, fmt.Sprintf("unsafe --assembly %q: %v", name, err))
			continue
		}
		folded := strings.ToLower(name)
		if previous, exists := byFoldedRequest[folded]; exists {
			problems = append(problems, fmt.Sprintf("duplicate --assembly: %s and %s", previous, name))
			continue
		}
		byFoldedRequest[folded] = name
	}

	// A package with two entries that differ only by case is ambiguous on the
	// platforms these packages get extracted on, so we refuse the whole
	// package rather than guess which entry the signature covers.
	matches := make(map[string]*zip.File, len(requested))
	byFoldedEntry := make(map[string]string, len(archive.File))
	for _, entry := range archive.File {
		if err := validateEntryName(entry.Name); err != nil {
			problems = append(problems, fmt.Sprintf("unsafe ZIP entry %q: %v", entry.Name, err))
			continue
		}
		folded := strings.ToLower(entry.Name)
		if previous, exists := byFoldedEntry[folded]; exists {
			problems = append(problems, fmt.Sprintf("duplicate ZIP name: %s and %s", previous, entry.Name))
			continue
		}
		byFoldedEntry[folded] = entry.Name
		if _, ok := byFoldedRequest[folded]; !ok {
			continue
		}
		// The match has to be exact. A request that differs only by case is
		// reported as unmatched below rather than quietly accepted. There can
		// be at most one match per name, because a second entry folding to the
		// same name was already rejected above.
		matches[entry.Name] = entry
	}

	for _, name := range requested {
		if _, ok := matches[name]; !ok {
			problems = append(problems, "no ZIP entry named "+name)
		}
	}
	if len(problems) != 0 {
		return nil, errors.New(strings.Join(problems, "\n"))
	}

	selected := make([]selectedEntry, 0, len(requested))
	for _, name := range requested {
		contents, err := readEntry(matches[name])
		if err != nil {
			return nil, fmt.Errorf("read %s: %w", name, err)
		}
		selected = append(selected, selectedEntry{name: name, contents: contents})
	}
	return selected, nil
}

func readEntry(entry *zip.File) ([]byte, error) {
	reader, err := entry.Open()
	if err != nil {
		return nil, err
	}
	contents, readErr := io.ReadAll(reader)
	return contents, errors.Join(readErr, reader.Close())
}
